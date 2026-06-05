using ZelosHR.Api.Shared.Tenant;

namespace ZelosHR.Api.Middleware;

/// <summary>Adds tenant/user/org to structured logs for the remainder of the request.</summary>
public sealed class RequestContextLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestContextLoggingMiddleware> _logger;

    public RequestContextLoggingMiddleware(RequestDelegate next, ILogger<RequestContextLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var requestId = context.Items["RequestId"] as string;
        var tenantId = context.Items[TrovesuiteHttpContextKeys.TenantId] as string;
        var userId = context.Items[TrovesuiteHttpContextKeys.UserId] as string;
        var orgId = context.Items[TrovesuiteHttpContextKeys.OrgId] as string;

        using (_logger.BeginScope(new Dictionary<string, object?>
        {
            ["RequestId"] = requestId,
            ["TenantId"] = tenantId,
            ["UserId"] = userId,
            ["OrgId"] = orgId,
        }))
        {
            await _next(context);
        }
    }
}
