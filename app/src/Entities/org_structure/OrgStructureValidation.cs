using System.Text.RegularExpressions;

namespace ZelosHR.Api.Entities.OrgStructure;

internal static partial class OrgStructureValidation
{
    [GeneratedRegex("^[A-Za-z]{2}$")]
    private static partial Regex IsoCountryCodePattern();

    internal static string? NormalizeOptionalCountryCode(string? countryCode)
    {
        if (string.IsNullOrWhiteSpace(countryCode))
            return null;

        var trimmed = countryCode.Trim().ToUpperInvariant();
        return IsoCountryCodePattern().IsMatch(trimmed) ? trimmed : null;
    }

    internal static Dictionary<string, string>? ValidateBranchLocationFields(
        string? city,
        string? region,
        string? countryCode)
    {
        var errors = new Dictionary<string, string>();

        if (city is { Length: > 100 })
            errors["city"] = "City must be at most 100 characters.";

        if (region is { Length: > 100 })
            errors["region"] = "Region must be at most 100 characters.";

        if (!string.IsNullOrWhiteSpace(countryCode)
            && NormalizeOptionalCountryCode(countryCode) is null)
        {
            errors["country_code"] = "Country code must be a 2-letter ISO code (e.g. GH).";
        }

        return errors.Count == 0 ? null : errors;
    }
}
