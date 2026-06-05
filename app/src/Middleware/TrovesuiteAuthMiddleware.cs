using System.Text.Json;
using Microsoft.Extensions.Options;
using Trovesuite.Package.Auth;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Tenant;

namespace ZelosHR.Api.Middleware;

/// <summary>
/// Validates TroveSuite JWT via <see cref="IAuthService"/> and populates tenant/user context.
/// Skipped when <see cref="TrovesuiteIntegrationOptions.RequireAuthentication"/> is false.
/// </summary>
public class TrovesuiteAuthMiddleware
{
    private static readonly PathString[] AnonymousPrefixes =
    [
        new("/health"),
        new("/swagger"),
        new("/"),
    ];

    private readonly RequestDelegate _next;
    private readonly ILogger<TrovesuiteAuthMiddleware> _logger;

    public TrovesuiteAuthMiddleware(RequestDelegate next, ILogger<TrovesuiteAuthMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IAuthService authService,
        IConfiguration configuration,
        IOptions<TrovesuiteIntegrationOptions> integrationOptions)
    {
        if (HttpMethods.IsOptions(context.Request.Method))
        {
            await _next(context);
            return;
        }

        if (!integrationOptions.Value.RequireAuthentication || IsAnonymous(context.Request.Path))
        {
            await _next(context);
            return;
        }

        if (!JwtSecretConfiguration.IsConfigured(configuration))
        {
            _logger.LogError("JWT secret is not configured; set SECRET_KEY on the Container App.");
            await WriteServiceUnavailableAsync(context, JwtSecretConfiguration.MissingKeyMessage);
            return;
        }

        var token = TroveBearerTokenHelper.ExtractBearerToken(context);
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning(
                "Missing Bearer token on {Method} {Path}",
                context.Request.Method,
                context.Request.Path);
            await WriteUnauthorizedAsync(context, new Dictionary<string, string>
            {
                ["authorization"] = "Header 'authorization' must be a Bearer JWT.",
            });
            return;
        }

        var result = await authService.AuthorizeUserFromTokenAsync(token);
        if (!result.Success || result.Data is null || result.Data.Count == 0)
        {
            _logger.LogWarning("Trovesuite auth failed: {Error}", result.Error);
            context.Response.StatusCode = result.StatusCode > 0 ? result.StatusCode : StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(result, PlatformJson.SerializerOptions));
            return;
        }

        var principal = result.Data[0];
        var orgFromHeader = context.Items[TrovesuiteHttpContextKeys.OrgId] as string;

        context.Items[TrovesuiteHttpContextKeys.AuthEntries] = result.Data;
        context.Items[TrovesuiteHttpContextKeys.UserId] = principal.UserId;
        context.Items[TrovesuiteHttpContextKeys.TenantId] = principal.TenantId;
        context.Items[TrovesuiteHttpContextKeys.OrgId] =
            !string.IsNullOrWhiteSpace(orgFromHeader) ? orgFromHeader : principal.OrgId;
        context.Items[TrovesuiteHttpContextKeys.Permissions] =
            result.Data.SelectMany(e => e.Permissions ?? []).Distinct().ToList();

        await _next(context);
    }

    private static bool IsAnonymous(PathString path)
    {
        foreach (var prefix in AnonymousPrefixes)
        {
            if (path.StartsWithSegments(prefix))
                return true;
        }

        return false;
    }

    private static Task WriteUnauthorizedAsync(HttpContext context, Dictionary<string, string> fieldErrors)
    {
        var response = Respons<object>.ValidationError(fieldErrors);
        response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsync(JsonSerializer.Serialize(response, PlatformJson.SerializerOptions));
    }

    private static Task WriteServiceUnavailableAsync(HttpContext context, string message)
    {
        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            success = false,
            status_code = 503,
            error = message,
        }, PlatformJson.SerializerOptions));
    }
}
