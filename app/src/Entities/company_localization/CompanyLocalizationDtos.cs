namespace ZelosHR.Api.Entities.CompanyLocalization;

public sealed record CompanyLocalizationReadDto
{
    public required string Id { get; init; }
    public required string TimeZone { get; init; }
    public required string CurrencyId { get; init; }
    public required string DateFormat { get; init; }
    public required string NumberFormat { get; init; }
    public required string FirstDayOfWeek { get; init; }
    public required string YearStartMonth { get; init; }
    public required int YearStartDay { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public string? CreatedById { get; init; }
    public string? UpdatedById { get; init; }
    public string? CreatedBy { get; init; }
    public string? UpdatedBy { get; init; }
}

public sealed class CreateCompanyLocalizationDto
{
    /// <summary>IANA time zone id, e.g. "Africa/Accra".</summary>
    public string? TimeZone { get; set; }

    /// <summary>Currency id from <c>GET /api/v1/currencies/list</c>.</summary>
    public string? CurrencyId { get; set; }

    public string? DateFormat { get; set; }
    public string? NumberFormat { get; set; }
    public string? FirstDayOfWeek { get; set; }
    public string? YearStartMonth { get; set; }
    public int? YearStartDay { get; set; }
}

/// <summary>
/// Same shape as <see cref="CreateCompanyLocalizationDto"/>, plus <see cref="Id"/> — a full
/// replacement, not a partial patch. Every field is required, same as create.
/// </summary>
public sealed class UpdateCompanyLocalizationDto
{
    /// <summary>Must match the settings row's current id (from <c>GET /get</c>).</summary>
    public string? Id { get; set; }
    public string? TimeZone { get; set; }
    public string? CurrencyId { get; set; }
    public string? DateFormat { get; set; }
    public string? NumberFormat { get; set; }
    public string? FirstDayOfWeek { get; set; }
    public string? YearStartMonth { get; set; }
    public int? YearStartDay { get; set; }
}
