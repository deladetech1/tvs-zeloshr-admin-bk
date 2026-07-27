using System.Text.Json;
using Microsoft.Extensions.Options;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Persistence.Repositories;
using ZelosHR.Api.Shared.Constants;
using ZelosHR.Api.Shared.Tenant;

namespace ZelosHR.Api.Middleware;

/// <summary>
/// Enforces Trove standard headers on <c>/api/v1/*</c> routes and populates request context
/// (<c>app-id</c>, <c>org-id</c>, <c>bus-id</c>, <c>loc-id</c>, JWT claims when auth is off).
/// </summary>
public sealed class TroveRequestHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TroveRequestHeadersMiddleware> _logger;

    public TroveRequestHeadersMiddleware(RequestDelegate next, ILogger<TroveRequestHeadersMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IOptions<TrovesuiteIntegrationOptions> integrationOptions,
        IOptions<AppSettings> appSettings,
        IPlatformContextRepository platformContext)
    {
        if (HttpMethods.IsOptions(context.Request.Method))
        {
            await _next(context);
            return;
        }

        if (!RequiresStandardHeaders(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var options = integrationOptions.Value;
        if (!options.RequireStandardHeaders)
        {
            PopulateFromHeaders(context);
            await _next(context);
            return;
        }

        foreach (var headerName in TroveStandardHeaders.RequiredForApi)
        {
            if (string.IsNullOrWhiteSpace(context.Request.Headers[headerName]))
            {
                _logger.LogWarning(
                    "Missing required header {HeaderName} on {Method} {Path}",
                    headerName,
                    context.Request.Method,
                    context.Request.Path);
                await WriteValidationErrorAsync(
                    context,
                    StatusCodes.Status400BadRequest,
                    new Dictionary<string, string>
                    {
                        [headerName] =
                            $"Required header '{headerName}' is missing. Include it on every /api/v1/* request.",
                    });
                return;
            }
        }

        var expectedAppId = appSettings.Value.AppId;
        var appId = context.Request.Headers[TroveStandardHeaders.AppId].ToString().Trim();
        if (!string.Equals(appId, expectedAppId, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "Invalid app-id {AppId} on {Method} {Path}",
                appId,
                context.Request.Method,
                context.Request.Path);
            await WriteValidationErrorAsync(
                context,
                StatusCodes.Status400BadRequest,
                new Dictionary<string, string>
                {
                    [TroveStandardHeaders.AppId] =
                        $"Header '{TroveStandardHeaders.AppId}' must be '{expectedAppId}'.",
                });
            return;
        }

        if (TroveBearerTokenHelper.ExtractBearerToken(context) is null)
        {
            _logger.LogWarning(
                "Missing Bearer token on {Method} {Path}",
                context.Request.Method,
                context.Request.Path);
            await WriteValidationErrorAsync(
                context,
                StatusCodes.Status401Unauthorized,
                new Dictionary<string, string>
                {
                    ["authorization"] = "Header 'authorization' must be a Bearer JWT.",
                });
            return;
        }

        PopulateFromHeaders(context);

        // Read tenant_id / user_id from Bearer for platform validation even when full
        // Trovesuite auth runs next (RequireAuthentication). Auth overwrites with validated claims.
        PopulateClaimsFromBearerWithoutValidation(context);

        if (options.ValidatePlatformContext)
        {
            var tenantId = context.Items[TrovesuiteHttpContextKeys.TenantId] as string;
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                _logger.LogWarning(
                    "JWT missing tenant_id claim on {Method} {Path}",
                    context.Request.Method,
                    context.Request.Path);
                await WriteValidationErrorAsync(
                    context,
                    StatusCodes.Status401Unauthorized,
                    new Dictionary<string, string>
                    {
                        ["authorization"] = "JWT must include a tenant_id claim for platform context validation.",
                    });
                return;
            }

            var userId = context.Items[TrovesuiteHttpContextKeys.UserId] as string;
            var orgId = (string)context.Items[TrovesuiteHttpContextKeys.OrgId]!;
            var busId = (string)context.Items[TrovesuiteHttpContextKeys.BusId]!;
            var locId = (string)context.Items[TrovesuiteHttpContextKeys.LocId]!;

            var valid = await platformContext.ValidateSessionContextAsync(
                tenantId, userId, orgId, busId, locId, expectedAppId, context.RequestAborted);

            if (!valid)
            {
                _logger.LogWarning(
                    "Invalid platform context for tenant {TenantId} user {UserId} on {Method} {Path}",
                    tenantId,
                    userId,
                    context.Request.Method,
                    context.Request.Path);
                await WriteErrorAsync(
                    context,
                    StatusCodes.Status403Forbidden,
                    "Invalid platform context: org-id, bus-id, loc-id, and app-id are not configured for this user/tenant, or the user has no access.");
                return;
            }
        }

        await _next(context);
    }

    private static bool RequiresStandardHeaders(PathString path) =>
        path.StartsWithSegments("/api/v1")
        && !path.StartsWithSegments("/api/v1/health")
        && !path.StartsWithSegments("/api/v1/public");

    private static void PopulateFromHeaders(HttpContext context)
    {
        context.Items[TrovesuiteHttpContextKeys.AppId] =
            context.Request.Headers[TroveStandardHeaders.AppId].ToString().Trim();
        context.Items[TrovesuiteHttpContextKeys.OrgId] =
            context.Request.Headers[TroveStandardHeaders.OrgId].ToString().Trim();
        context.Items[TrovesuiteHttpContextKeys.BusId] =
            context.Request.Headers[TroveStandardHeaders.BusId].ToString().Trim();
        context.Items[TrovesuiteHttpContextKeys.LocId] =
            context.Request.Headers[TroveStandardHeaders.LocId].ToString().Trim();
    }

    private static void PopulateClaimsFromBearerWithoutValidation(HttpContext context)
    {
        var token = TroveBearerTokenHelper.ExtractBearerToken(context);
        if (token is null)
            return;

        var claims = TroveBearerTokenHelper.ReadUnvalidatedClaims(token);
        if (claims.TryGetValue(CorePlatformConstants.JwtClaims.TenantId, out var tenantId)
            && !string.IsNullOrWhiteSpace(tenantId))
            context.Items[TrovesuiteHttpContextKeys.TenantId] = tenantId;
        if (claims.TryGetValue(CorePlatformConstants.JwtClaims.UserId, out var userId)
            && !string.IsNullOrWhiteSpace(userId))
            context.Items[TrovesuiteHttpContextKeys.UserId] = userId;
    }

    private static Task WriteValidationErrorAsync(
        HttpContext context,
        int statusCode,
        Dictionary<string, string> fieldErrors)
    {
        var response = Respons<object>.ValidationError(fieldErrors);
        response.StatusCode = statusCode;
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsync(JsonSerializer.Serialize(response, PlatformJson.SerializerOptions));
    }

    private static Task WriteErrorAsync(HttpContext context, int statusCode, string message)
    {
        var response = Respons<object>.Fail(message, statusCode: statusCode);
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsync(JsonSerializer.Serialize(response, PlatformJson.SerializerOptions));
    }
}
