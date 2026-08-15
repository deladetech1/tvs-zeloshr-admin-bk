namespace ZelosHR.Api.Persistence.Entities;

/// <summary>core_platform.cp_otps — one-time activation tokens for HR-provisioned employees.</summary>
public sealed class CpOtpEntity
{
    public string Id { get; set; } = default!;
    public string TenantId { get; set; } = default!;
    public string? OtpCode { get; set; }
    public bool IsActive { get; set; }
    public string? Description { get; set; }
    public string Email { get; set; } = default!;
    public string? Contact { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
    public string? Cdate { get; set; }
    public string? Ctime { get; set; }
    public DateTimeOffset? Cdatetime { get; set; }
}

/// <summary>core_platform.cp_password_policies — tenant password rules.</summary>
public sealed class CpPasswordPolicyEntity
{
    public string Id { get; set; } = default!;
    public string TenantId { get; set; } = default!;
    public bool EnforcePasswordPolicy { get; set; }
    public int MinLength { get; set; } = 8;
    public bool RequireUppercase { get; set; } = true;
    public bool RequireLowercase { get; set; } = true;
    public bool RequireNumbers { get; set; } = true;
    public bool RequireSpecialChars { get; set; } = true;
    public string? SpecialCharsList { get; set; } = "!@#$%^&*()_+-=[]{}|;:,.<>?";
    public bool IsActive { get; set; } = true;
}
