using System.Text.Json;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Validation;

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
            await context.Response.WriteAsync(JsonSerializer.Serialize(ex.Response, PlatformJson.SerializerOptions));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(
                "Authorization failed on {Method} {Path}: {Message}",
                context.Request.Method,
                context.Request.Path,
                ex.Message);
            await WriteResponsAsync(context, Respons<object>.Forbidden(ex.Message));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogInformation(ex, "Not found");
            await WriteResponsAsync(context, Respons<object>.NotFound(ex.Message));
        }
        catch (PlatformUserConflictException ex)
        {
            _logger.LogInformation(ex, "Platform user conflict on {Field}", ex.FieldKey);
            await WriteResponsAsync(context, Respons<object>.ValidationError(
                new Dictionary<string, string> { [ex.FieldKey] = ex.Message }));
        }
        catch (InvalidOperationException ex)
        {
            if (ClientSafeErrors.IsEntityFrameworkQueryTranslationError(ex.Message))
                _logger.LogError(ex, "EF query translation failed on {Method} {Path}",
                    context.Request.Method, context.Request.Path);
            else
                _logger.LogInformation(ex, "Invalid operation");

            if (ex.Message.Contains("Platform user already exists", StringComparison.OrdinalIgnoreCase)
                || ex.Message.Contains("Platform user not found", StringComparison.OrdinalIgnoreCase))
            {
                await WriteResponsAsync(context, Respons<object>.ValidationError(
                    new Dictionary<string, string>
                    {
                        ["identity.work_email"] = EmployeeErrorMessages.WorkEmailAlreadyRegistered,
                    }));
                return;
            }

            var clientMessage = ClientSafeErrors.SanitizeInvalidOperationMessage(ex.Message);
            await WriteResponsAsync(context, Respons<object>.ValidationError(
                new Dictionary<string, string> { ["request"] = clientMessage },
                summary: clientMessage));
        }
        catch (ArgumentException ex)
        {
            _logger.LogInformation(ex, "Bad request");
            await WriteResponsAsync(context, Respons<object>.ValidationError(new Dictionary<string, string>
            {
                ["request"] = ex.Message.TrimEnd('.') + ".",
            }));
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
        await context.Response.WriteAsync(JsonSerializer.Serialize(body, PlatformJson.SerializerOptions));
    }
}
