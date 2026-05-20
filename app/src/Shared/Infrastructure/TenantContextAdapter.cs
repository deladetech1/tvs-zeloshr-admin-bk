using ZelosHR.Api.Shared.Abstractions;
using ZelosHR.Api.Shared.Tenant;

namespace ZelosHR.Api.Shared.Infrastructure;

/// <summary>Maps <see cref="ITenantContextAccessor"/> to <see cref="ITenantContext"/> for services/repositories.</summary>
public sealed class TenantContextAdapter(ITenantContextAccessor accessor) : ITenantContext
{
    public string TenantId => accessor.Current.TenantId;
    public string OrgId => accessor.Current.OrgId;
    public string? UserId => accessor.Current.UserId;
    public IReadOnlyList<string> Permissions => accessor.Current.Permissions;
}
