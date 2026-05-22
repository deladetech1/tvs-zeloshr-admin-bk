using ZelosHR.Api.Persistence.Entities;

namespace ZelosHR.Api.Entities.Employees;

/// <summary>Identity payload for core_platform.cp_users (not stored on zhr_employees after sync).</summary>
public sealed record CpUserIdentityData(
    string FullName,
    string Email,
    string Contact,
    string? Gender,
    string? Dob,
    string? Address,
    string? ProfilePic);

public static class CpUserIdentityMapper
{
    public static CpUserIdentityData FromEmployeeAndRequest(EmployeeEntity employee, CreateEmployeeRequest? dto = null)
    {
        var fullName = !string.IsNullOrWhiteSpace(dto?.FullName)
            ? dto.FullName.Trim()
            : !string.IsNullOrWhiteSpace(employee.FullName)
                ? employee.FullName.Trim()
                : string.Empty;

        var email = (dto?.WorkEmail ?? employee.WorkEmail)?.Trim().ToLowerInvariant() ?? string.Empty;
        var contact = (dto?.Phone ?? employee.Phone ?? employee.PersonalPhone)?.Trim() ?? string.Empty;

        return new CpUserIdentityData(
            fullName,
            email,
            contact,
            NormalizeGender(dto?.Gender ?? employee.Gender),
            FormatDob(dto?.DateOfBirth ?? employee.DateOfBirth),
            BuildAddress(dto, employee),
            employee.ProfilePhotoUrl);
    }

    public static string? BuildAddress(CreateEmployeeRequest? dto, EmployeeEntity employee)
    {
        var parts = new[]
        {
            dto?.ResidentialAddress ?? employee.ResidentialAddress,
            dto?.GpsAddress ?? employee.GhanaPostGps,
            dto?.State ?? employee.State,
        }.Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p!.Trim()).ToArray();

        return parts.Length == 0 ? null : string.Join(", ", parts);
    }

    public static string? NormalizeGender(string? gender)
    {
        if (string.IsNullOrWhiteSpace(gender))
            return null;

        var g = gender.Trim();
        if (g.Equals("MALE", StringComparison.OrdinalIgnoreCase)
            || g.Equals("M", StringComparison.OrdinalIgnoreCase))
            return "MALE";
        if (g.Equals("FEMALE", StringComparison.OrdinalIgnoreCase)
            || g.Equals("F", StringComparison.OrdinalIgnoreCase))
            return "FEMALE";
        if (g.Contains("female", StringComparison.OrdinalIgnoreCase)
            || g.Contains("woman", StringComparison.OrdinalIgnoreCase))
            return "FEMALE";
        if (g.Contains("male", StringComparison.OrdinalIgnoreCase))
            return "MALE";
        return null;
    }

    public static string? FormatDob(DateOnly? dob) => dob?.ToString("yyyy-MM-dd");
}
