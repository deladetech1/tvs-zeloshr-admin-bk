namespace ZelosHR.Api.Shared.Tenant;

/// <summary>
/// Trove platform request headers (exact names — lowercase with hyphen).
/// Required on every <c>/api/v1/*</c> request.
/// </summary>
public static class TroveStandardHeaders
{
    public const string AppId = "app-id";
    public const string Authorization = "authorization";
    public const string BusId = "bus-id";
    public const string LocId = "loc-id";
    public const string OrgId = "org-id";

    /// <summary>HR application id expected by ZelosHR API.</summary>
    public const string HrAppId = "app-hr";

    /// <summary>Legacy dev headers (fallback only).</summary>
    public const string LegacyTenantId = "X-Tenant-Id";
    public const string LegacyOrgId = "X-Org-Id";

    public static readonly string[] RequiredForApi =
    [
        AppId,
        Authorization,
        BusId,
        LocId,
        OrgId,
    ];
}
