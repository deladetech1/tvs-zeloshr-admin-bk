namespace ZelosHR.Api.Shared.Tenant;

public sealed class TenantContext
{
    public const string DefaultTenantId = "demo-tenant";
    public const string DefaultOrgId = "demo-org";
    public const string DefaultBusId = "bus_demo";
    public const string DefaultLocId = "loc_demo";

    public string TenantId { get; init; } = DefaultTenantId;
    public string OrgId { get; init; } = DefaultOrgId;
    public string AppId { get; init; } = TroveStandardHeaders.HrAppId;
    public string BusId { get; init; } = DefaultBusId;
    public string LocId { get; init; } = DefaultLocId;
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

    public TenantContextAccessor(IHttpContextAccessor httpContextAccessor) =>
        _httpContextAccessor = httpContextAccessor;

    public TenantContext Current
    {
        get
        {
            var http = _httpContextAccessor.HttpContext;
            if (http is null)
                return new TenantContext();

            var tenantId = http.Items[TrovesuiteHttpContextKeys.TenantId] as string
                ?? http.Request.Headers[TroveStandardHeaders.LegacyTenantId].FirstOrDefault()
                ?? TenantContext.DefaultTenantId;
            var orgId = http.Items[TrovesuiteHttpContextKeys.OrgId] as string
                ?? http.Request.Headers[TroveStandardHeaders.OrgId].FirstOrDefault()
                ?? http.Request.Headers[TroveStandardHeaders.LegacyOrgId].FirstOrDefault()
                ?? TenantContext.DefaultOrgId;
            var appId = http.Items[TrovesuiteHttpContextKeys.AppId] as string
                ?? http.Request.Headers[TroveStandardHeaders.AppId].FirstOrDefault()
                ?? TroveStandardHeaders.HrAppId;
            var busId = http.Items[TrovesuiteHttpContextKeys.BusId] as string
                ?? http.Request.Headers[TroveStandardHeaders.BusId].FirstOrDefault()
                ?? TenantContext.DefaultBusId;
            var locId = http.Items[TrovesuiteHttpContextKeys.LocId] as string
                ?? http.Request.Headers[TroveStandardHeaders.LocId].FirstOrDefault()
                ?? TenantContext.DefaultLocId;
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
}
