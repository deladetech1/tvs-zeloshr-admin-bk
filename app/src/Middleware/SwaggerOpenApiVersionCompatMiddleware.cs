using System.Text;

namespace ZelosHR.Api.Middleware;

/// <summary>
/// Rewrites <c>"openapi": "3.0.4"</c> to <c>3.0.3</c> so bundled Swagger UI can render the spec
/// (see https://github.com/swagger-api/swagger-ui/issues/10502).
/// </summary>
internal sealed class SwaggerOpenApiVersionCompatMiddleware(RequestDelegate next)
{
    private const string UnsupportedVersion = "\"openapi\": \"3.0.4\"";
    private const string UiCompatibleVersion = "\"openapi\": \"3.0.3\"";

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";
        if (!HttpMethods.IsGet(context.Request.Method)
            || !path.Contains("/swagger/", StringComparison.OrdinalIgnoreCase)
            || !path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        var originalBody = context.Response.Body;
        await using var buffer = new MemoryStream();
        context.Response.Body = buffer;

        try
        {
            await next(context);

            buffer.Position = 0;
            if (context.Response.StatusCode != StatusCodes.Status200OK || buffer.Length == 0)
            {
                await buffer.CopyToAsync(originalBody, context.RequestAborted);
                return;
            }

            using var reader = new StreamReader(buffer, Encoding.UTF8);
            var json = await reader.ReadToEndAsync(context.RequestAborted);
            if (json.Contains(UnsupportedVersion, StringComparison.Ordinal))
                json = json.Replace(UnsupportedVersion, UiCompatibleVersion, StringComparison.Ordinal);

            var bytes = Encoding.UTF8.GetBytes(json);
            context.Response.Body = originalBody;
            context.Response.ContentLength = bytes.Length;
            await originalBody.WriteAsync(bytes, context.RequestAborted);
        }
        finally
        {
            context.Response.Body = originalBody;
        }
    }
}
