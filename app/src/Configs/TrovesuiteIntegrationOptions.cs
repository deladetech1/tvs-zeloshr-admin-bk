namespace ZelosHR.Api.Configs;

/// <summary>ZelosHR-specific settings for Trovesuite.Package middleware (not part of the NuGet package).</summary>
public class TrovesuiteIntegrationOptions
{
    public const string SectionName = "TrovesuiteIntegration";

    /// <summary>When false, requests use X-Tenant-Id / X-Org-Id headers (demo mode). When true, Bearer JWT is required.</summary>
    public bool RequireAuthentication { get; set; }

    /// <summary>Permission required for HR Admin routes (optional). Example: permission-zeloshr-admin.</summary>
    public string? HrAdminPermission { get; set; }
}
