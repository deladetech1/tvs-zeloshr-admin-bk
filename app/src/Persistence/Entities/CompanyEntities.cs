namespace ZelosHR.Api.Persistence.Entities;

/// <summary>Maps to zeloshr.zhr_company_profile.</summary>
public sealed class CompanyProfileEntity
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = default!;
    public string OrgId { get; set; } = default!;
    public string LegalName { get; set; } = default!;
    public string? TradingName { get; set; }
    public string? Industry { get; set; }
    public string? CompanySize { get; set; }
    public string? BusinessRegistrationNumber { get; set; }
    public string? Tin { get; set; }
    public string? PrimaryWorkCountry { get; set; }
    public string? CompanyEmail { get; set; }
    public string? Website { get; set; }
    public string? LogoDocumentId { get; set; }
    public string? BannerDocumentId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}

/// <summary>Maps to zeloshr.zhr_company_offices.</summary>
public sealed class CompanyOfficeEntity
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = default!;
    public string OrgId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Country { get; set; }
    public string? City { get; set; }
    public string? Phone { get; set; }
    public bool IsHeadOffice { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}
