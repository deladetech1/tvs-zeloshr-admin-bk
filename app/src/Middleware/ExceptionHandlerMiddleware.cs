using System.Text.Json;
using ZelosHR.Api.Entities.Shared;

namespace ZelosHR.Api.Middleware;

public class ExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlerMiddleware> _logger;

    public ExceptionHandlerMiddleware(RequestDelegate next, ILogger<ExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ResponseException ex)
        {
            _logger.LogInformation("ResponseException: {Message}", ex.Message);
            context.Response.StatusCode = ex.Response.StatusCode;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(ex.Response));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Forbidden");
            await WriteResponsAsync(context, Respons<object>.Forbidden(ex.Message));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogInformation(ex, "Not found");
            await WriteResponsAsync(context, Respons<object>.NotFound(ex.Message));
        }
        catch (ArgumentException ex)
        {
            _logger.LogInformation(ex, "Bad request");
            await WriteResponsAsync(context, Respons<object>.Fail(ex.Message, statusCode: 400));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            var message = context.RequestServices.GetService<IHostEnvironment>()?.IsDevelopment() == true
                ? ex.Message
                : "An unexpected error occurred";
            await WriteResponsAsync(context, Respons<object>.Fail(message, statusCode: 500));
        }
    }

    private static async Task WriteResponsAsync(HttpContext context, Respons<object> body)
    {
        context.Response.StatusCode = body.StatusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(body));
    }
}
