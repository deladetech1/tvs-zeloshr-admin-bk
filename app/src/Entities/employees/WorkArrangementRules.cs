namespace ZelosHR.Api.Entities.Employees;

/// <summary>
/// Normalizes work arrangement values and enforces branch_id rules
/// (departments and branches meet only on the employee record).
/// </summary>
internal static class WorkArrangementRules
{
    internal static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var key = value.Trim().Replace('-', '_').ToLowerInvariant();
        return key switch
        {
            "onsite" or "on_site" => "on_site",
            "remote" => "remote",
            "hybrid" => "hybrid",
            "field" => "field",
            _ => key,
        };
    }

    internal static bool RequiresBranch(string? normalized) =>
        normalized is "on_site" or "field";

    internal static bool ProhibitsBranch(string? normalized) =>
        normalized is "remote";

    internal static Dictionary<string, string>? ValidateBranchForArrangement(
        string? workArrangement,
        Guid? branchId)
    {
        var normalized = Normalize(workArrangement);
        if (normalized is null)
            return null;

        if (RequiresBranch(normalized) && branchId is null)
        {
            return new Dictionary<string, string>
            {
                ["employment.branch_id"] =
                    "Branch is required when work_arrangement is on_site or field.",
            };
        }

        if (ProhibitsBranch(normalized) && branchId is not null)
        {
            return new Dictionary<string, string>
            {
                ["employment.branch_id"] =
                    "Remote employees must not have a branch. Omit branch_id or set work_arrangement to hybrid or on_site.",
            };
        }

        return null;
    }
}
