namespace ZelosHR.Api.Entities.Employees;

/// <summary>
/// Normalizes work arrangement values and rejects inconsistent branch_id pairings
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

    internal static bool ProhibitsBranch(string? normalized) =>
        normalized is "remote";

    internal static Dictionary<string, string>? ValidateBranchForArrangement(
        string? workArrangement,
        Guid? branchId)
    {
        var normalized = Normalize(workArrangement);
        if (normalized is null)
            return null;

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
