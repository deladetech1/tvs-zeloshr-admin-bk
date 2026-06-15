using System.Text.Json;

namespace ZelosHR.Api.Entities.Leave;

public static class LeaveAccrualMethods
{
    public const string FrontLoaded = "front_loaded";
    public const string Monthly = "monthly";
}

public static class LeaveTypePolicy
{
    public static string? SerializeEmploymentTypes(IReadOnlyList<string>? values)
    {
        if (values is null || values.Count == 0)
            return null;

        var normalized = values
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        return normalized.Count == 0 ? null : JsonSerializer.Serialize(normalized);
    }

    public static IReadOnlyList<string>? DeserializeEmploymentTypes(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            var values = JsonSerializer.Deserialize<List<string>>(json);
            if (values is null || values.Count == 0)
                return null;

            return values
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => v.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static Dictionary<string, string>? ValidateCreate(CreateLeaveTypeDto data) =>
        ValidateSave(data);

    public static Dictionary<string, string>? ValidateSave(CreateLeaveTypeDto data)
    {
        var errors = new Dictionary<string, string>();

        if (string.IsNullOrWhiteSpace(data.Name))
            errors["name"] = "Leave type name is required.";

        if (data.DefaultEntitledDays is null)
            errors["default_entitled_days"] = "Entitlement (days) is required.";

        if (string.IsNullOrWhiteSpace(data.AccrualMethod))
            errors["accrual_method"] = "Accrual method is required.";
        else if (!LeaveFieldOptions.AccrualMethods.Contains(data.AccrualMethod.Trim(), StringComparer.OrdinalIgnoreCase))
            errors["accrual_method"] = $"Accrual method must be one of: {string.Join(", ", LeaveFieldOptions.AccrualMethods)}.";

        if (data.AppliesToEmploymentTypes is null || data.AppliesToEmploymentTypes.Count == 0)
            errors["applies_to_employment_types"] = "Select at least one employment type, or choose All.";
        else
            ValidateEmploymentTypes(data.AppliesToEmploymentTypes, errors);

        if (data.MinNoticeWorkingDays is < 0)
            errors["min_notice_working_days"] = "Minimum notice cannot be negative.";

        if (data.MaxConsecutiveDays is < 1)
            errors["max_consecutive_days"] = "Max consecutive days must be at least 1 when provided.";

        return errors.Count == 0 ? null : errors;
    }

    private static void ValidateEmploymentTypes(
        IReadOnlyList<string> values,
        Dictionary<string, string> errors)
    {
        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
                continue;

            if (!LeaveFieldOptions.AppliesToEmploymentTypes.Contains(value.Trim(), StringComparer.OrdinalIgnoreCase))
            {
                errors["applies_to_employment_types"] =
                    $"Invalid employment type '{value.Trim()}'. Allowed: {string.Join(", ", LeaveFieldOptions.AppliesToEmploymentTypes)}.";
                return;
            }
        }
    }

    public static string NormalizeAccrualMethod(string value) =>
        value.Trim().Equals(LeaveAccrualMethods.Monthly, StringComparison.OrdinalIgnoreCase)
            ? LeaveAccrualMethods.Monthly
            : LeaveAccrualMethods.FrontLoaded;

    public static IReadOnlyList<string>? NormalizeEmploymentTypes(IReadOnlyList<string>? values)
    {
        if (values is null || values.Count == 0)
            return null;

        return values
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v =>
            {
                var trimmed = v.Trim();
                return LeaveFieldOptions.AppliesToEmploymentTypes.First(
                    allowed => allowed.Equals(trimmed, StringComparison.OrdinalIgnoreCase));
            })
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
