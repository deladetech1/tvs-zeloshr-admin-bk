using ZelosHR.Api.Configs;

namespace ZelosHR.Api.Shared.Tenant;

public sealed class TenantContext
{
    public string TenantId { get; init; } = string.Empty;
    public string OrgId { get; init; } = string.Empty;
    public string AppId { get; init; } = TroveStandardHeaders.HrAppId;
    public string BusId { get; init; } = string.Empty;
    public string LocId { get; init; } = string.Empty;
    public string? UserId { get; init; }
    public IReadOnlyList<string> Permissions { get; init; } = [];
}

public interface ITenantContextAccessor
{
    TenantContext Current { get; }
}

public sealed class TenantContextAccessor : ITenantContextAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IHostEnvironment _environment;
    private readonly LocalDevelopmentOptions _localDev;

    public TenantContextAccessor(
        IHttpContextAccessor httpContextAccessor,
        IHostEnvironment environment,
        Microsoft.Extensions.Options.IOptions<LocalDevelopmentOptions> localDev)
    {
        _httpContextAccessor = httpContextAccessor;
        _environment = environment;
        _localDev = localDev.Value;
    }

    public TenantContext Current
    {
        get
        {
            var http = _httpContextAccessor.HttpContext;
            if (http is null)
                return new TenantContext();

            var tenantId = http.Items[TrovesuiteHttpContextKeys.TenantId] as string
                ?? http.Request.Headers[TroveStandardHeaders.LegacyTenantId].FirstOrDefault()
                ?? DevFallback(_localDev.TenantId);
            var orgId = http.Items[TrovesuiteHttpContextKeys.OrgId] as string
                ?? http.Request.Headers[TroveStandardHeaders.OrgId].FirstOrDefault()
                ?? http.Request.Headers[TroveStandardHeaders.LegacyOrgId].FirstOrDefault()
                ?? DevFallback(_localDev.OrgId);
            var appId = http.Items[TrovesuiteHttpContextKeys.AppId] as string
                ?? http.Request.Headers[TroveStandardHeaders.AppId].FirstOrDefault()
                ?? TroveStandardHeaders.HrAppId;
            var busId = http.Items[TrovesuiteHttpContextKeys.BusId] as string
                ?? http.Request.Headers[TroveStandardHeaders.BusId].FirstOrDefault()
                ?? DevFallback(_localDev.BusId);
            var locId = http.Items[TrovesuiteHttpContextKeys.LocId] as string
                ?? http.Request.Headers[TroveStandardHeaders.LocId].FirstOrDefault()
                ?? DevFallback(_localDev.LocId);
            var userId = http.Items[TrovesuiteHttpContextKeys.UserId] as string;
            var permissions = http.Items[TrovesuiteHttpContextKeys.Permissions] as IReadOnlyList<string>
                ?? [];

            return new TenantContext
            {
                TenantId = tenantId,
                OrgId = orgId,
                AppId = appId,
                BusId = busId,
                LocId = locId,
                UserId = userId,
                Permissions = permissions,
            };
        }
    }

    private string DevFallback(string configured) =>
        _environment.IsDevelopment() ? configured : string.Empty;
}
