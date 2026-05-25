using System.Text;
using System.Text.RegularExpressions;

namespace ZelosHR.Api.Middleware;

/// <summary>
/// Rewrites OpenAPI 3.0.4+ to 3.0.3 for bundled Swagger UI and disables caching of the spec.
/// </summary>
internal sealed partial class SwaggerOpenApiVersionCompatMiddleware(RequestDelegate next)
{
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

        context.Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
        context.Response.Headers.Pragma = "no-cache";

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
            json = OpenApiVersionRegex().Replace(json, UiCompatibleVersion);

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

    [GeneratedRegex("\"openapi\":\\s*\"3\\.0\\.(?:[4-9]|[1-9]\\d+)\"", RegexOptions.CultureInvariant)]
    private static partial Regex OpenApiVersionRegex();
}
