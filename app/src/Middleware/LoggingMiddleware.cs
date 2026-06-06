using System.Diagnostics;

namespace ZelosHR.Api.Middleware;

public class LoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<LoggingMiddleware> _logger;

    public LoggingMiddleware(RequestDelegate next, ILogger<LoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var requestId = Guid.NewGuid().ToString();
        context.Items["RequestId"] = requestId;
        context.Response.Headers["X-Request-Id"] = requestId;

        var sw = Stopwatch.StartNew();
        _logger.LogInformation(
            "HTTP {Method} {Path} started [RequestId: {RequestId}]",
            context.Request.Method,
            context.Request.Path,
            requestId);

        try
        {
            await _next(context);
            sw.Stop();
            if (context.Response.StatusCode is StatusCodes.Status401Unauthorized or StatusCodes.Status403Forbidden)
            {
                var authEvent = context.Response.StatusCode == StatusCodes.Status401Unauthorized
                    ? "Authentication"
                    : "Authorization";
                _logger.LogWarning(
                    "{AuthEvent} failed on {Method} {Path} in {ElapsedMs}ms [RequestId: {RequestId}]",
                    authEvent,
                    context.Request.Method,
                    context.Request.Path,
                    sw.ElapsedMilliseconds,
                    requestId);
            }
            else
            {
                _logger.LogInformation(
                    "HTTP {Method} {Path} completed {StatusCode} in {ElapsedMs}ms [RequestId: {RequestId}]",
                    context.Request.Method,
                    context.Request.Path,
                    context.Response.StatusCode,
                    sw.ElapsedMilliseconds,
                    requestId);
            }
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(
                ex,
                "HTTP {Method} {Path} failed after {ElapsedMs}ms [RequestId: {RequestId}]",
                context.Request.Method,
                context.Request.Path,
                sw.ElapsedMilliseconds,
                requestId);
            throw;
        }
    }
}
