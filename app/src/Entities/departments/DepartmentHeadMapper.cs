using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Entities.Files;
using ZelosHR.Api.Shared.Formatting;

namespace ZelosHR.Api.Entities.Departments;

internal static class DepartmentHeadMapper
{
    internal static DepartmentHeadDto? Map(
        DepartmentListRow row,
        IReadOnlyDictionary<string, CpUserDto> platformUsers,
        DocumentReadDto? profileUrl = null)
    {
        if (row.HeadId is null)
            return null;

        platformUsers.TryGetValue(row.HeadUserId ?? string.Empty, out var cp);
        var fullName = ResolveHeadFullName(row, cp);

        return new DepartmentHeadDto
        {
            EmployeeId = row.HeadId.Value.ToString(),
            FullName = fullName,
            JobTitle = row.HeadJobTitle,
            ProfileUrl = profileUrl,
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
}
