namespace ZelosHR.Api.Entities.Countries;

/// <summary>Platform country catalog — stable ids for pickers (DB stores ISO alpha-2 code).</summary>
internal static class CountryCatalog
{
    internal static readonly IReadOnlyList<CountryRecord> All =
    [
        new("ctr_gh", "GH", "Ghana"),
        new("ctr_ke", "KE", "Kenya"),
        new("ctr_ng", "NG", "Nigeria"),
        new("ctr_za", "ZA", "South Africa"),
        new("ctr_ci", "CI", "Côte d'Ivoire"),
        new("ctr_sn", "SN", "Senegal"),
        new("ctr_us", "US", "United States"),
        new("ctr_gb", "GB", "United Kingdom"),
        new("ctr_fr", "FR", "France"),
        new("ctr_de", "DE", "Germany"),
        new("ctr_in", "IN", "India"),
        new("ctr_cn", "CN", "China"),
    ];

    internal static bool TryGetById(string? countryId, out CountryRecord country)
    {
        country = default!;
        if (string.IsNullOrWhiteSpace(countryId))
            return false;

        var normalized = countryId.Trim();
        foreach (var entry in All)
        {
            if (entry.Id.Equals(normalized, StringComparison.OrdinalIgnoreCase))
            {
                country = entry;
                return true;
            }
        }

        return false;
    }

    internal static bool TryGetByCode(string? countryCode, out CountryRecord country)
    {
        country = default!;
        if (string.IsNullOrWhiteSpace(countryCode))
            return false;

        var normalized = countryCode.Trim().ToUpperInvariant();
        foreach (var entry in All)
        {
            if (entry.Code.Equals(normalized, StringComparison.Ordinal))
            {
                country = entry;
                return true;
            }
        }

        return false;
    }

    internal static GetCountrySimpleReadDto ToReadDto(CountryRecord country) => new()
    {
        Id = country.Id,
        Name = country.Name,
        Code = country.Code,
    };

    internal static (string CountryId, string CountryCode, string CountryName) ResolveHolidayCountryFields(
        string storedCountryCode)
    {
        if (TryGetByCode(storedCountryCode, out var country))
            return (country.Id, country.Code, country.Name);

        var code = storedCountryCode.Trim().ToUpperInvariant();
        return (code, code, code);
    }

    internal sealed record CountryRecord(string Id, string Code, string Name);
}
