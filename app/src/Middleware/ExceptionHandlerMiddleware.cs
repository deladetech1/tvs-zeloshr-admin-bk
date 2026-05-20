using System.Net;
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";
            var body = Respons<object>.Fail("An unexpected error occurred", 500);
            await context.Response.WriteAsync(JsonSerializer.Serialize(body));
        }
    }
}
