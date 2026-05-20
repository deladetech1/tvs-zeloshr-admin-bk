namespace ZelosHR.Api.Entities.Employees;

internal static class EmployeeValidator
{
    public static Dictionary<string, string> ValidateCreate(EmployeeWriteDto data)
    {
        var errors = new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(data.FirstName))
            errors["firstName"] = "First name is required.";
        if (string.IsNullOrWhiteSpace(data.LastName))
            errors["lastName"] = "Last name is required.";
        if (string.IsNullOrWhiteSpace(data.GhanaCardNumber))
            errors["ghanaCardNumber"] = "Ghana Card number is required.";
        else if (!IsValidGhanaCardFormat(data.GhanaCardNumber))
            errors["ghanaCardNumber"] = "Ghana Card number format is invalid.";
        if (data.DateOfBirth.HasValue && data.DateOfBirth > DateOnly.FromDateTime(DateTime.UtcNow))
            errors["dateOfBirth"] = "Date of birth cannot be in the future.";
        return errors;
    }

    public static Dictionary<string, string> ValidateUpdate(EmployeeWriteDto data)
    {
        var errors = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(data.GhanaCardNumber) && !IsValidGhanaCardFormat(data.GhanaCardNumber))
            errors["ghanaCardNumber"] = "Ghana Card number format is invalid.";
        return errors;
    }

    private static bool IsValidGhanaCardFormat(string value)
    {
        var normalized = EmployeeMappingExtensions.NormalizeGhanaCard(value);
        return normalized.Length >= 5 && normalized.Length <= 50;
    }
}
