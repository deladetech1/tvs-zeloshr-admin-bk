using ZelosHR.Api.Persistence.Entities;

namespace ZelosHR.Api.Entities.Employees;

/// <summary>
/// Composite employee status: engagement (workforce relationship) plus optional work-state overlays
/// (probation, on leave) that can combine — e.g. Active + on probation.
/// </summary>
public static class EmployeeStatusFilter
{
    public static readonly IReadOnlyList<string> Engagements =
    [
        EmployeeEngagementValues.Active,
        EmployeeEngagementValues.PreHire,
        EmployeeEngagementValues.Suspended,
        EmployeeEngagementValues.Terminated,
        EmployeeEngagementValues.Resigned,
        EmployeeEngagementValues.Inactive,
        EmployeeEngagementValues.Draft,
    ];

    public static readonly IReadOnlyList<string> WorkStates =
    [
        EmployeeWorkStateValues.Probation,
        EmployeeWorkStateValues.OnLeave,
    ];

    /// <summary>Simple filter commands for <c>GET /employees/list?status=</c>.</summary>
    public static readonly IReadOnlyList<string> ListStatusFilters =
    [
        EmployeeEngagementValues.Active,
        EmployeeWorkStateValues.Probation,
        EmployeeWorkStateValues.OnLeave,
        EmployeeEngagementValues.PreHire,
        EmployeeEngagementValues.Draft,
        EmployeeEngagementValues.Suspended,
        EmployeeEngagementValues.Terminated,
        EmployeeEngagementValues.Resigned,
        EmployeeEngagementValues.Inactive,
    ];

    public sealed record StatusOrBranch(string? Engagement, IReadOnlyList<string> WorkStates);

    public sealed record ResolvedStatusFilters(
        string? ExactEmploymentStatus,
        string? Engagement,
        IReadOnlyList<string> WorkStates,
        IReadOnlyList<StatusOrBranch> OrBranches)
    {
        public bool HasCompositeFilter =>
            Engagement is not null || WorkStates.Count > 0 || OrBranches.Count > 0;
    }

    public static ResolvedStatusFilters ResolveDirectoryFilters(
        string? exactEmploymentStatus,
        string? statusFilter,
        string? engagement,
        IEnumerable<string>? workStates)
    {
        if (!string.IsNullOrWhiteSpace(exactEmploymentStatus))
            return new ResolvedStatusFilters(exactEmploymentStatus.Trim(), null, [], []);

        var resolvedEngagement = NormalizeEngagement(engagement);
        var resolvedWorkStates = ParseWorkStates(workStates);

        if (resolvedEngagement is null
            && resolvedWorkStates.Count == 0
            && !string.IsNullOrWhiteSpace(statusFilter))
        {
            if (statusFilter.Contains(',', StringComparison.Ordinal))
            {
                if (IsDefaultWorkforceTabFilter(statusFilter))
                {
                    return new ResolvedStatusFilters(null, null, [], DefaultWorkforceTabOrBranches);
                }

                var branches = ParseStatusFilterBranches(statusFilter);
                if (branches.Count > 0)
                    return new ResolvedStatusFilters(null, null, [], branches);
            }
            else
            {
                var (cmdEngagement, cmdWorkStates) = ResolveStatusCommand(statusFilter);
                resolvedEngagement = cmdEngagement ?? resolvedEngagement;
                if (cmdWorkStates.Count > 0)
                    resolvedWorkStates = cmdWorkStates;
            }
        }

        return new ResolvedStatusFilters(null, resolvedEngagement, resolvedWorkStates, []);
    }

    /// <summary>
    /// Default directory tab: active + probation + on leave + pre-hire collapses to active ∪ pre_hire
    /// (probation and on leave are subsets of active engagement).
    /// </summary>
    public static readonly IReadOnlyList<StatusOrBranch> DefaultWorkforceTabOrBranches =
    [
        new(EmployeeEngagementValues.Active, []),
        new(EmployeeEngagementValues.PreHire, []),
    ];

    public static bool IsDefaultWorkforceTabFilter(string statusFilter)
    {
        var tokens = statusFilter
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(token => token.Trim().Replace('-', '_').Replace(' ', '_').ToLowerInvariant())
            .ToHashSet(StringComparer.Ordinal);

        return tokens.SetEquals(new HashSet<string>(StringComparer.Ordinal)
        {
            "active",
            "probation",
            "on_leave",
            "pre_hire",
        });
    }

