namespace ZelosHR.Api.Configs;

/// <summary>ZelosHR-specific settings for Trovesuite.Package middleware (not part of the NuGet package).</summary>
public class TrovesuiteIntegrationOptions
{
    public const string SectionName = "TrovesuiteIntegration";

    /// <summary>When false, JWT is not validated against core_platform (claims still read from Bearer). When true, full Trovesuite auth runs.</summary>
    public bool RequireAuthentication { get; set; }

    /// <summary>When true, every /api/v1/* request must include app-id, authorization, bus-id, loc-id, org-id.</summary>
    public bool RequireStandardHeaders { get; set; } = true;

    /// <summary>
    /// When true, validates org/bus/loc/app headers against core_platform and user cp_user_locations.
    /// </summary>
    public bool ValidatePlatformContext { get; set; } = true;

    /// <summary>Permission required for HR Admin routes (optional). Example: permission-zeloshr-admin.</summary>
    public string? HrAdminPermission { get; set; }
}
