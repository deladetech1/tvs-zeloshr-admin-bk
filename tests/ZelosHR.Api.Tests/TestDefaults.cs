using ZelosHR.Api.Configs;
using ZelosHR.Api.Shared.Tenant;

namespace ZelosHR.Api.Tests;

/// <summary>Shared demo tenant/org values for unit tests (aligned with local seed).</summary>
public static class TestDefaults
{
    public const string TenantId = LocalDevelopmentDefaults.TenantId;
    public const string OrgId = LocalDevelopmentDefaults.OrgId;
    public const string BusId = LocalDevelopmentDefaults.BusId;
    public const string LocId = LocalDevelopmentDefaults.LocId;
    public const string AppId = TroveStandardHeaders.HrAppId;
}