    public static IReadOnlyList<StatusOrBranch> ParseStatusFilterBranches(string statusFilter) =>
        statusFilter
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(ResolveStatusCommand)
            .Where(b => b.Engagement is not null || b.WorkStates.Count > 0)
            .Select(b => new StatusOrBranch(b.Engagement, b.WorkStates))
            .ToList();

    public static (string? Engagement, IReadOnlyList<string> WorkStates) ResolveStatusCommand(string? statusFilter)
    {
        if (string.IsNullOrWhiteSpace(statusFilter))
            return (null, []);

        var normalized = statusFilter.Trim().Replace('-', '_').Replace(' ', '_').ToLowerInvariant();
        return normalized switch
        {
            "active" => (EmployeeEngagementValues.Active, []),
            "probation" or "on_probation" => (EmployeeEngagementValues.Active, [EmployeeWorkStateValues.Probation]),
            "on_leave" or "leave" => (EmployeeEngagementValues.Active, [EmployeeWorkStateValues.OnLeave]),
            "pre_hire" or "prehire" => (EmployeeEngagementValues.PreHire, []),
            "draft" => (EmployeeEngagementValues.Draft, []),
            "suspended" => (EmployeeEngagementValues.Suspended, []),
            "terminated" => (EmployeeEngagementValues.Terminated, []),
            "resigned" => (EmployeeEngagementValues.Resigned, []),
            "inactive" => (EmployeeEngagementValues.Inactive, []),
            _ => (null, []),
        };
    }

