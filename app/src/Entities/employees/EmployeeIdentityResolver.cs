using ZelosHR.Api.Persistence.Entities;
using ZelosHR.Api.Shared.Formatting;

namespace ZelosHR.Api.Entities.Employees;

/// <summary>Resolves display identity from cp_users when linked; falls back to zhr_employees draft/legacy fields.</summary>
internal static class EmployeeIdentityResolver
{
    public static string ResolveFullName(EmployeeEntity employee, CpUserDto? platformUser) =>
        platformUser?.FullName
        ?? (!string.IsNullOrWhiteSpace(employee.FullName)
            ? employee.FullName
            : NameFormatting.ResolveFullName(employee.FullName, employee.FirstName, employee.MiddleName, employee.LastName));

    public static string ResolveFullName(EmployeeLeaveContextRow row, CpUserDto? platformUser) =>
        platformUser?.FullName
        ?? (!string.IsNullOrWhiteSpace(row.FullName) ? row.FullName : row.EmployeeCode);

    public static string? ResolveWorkEmail(EmployeeEntity employee, CpUserDto? platformUser) =>
        platformUser?.Email ?? employee.WorkEmail;

    public static string? ResolvePhone(EmployeeEntity employee, CpUserDto? platformUser) =>
        platformUser?.Phone ?? employee.Phone ?? employee.PersonalPhone;

    /// <summary>Stored reference: document id from file upload, or legacy https URL — not a presigned display URL.</summary>
    public static string? ResolveStoredProfileReference(EmployeeEntity employee, CpUserDto? platformUser) =>
        platformUser?.ProfilePic ?? employee.ProfilePhotoUrl;

    public static string? ResolveProfilePhoto(EmployeeEntity employee, CpUserDto? platformUser) =>
        ResolveStoredProfileReference(employee, platformUser);

    public static (string First, string Last) ResolveNameParts(EmployeeEntity employee, CpUserDto? platformUser)
    {
        var full = ResolveFullName(employee, platformUser);
        if (string.IsNullOrWhiteSpace(full))
            return (employee.FirstName ?? string.Empty, employee.LastName ?? string.Empty);

        var parts = full.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1)
            return (parts[0], string.Empty);

        return (parts[0], string.Join(' ', parts.Skip(1)));
    }
}
