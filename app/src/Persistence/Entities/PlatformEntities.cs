using ZelosHR.Api.Shared.Constants;

namespace ZelosHR.Api.Persistence.Entities;

/// <summary>Writable map to core_platform.cp_users (identity source of truth).</summary>
public sealed class CpUserEntity
{
    public string Id { get; set; } = default!;
    public string TenantId { get; set; } = default!;
    public string Fullname { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string Contact { get; set; } = default!;
    public bool IsOwner { get; set; }
    public bool CanLogin { get; set; } = true;
    public string DeleteStatus { get; set; } = CorePlatformConstants.DeleteStatus.NotDeleted;
    public bool IsActive { get; set; } = true;
    public string? Gender { get; set; }
    public string? Dob { get; set; }
    public string? Address { get; set; }
    public string? ProfilePic { get; set; }
    public string? Description { get; set; }
    public string? CreatedBy { get; set; }
    public string? Cdate { get; set; }
    public string? Ctime { get; set; }
    public DateTimeOffset? Cdatetime { get; set; }
}

/// <summary>core_platform.cp_members — core-platform directory users (not HR-only shells).</summary>
public sealed class CpMemberEntity
{
    public string Id { get; set; } = default!;
    public string TenantId { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public string DeleteStatus { get; set; } = CorePlatformConstants.DeleteStatus.NotDeleted;
    public bool IsActive { get; set; } = true;
}

/// <summary>core_platform.cp_login_settings — required for platform auth.</summary>
public sealed class CpLoginSettingsEntity
{
    public string Id { get; set; } = default!;
    public string TenantId { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public bool IsSuspended { get; set; }
    public bool IsMultiFactorEnabled { get; set; }
    public bool IsLoginBefore { get; set; }
    public bool CanAlwaysLogin { get; set; } = true;
    public string DeleteStatus { get; set; } = CorePlatformConstants.DeleteStatus.NotDeleted;
    public bool IsActive { get; set; } = true;
}

/// <summary>core_platform.cp_business_app_locations — org + business + app + location tuple.</summary>
public sealed class CpBusinessAppLocationEntity
{
    public string Id { get; set; } = default!;
    public string TenantId { get; set; } = default!;
    public string? OrgId { get; set; }
    public string? BusId { get; set; }
    public string? AppId { get; set; }
    public string? LocId { get; set; }
    public string DeleteStatus { get; set; } = CorePlatformConstants.DeleteStatus.NotDeleted;
    public bool IsActive { get; set; } = true;
}

/// <summary>core_platform.cp_user_locations — tenant/org/app context for the user.</summary>
public sealed class CpUserLocationEntity
{
    public string Id { get; set; } = default!;
    public string TenantId { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public string? OrgId { get; set; }
    public string? BusId { get; set; }
    public string? AppId { get; set; }
    public string? BusAppLocId { get; set; }
    public string DeleteStatus { get; set; } = CorePlatformConstants.DeleteStatus.NotDeleted;
    public bool IsActive { get; set; } = true;
}

/// <summary>human_resource.hr_employees — platform HR membership (user_id only).</summary>
public sealed class HrEmployeeEntity
{
    public string Id { get; set; } = default!;
    public string TenantId { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public string DeleteStatus { get; set; } = CorePlatformConstants.DeleteStatus.NotDeleted;
    public bool IsActive { get; set; } = true;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? Cdatetime { get; set; }
}

/// <summary>core_platform.cp_currencies — tenant-scoped currency catalog (seeded).</summary>
public sealed class CpCurrencyEntity
{
    public string Id { get; set; } = default!;
    public string TenantId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Code { get; set; } = default!;
    public string Symbol { get; set; } = default!;
    public int DecimalPlaces { get; set; } = 2;
    public string CurrencyPosition { get; set; } = "before";
    public bool IsDefault { get; set; }
    public string DeleteStatus { get; set; } = CorePlatformConstants.DeleteStatus.NotDeleted;
    public bool IsActive { get; set; } = true;
}
