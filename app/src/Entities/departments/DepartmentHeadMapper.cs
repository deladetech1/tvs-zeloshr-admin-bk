using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Shared.Formatting;

namespace ZelosHR.Api.Entities.Departments;

internal static class DepartmentHeadMapper
{
    internal static DepartmentHeadDto? Map(
        DepartmentListRow row,
        IReadOnlyDictionary<string, CpUserDto> platformUsers)
    {
        if (row.HeadId is null)
            return null;

        platformUsers.TryGetValue(row.HeadUserId ?? string.Empty, out var cp);
        var fullName = ResolveHeadFullName(row, cp);
        var (firstName, lastName) = ResolveHeadNameParts(row, cp, fullName);

        return new DepartmentHeadDto
        {
            EmployeeId = row.HeadId.Value.ToString(),
            FullName = fullName,
            JobTitle = row.HeadJobTitle,
            Initials = NameFormatting.BuildInitials(firstName, lastName),
        };
    }

    internal static string ResolveHeadFullName(DepartmentListRow row, CpUserDto? platformUser)
    {
        if (!string.IsNullOrWhiteSpace(platformUser?.FullName))
            return platformUser.FullName.Trim();

        return NameFormatting.ResolveFullName(
            row.HeadFullName,
            row.HeadFirstName,
            middleName: null,
            row.HeadLastName);
    }

    private static (string FirstName, string LastName) ResolveHeadNameParts(
        DepartmentListRow row,
        CpUserDto? platformUser,
        string fullName)
    {
        if (!string.IsNullOrWhiteSpace(row.HeadFirstName) || !string.IsNullOrWhiteSpace(row.HeadLastName))
        {
            return (
                row.HeadFirstName?.Trim() ?? string.Empty,
                row.HeadLastName?.Trim() ?? string.Empty);
        }

        if (!string.IsNullOrWhiteSpace(platformUser?.FullName))
            return SplitFullName(platformUser.FullName);

        return SplitFullName(fullName);
    }

    private static (string FirstName, string LastName) SplitFullName(string? fullName)
    {
        var parts = fullName?.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            ?? [];
        if (parts.Length == 0)
            return ("?", "?");
        if (parts.Length == 1)
            return (parts[0], parts[0]);

        return (parts[0], parts[^1]);
    }
}
