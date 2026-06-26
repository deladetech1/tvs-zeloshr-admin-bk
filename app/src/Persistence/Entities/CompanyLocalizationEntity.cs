namespace ZelosHR.Api.Persistence.Entities;

/// <summary>Maps to zeloshr.zhr_company_localization.</summary>
public sealed class CompanyLocalizationEntity
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = default!;
    public string OrgId { get; set; } = default!;
    public string TimeZone { get; set; } = default!;
    public string CurrencyId { get; set; } = default!;
    public string DateFormat { get; set; } = default!;
    public string NumberFormat { get; set; } = default!;
    public string FirstDayOfWeek { get; set; } = default!;
    public string YearStartMonth { get; set; } = default!;
    public int YearStartDay { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}
