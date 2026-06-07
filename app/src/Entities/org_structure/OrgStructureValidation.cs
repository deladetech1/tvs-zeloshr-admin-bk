namespace ZelosHR.Api.Entities.OrgStructure;

internal static class OrgStructureValidation
{
    internal static Dictionary<string, string>? ValidateBranchFields(
        string? address,
        string? country,
        string? description)
    {
        var errors = new Dictionary<string, string>();

        if (address is { Length: > 500 })
            errors["address"] = "Address must be at most 500 characters.";

        if (country is { Length: > 100 })
            errors["country"] = "Country must be at most 100 characters.";

        if (description is { Length: > 500 })
            errors["description"] = "Description must be at most 500 characters.";

        return errors.Count == 0 ? null : errors;
    }

    internal static Dictionary<string, string>? ValidateDepartmentDescription(string? description)
    {
        if (description is { Length: > 500 })
            return new Dictionary<string, string>
            {
                ["description"] = "Description must be at most 500 characters.",
            };

        return null;
    }

    internal static Dictionary<string, string>? ValidateHeadcountCapacity(int? headcountCapacity)
    {
        if (headcountCapacity is < 0)
            return new Dictionary<string, string>
            {
                ["headcount_capacity"] = "Headcount capacity must be zero or greater.",
            };

        return null;
    }
}
