namespace ZelosHR.Api.Configs;

/// <summary>Configurable demo tenant/org/bus/loc for local Swagger and dev fallbacks.</summary>
public sealed class LocalDevelopmentOptions
{
    public const string SectionName = "LocalDevelopment";

    public string TenantId { get; set; } = LocalDevelopmentDefaults.TenantId;
    public string OrgId { get; set; } = LocalDevelopmentDefaults.OrgId;
    public string BusId { get; set; } = LocalDevelopmentDefaults.BusId;
    public string LocId { get; set; } = LocalDevelopmentDefaults.LocId;
    public string DemoAdminUserId { get; set; } = LocalDevelopmentDefaults.DemoAdminUserId;
}
