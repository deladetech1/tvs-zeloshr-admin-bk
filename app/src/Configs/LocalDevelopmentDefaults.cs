namespace ZelosHR.Api.Configs;

/// <summary>
/// Swagger / local Trove header defaults (format: <c>org_</c> / <c>bus_</c> / <c>loc_</c> prefixes).
/// Must match rows in your database (<c>cp_*</c> context + JWT <c>tenant_id</c>) — insert locally or use a shared dev DB.
/// </summary>
public static class LocalDevelopmentDefaults
{
    public const string TenantId = "demo-tenant";

    public const string OrgId =
        "org_bcf5a0951f5ed22448dc5262e641e428caa3638d38b94cfa3b79c13d38a";

    public const string BusId =
        "bus_5d929457b0ea7e6d55c5da25c8cfb38aeef0573658121bf5399f6f1e64d";

    public const string LocId =
        "loc_c79fd9a5c53a8eaa82805e63a84da112387743c5dcdff7f7b254c02302c";

    /// <summary><c>cp_business_apps.id</c> for HR on the demo business.</summary>
    public const string BusinessAppId =
        "ba_5d929457b0ea7e6d55c5da25c8cfb38aeef0573658121bf5399f6f1e64d";

    /// <summary><c>cp_business_app_locations.id</c> for demo org + bus + loc + app-hr.</summary>
    public const string BusAppLocationId =
        "bal_c79fd9a5c53a8eaa82805e63a84da112387743c5dcdff7f7b254c02302c";

    public const string DemoAdminUserId = "u1000001-0000-4000-8000-000000000001";
}
