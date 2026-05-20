namespace ZelosHR.Api.Shared.Tenant;

public sealed class TenantContext
{
    public const string DefaultTenantId = "demo-tenant";
    public const string DefaultOrgId = "demo-org";

    public string TenantId { get; init; } = DefaultTenantId;
    public string OrgId { get; init; } = DefaultOrgId;
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
                ?? http.Request.Headers["X-Tenant-Id"].FirstOrDefault()
                ?? TenantContext.DefaultTenantId;
            var orgId = http.Items[TrovesuiteHttpContextKeys.OrgId] as string
                ?? http.Request.Headers["X-Org-Id"].FirstOrDefault()
                ?? TenantContext.DefaultOrgId;
            var userId = http.Items[TrovesuiteHttpContextKeys.UserId] as string;
            var permissions = http.Items[TrovesuiteHttpContextKeys.Permissions] as IReadOnlyList<string>
                ?? [];

            return new TenantContext
            {
                TenantId = tenantId,
                OrgId = orgId,
                UserId = userId,
                Permissions = permissions,
            };
        }
    }
}
