using System.Text.Json;
using Microsoft.Extensions.Options;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Shared.Tenant;

namespace ZelosHR.Api.Middleware;

/// <summary>
/// Enforces Trove standard headers on <c>/api/v1/*</c> routes and populates request context
/// (<c>app-id</c>, <c>org-id</c>, <c>bus-id</c>, <c>loc-id</c>, JWT claims when auth is off).
/// </summary>
public sealed class TroveRequestHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public TroveRequestHeadersMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(
        HttpContext context,
        IOptions<TrovesuiteIntegrationOptions> integrationOptions)
    {
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
                await WriteErrorAsync(
                    context,
                    StatusCodes.Status400BadRequest,
                    $"Required header '{headerName}' is missing.");
                return;
            }
        }

        var appId = context.Request.Headers[TroveStandardHeaders.AppId].ToString().Trim();
        if (!string.Equals(appId, TroveStandardHeaders.HrAppId, StringComparison.OrdinalIgnoreCase))
        {
            await WriteErrorAsync(
                context,
                StatusCodes.Status400BadRequest,
                $"Header '{TroveStandardHeaders.AppId}' must be '{TroveStandardHeaders.HrAppId}'.");
            return;
        }

        if (TroveBearerTokenHelper.ExtractBearerToken(context) is null)
        {
            await WriteErrorAsync(
                context,
                StatusCodes.Status401Unauthorized,
                "Header 'authorization' must be a Bearer JWT.");
            return;
        }

        PopulateFromHeaders(context);

        if (!options.RequireAuthentication)
            PopulateClaimsFromBearerWithoutValidation(context);

        await _next(context);
    }

    private static bool RequiresStandardHeaders(PathString path) =>
        path.StartsWithSegments("/api/v1")
        && !path.StartsWithSegments("/api/v1/health");

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
        if (claims.TryGetValue("tenant_id", out var tenantId) && !string.IsNullOrWhiteSpace(tenantId))
            context.Items[TrovesuiteHttpContextKeys.TenantId] = tenantId;
        if (claims.TryGetValue("user_id", out var userId) && !string.IsNullOrWhiteSpace(userId))
            context.Items[TrovesuiteHttpContextKeys.UserId] = userId;
    }

    private static Task WriteErrorAsync(HttpContext context, int statusCode, string message)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            success = false,
            statusCode,
            error = message,
        }));
    }
}