    public static IReadOnlyList<string> ParseWorkStates(IEnumerable<string>? values)
    {
        if (values is null)
            return [];

        return values
            .SelectMany(v => v.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Select(NormalizeWorkState)
            .Where(v => v is not null)
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static string? NormalizeEngagement(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalized = value.Trim().Replace('-', '_').Replace(' ', '_').ToLowerInvariant();
        return normalized switch
        {
            "active" => EmployeeEngagementValues.Active,
            "pre_hire" or "prehire" => EmployeeEngagementValues.PreHire,
            "suspended" => EmployeeEngagementValues.Suspended,
            "terminated" => EmployeeEngagementValues.Terminated,
            "resigned" => EmployeeEngagementValues.Resigned,
            "inactive" => EmployeeEngagementValues.Inactive,
            "draft" => EmployeeEngagementValues.Draft,
            _ => null,
        };
    }

    public static string? NormalizeWorkState(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalized = value.Trim().Replace('-', '_').Replace(' ', '_').ToLowerInvariant();
        return normalized switch
        {
            "probation" or "on_probation" => EmployeeWorkStateValues.Probation,
            "on_leave" or "leave" => EmployeeWorkStateValues.OnLeave,
            _ => null,
        };
    }

    public static bool MatchesEngagement(EmployeeEntity employee, string engagement, DateOnly today)
    {
        return engagement switch
        {
            EmployeeEngagementValues.Draft => employee.IsDraft
                || string.Equals(employee.EmploymentStatus, EmploymentStatusValues.Draft, StringComparison.OrdinalIgnoreCase),
            EmployeeEngagementValues.PreHire => IsPreHire(employee),
            EmployeeEngagementValues.Active => IsActiveEngagement(employee),
            EmployeeEngagementValues.Suspended => IsSuspended(employee),
            EmployeeEngagementValues.Terminated => IsTerminated(employee),
            EmployeeEngagementValues.Resigned => IsResigned(employee),
            EmployeeEngagementValues.Inactive => string.Equals(
                employee.EmploymentStatus, EmploymentStatusValues.Inactive, StringComparison.OrdinalIgnoreCase),
            _ => false,
        };
    }

    public static bool MatchesWorkState(EmployeeEntity employee, string workState, DateOnly today) =>
        workState switch
        {
            EmployeeWorkStateValues.Probation => IsOnProbation(employee, today),
            EmployeeWorkStateValues.OnLeave => IsOnLeave(employee),
            _ => false,
        };

    public static string ResolveEngagement(EmployeeEntity employee)
    {
        if (employee.IsDraft
            || string.Equals(employee.EmploymentStatus, EmploymentStatusValues.Draft, StringComparison.OrdinalIgnoreCase))
            return EmployeeEngagementValues.Draft;

        if (IsPreHire(employee))
            return EmployeeEngagementValues.PreHire;

        if (IsTerminated(employee))
            return EmployeeEngagementValues.Terminated;

        if (IsResigned(employee))
            return EmployeeEngagementValues.Resigned;

        if (IsSuspended(employee))
            return EmployeeEngagementValues.Suspended;

        if (string.Equals(employee.EmploymentStatus, EmploymentStatusValues.Inactive, StringComparison.OrdinalIgnoreCase))
            return EmployeeEngagementValues.Inactive;

        return EmployeeEngagementValues.Active;
    }

    public static IReadOnlyList<string> ResolveWorkStates(EmployeeEntity employee, DateOnly today)
    {
        var states = new List<string>(2);
        if (IsOnProbation(employee, today))
            states.Add(EmployeeWorkStateValues.Probation);
        if (IsOnLeave(employee))
            states.Add(EmployeeWorkStateValues.OnLeave);
        return states;
    }

    internal static bool IsActiveEngagement(EmployeeEntity employee) =>
        !employee.IsDraft
        && !IsPreHire(employee)
        && !IsTerminated(employee)
        && !IsResigned(employee)
        && !IsSuspended(employee)
        && !string.Equals(employee.EmploymentStatus, EmploymentStatusValues.Inactive, StringComparison.OrdinalIgnoreCase)
        && (
            string.Equals(employee.LifecycleState, EmployeeLifecycleStates.Active, StringComparison.OrdinalIgnoreCase)
            || string.Equals(employee.LifecycleState, EmployeeLifecycleStates.OnLeave, StringComparison.OrdinalIgnoreCase)
            || string.Equals(employee.EmploymentStatus, EmploymentStatusValues.Active, StringComparison.OrdinalIgnoreCase)
            || string.Equals(employee.EmploymentStatus, EmploymentStatusValues.Probation, StringComparison.OrdinalIgnoreCase)
            || string.Equals(employee.EmploymentStatus, EmploymentStatusValues.OnLeave, StringComparison.OrdinalIgnoreCase));

    internal static bool IsOnProbation(EmployeeEntity employee, DateOnly today) =>
        string.Equals(employee.EmploymentStatus, EmploymentStatusValues.Probation, StringComparison.OrdinalIgnoreCase)
        || (employee.ProbationEndDate is not null
            && employee.ProbationEndDate.Value >= today
            && string.Equals(employee.LifecycleState, EmployeeLifecycleStates.Active, StringComparison.OrdinalIgnoreCase));

    internal static bool IsOnLeave(EmployeeEntity employee) =>
        string.Equals(employee.LifecycleState, EmployeeLifecycleStates.OnLeave, StringComparison.OrdinalIgnoreCase)
        || string.Equals(employee.EmploymentStatus, EmploymentStatusValues.OnLeave, StringComparison.OrdinalIgnoreCase);

    private static bool IsPreHire(EmployeeEntity employee) =>
        string.Equals(employee.LifecycleState, EmployeeLifecycleStates.PreHire, StringComparison.OrdinalIgnoreCase)
        || string.Equals(employee.EmploymentStatus, EmploymentStatusValues.PreHire, StringComparison.OrdinalIgnoreCase);

    private static bool IsSuspended(EmployeeEntity employee) =>
        string.Equals(employee.LifecycleState, EmployeeLifecycleStates.Suspended, StringComparison.OrdinalIgnoreCase)
        || string.Equals(employee.EmploymentStatus, EmploymentStatusValues.Suspended, StringComparison.OrdinalIgnoreCase);

    private static bool IsTerminated(EmployeeEntity employee) =>
        string.Equals(employee.LifecycleState, EmployeeLifecycleStates.Terminated, StringComparison.OrdinalIgnoreCase)
        || string.Equals(employee.EmploymentStatus, EmploymentStatusValues.Terminated, StringComparison.OrdinalIgnoreCase);

    private static bool IsResigned(EmployeeEntity employee) =>
        string.Equals(employee.LifecycleState, EmployeeLifecycleStates.Resigned, StringComparison.OrdinalIgnoreCase)
        || string.Equals(employee.EmploymentStatus, EmploymentStatusValues.Resigned, StringComparison.OrdinalIgnoreCase);
}

public static class EmployeeEngagementValues
{
    public const string Active = "active";
    public const string PreHire = "pre_hire";
    public const string Suspended = "suspended";
    public const string Terminated = "terminated";
    public const string Resigned = "resigned";
    public const string Inactive = "inactive";
    public const string Draft = "draft";
}

public static class EmployeeWorkStateValues
{
    public const string Probation = "probation";
    public const string OnLeave = "on_leave";
}
