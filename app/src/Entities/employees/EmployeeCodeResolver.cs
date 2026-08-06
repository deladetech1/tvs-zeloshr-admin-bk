namespace ZelosHR.Api.Entities.Employees;

/// <summary>
/// Resolves display employee code from persisted system/custom columns.
/// Display: <c>employee_code_custom ?? employee_code_system</c>.
/// </summary>
internal static class EmployeeCodeResolver
{
    internal static string Display(string? system, string? custom)
    {
        if (!string.IsNullOrWhiteSpace(custom))
            return custom.Trim();

        return string.IsNullOrWhiteSpace(system) ? string.Empty : system.Trim();
    }

    internal static string? NormalizeCustom(string? custom) =>
        string.IsNullOrWhiteSpace(custom) ? null : custom.Trim();
}
