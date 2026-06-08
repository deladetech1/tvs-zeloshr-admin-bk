using Microsoft.EntityFrameworkCore;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Persistence.Entities;

namespace ZelosHR.Api.Entities.Employees;

/// <summary>Query parameters for the employee directory list (matches frontend filters).</summary>
public sealed class EmployeeDirectoryQuery
{
    public string? Search { get; init; }
    public Guid? DepartmentId { get; init; }
    public Guid? BranchId { get; init; }

    [SwaggerAllowedValues(typeof(EmployeeFieldOptions), nameof(EmployeeFieldOptions.EmploymentTypes))]
    public string? EmploymentType { get; init; }

    [SwaggerAllowedValues(typeof(EmployeeFieldOptions), nameof(EmployeeFieldOptions.LifecycleStatesAll))]
    public string? LifecycleState { get; init; }

    public string? WorkLocation { get; init; }

    [SwaggerAllowedValues(typeof(EmployeeFieldOptions), nameof(EmployeeFieldOptions.DirectoryEmploymentStatuses),
        Description = "Legacy exact match on employment_status. Prefer engagement + work_states.")]
    public string? Status { get; init; }

    /// <summary>
    /// Primary workforce relationship — e.g. <c>active</c>, <c>pre_hire</c>, <c>terminated</c>.
    /// Combines with <see cref="WorkStates"/> (AND): <c>engagement=active&amp;work_states=probation</c>.
    /// </summary>
    [SwaggerAllowedValues(typeof(EmployeeFieldOptions), nameof(EmployeeFieldOptions.Engagements))]
    public string? Engagement { get; init; }

    /// <summary>
    /// Overlay work states (OR within this list, AND with engagement): <c>probation</c>, <c>on_leave</c>.
    /// Repeat query param: <c>work_states=probation&amp;work_states=on_leave</c>.
    /// </summary>
    [SwaggerAllowedValues(typeof(EmployeeFieldOptions), nameof(EmployeeFieldOptions.WorkStates))]
    public string[]? WorkStates { get; init; }

    [SwaggerAllowedValues(typeof(EmployeeFieldOptions), nameof(EmployeeFieldOptions.DirectorySortBy))]
    public string SortBy { get; init; } = "name";

    [SwaggerAllowedValues(typeof(EmployeeFieldOptions), nameof(EmployeeFieldOptions.SortOrder))]
    public string SortOrder { get; init; } = "asc";
    public int Page { get; init; } = 1;
    public int Size { get; init; } = 10;

    /// <summary>When false, Terminated/Resigned are excluded unless status filter is set.</summary>
    public bool IncludeInactive { get; init; }

    /// <summary>Employment start on or after this date (uses start_date or employment_start_date).</summary>
    public DateOnly? StartDate { get; init; }

    /// <summary>Employment start on or before this date (uses start_date or employment_start_date).</summary>
    public DateOnly? EndDate { get; init; }
}

public static class EmployeeDirectoryQueryBuilder
{
    public const int MinimumSearchLength = 3;

    public static (string WhereClause, Dictionary<string, object?> Parameters) Build(
        EmployeeDirectoryQuery query,
        string employeesTable,
        string tenantId,
        string orgId)
    {
        var conditions = new List<string>
        {
            "e.tenant_id = @TenantId",
            "e.org_id = @OrgId",
            "e.is_deleted = FALSE",
        };

        var parameters = new Dictionary<string, object?>
        {
            ["TenantId"] = tenantId,
            ["OrgId"] = orgId,
        };

        if (!string.IsNullOrWhiteSpace(query.Search) && query.Search.Trim().Length >= MinimumSearchLength)
        {
            conditions.Add(
                """
                (
                    e.first_name ILIKE @Search OR
                    e.last_name ILIKE @Search OR
                    e.employee_code ILIKE @Search OR
                    COALESCE(e.job_title, '') ILIKE @Search OR
                    TRIM(CONCAT(e.first_name, ' ', COALESCE(e.middle_name || ' ', ''), e.last_name)) ILIKE @Search
                )
                """);
            parameters["Search"] = $"%{query.Search.Trim()}%";
        }

        if (query.DepartmentId.HasValue)
        {
            conditions.Add("e.department_id = @DepartmentId");
            parameters["DepartmentId"] = query.DepartmentId.Value;
        }

        if (query.BranchId.HasValue)
        {
            conditions.Add("e.branch_id = @BranchId");
            parameters["BranchId"] = query.BranchId.Value;
        }

        if (!string.IsNullOrWhiteSpace(query.EmploymentType))
        {
            conditions.Add("e.employment_type = @EmploymentType");
            parameters["EmploymentType"] = query.EmploymentType.Trim();
        }

        if (!string.IsNullOrWhiteSpace(query.LifecycleState))
        {
            conditions.Add("e.lifecycle_state = @LifecycleState");
            parameters["LifecycleState"] = query.LifecycleState.Trim();
        }

        if (!string.IsNullOrWhiteSpace(query.WorkLocation))
        {
            conditions.Add("e.work_location ILIKE @WorkLocation");
            parameters["WorkLocation"] = $"%{query.WorkLocation.Trim()}%";
        }

        if (!string.IsNullOrWhiteSpace(query.Status)
            && EmployeeStatusFilter.NormalizeEngagement(query.Engagement) is null
            && EmployeeStatusFilter.ParseWorkStates(query.WorkStates).Count == 0)
        {
            conditions.Add("e.employment_status = @EmploymentStatus");
            parameters["EmploymentStatus"] = query.Status.Trim();
        }
        else if (!query.IncludeInactive
                 && EmployeeStatusFilter.NormalizeEngagement(query.Engagement) is null
                 && EmployeeStatusFilter.ParseWorkStates(query.WorkStates).Count == 0)
        {
            conditions.Add("e.employment_status NOT IN ('Terminated', 'Resigned')");
        }

        AppendCompositeStatusSql(conditions, parameters, query);

        var whereClause = string.Join(" AND ", conditions);
        return (whereClause, parameters);
    }

    public static string BuildOrderBy(string? sortBy, string? sortOrder)
    {
        var direction = string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase)
            ? "DESC"
            : "ASC";

        var column = sortBy?.Trim().ToLowerInvariant() switch
        {
            "employeecode" or "employeeid" or "id" => "e.employee_code",
            "department" => "d.name",
            "status" => "e.employment_status",
            "employmenttype" or "type" => "e.employment_type",
            "jobtitle" => "e.job_title",
            _ => "e.last_name, e.first_name",
        };

        return column.Contains(',')
            ? $"ORDER BY {column} {direction}"
            : $"ORDER BY {column} {direction} NULLS LAST";
    }

    public static IQueryable<EmployeeEntity> ApplyFilters(
        IQueryable<EmployeeEntity> query, EmployeeDirectoryQuery directoryQuery)
    {
        if (!string.IsNullOrWhiteSpace(directoryQuery.Search)
            && directoryQuery.Search.Trim().Length >= MinimumSearchLength)
        {
            var pattern = $"%{directoryQuery.Search.Trim()}%";
            query = query.Where(e =>
                EF.Functions.ILike(e.FullName, pattern)
                || (e.FirstName != null && EF.Functions.ILike(e.FirstName, pattern))
                || (e.LastName != null && EF.Functions.ILike(e.LastName, pattern))
                || EF.Functions.ILike(e.EmployeeCode, pattern)
                || (e.JobTitle != null && EF.Functions.ILike(e.JobTitle, pattern)));
        }

        if (directoryQuery.DepartmentId.HasValue)
            query = query.Where(e => e.DepartmentId == directoryQuery.DepartmentId);

        if (directoryQuery.BranchId.HasValue)
            query = query.Where(e => e.BranchId == directoryQuery.BranchId);

        if (!string.IsNullOrWhiteSpace(directoryQuery.EmploymentType))
            query = query.Where(e => e.EmploymentType == directoryQuery.EmploymentType.Trim());

        if (!string.IsNullOrWhiteSpace(directoryQuery.LifecycleState))
            query = query.Where(e => e.LifecycleState == directoryQuery.LifecycleState.Trim());

        if (!string.IsNullOrWhiteSpace(directoryQuery.WorkLocation))
        {
            var loc = directoryQuery.WorkLocation.Trim();
            query = query.Where(e => e.WorkLocation != null && EF.Functions.ILike(e.WorkLocation, $"%{loc}%"));
        }

        if (!string.IsNullOrWhiteSpace(directoryQuery.Status)
            && EmployeeStatusFilter.NormalizeEngagement(directoryQuery.Engagement) is null
            && EmployeeStatusFilter.ParseWorkStates(directoryQuery.WorkStates).Count == 0)
        {
            var status = directoryQuery.Status.Trim();
            query = query.Where(e => e.EmploymentStatus == status);
        }
        else if (!directoryQuery.IncludeInactive
                 && EmployeeStatusFilter.NormalizeEngagement(directoryQuery.Engagement) is null
                 && EmployeeStatusFilter.ParseWorkStates(directoryQuery.WorkStates).Count == 0)
        {
            query = query.Where(e => e.EmploymentStatus != EmploymentStatusValues.Terminated
                && e.EmploymentStatus != EmploymentStatusValues.Resigned);
        }

        query = ApplyCompositeStatusFilters(query, directoryQuery);

        if (directoryQuery.StartDate.HasValue)
        {
            var from = directoryQuery.StartDate.Value;
            query = query.Where(e =>
                (e.StartDate ?? e.EmploymentStartDate) != null
                && (e.StartDate ?? e.EmploymentStartDate)! >= from);
        }

        if (directoryQuery.EndDate.HasValue)
        {
            var to = directoryQuery.EndDate.Value;
            query = query.Where(e =>
                (e.StartDate ?? e.EmploymentStartDate) != null
                && (e.StartDate ?? e.EmploymentStartDate)! <= to);
        }

        return query;
    }

    private static IQueryable<EmployeeEntity> ApplyCompositeStatusFilters(
        IQueryable<EmployeeEntity> query, EmployeeDirectoryQuery directoryQuery)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var engagement = EmployeeStatusFilter.NormalizeEngagement(directoryQuery.Engagement);
        if (engagement is not null)
            query = query.Where(e => MatchesEngagement(e, engagement, today));

        var workStates = EmployeeStatusFilter.ParseWorkStates(directoryQuery.WorkStates);
        if (workStates.Count == 0)
            return query;

        var wantsProbation = workStates.Contains(EmployeeWorkStateValues.Probation, StringComparer.OrdinalIgnoreCase);
        var wantsOnLeave = workStates.Contains(EmployeeWorkStateValues.OnLeave, StringComparer.OrdinalIgnoreCase);
        return query.Where(e =>
            (wantsProbation && IsOnProbation(e, today))
            || (wantsOnLeave && IsOnLeave(e)));
    }

    private static void AppendCompositeStatusSql(
        List<string> conditions,
        Dictionary<string, object?> parameters,
        EmployeeDirectoryQuery query)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        parameters["StatusFilterToday"] = today;

        var engagement = EmployeeStatusFilter.NormalizeEngagement(query.Engagement);
        if (engagement is not null)
            conditions.Add(BuildEngagementSql(engagement));

        var workStates = EmployeeStatusFilter.ParseWorkStates(query.WorkStates);
        if (workStates.Count == 0)
            return;

        var wantsProbation = workStates.Contains(EmployeeWorkStateValues.Probation, StringComparer.OrdinalIgnoreCase);
        var wantsOnLeave = workStates.Contains(EmployeeWorkStateValues.OnLeave, StringComparer.OrdinalIgnoreCase);
        conditions.Add(BuildWorkStatesSql(wantsProbation, wantsOnLeave));
    }

    private static string BuildEngagementSql(string engagement) => engagement switch
    {
        EmployeeEngagementValues.Draft =>
            "(e.is_draft = TRUE OR e.employment_status = 'Draft')",
        EmployeeEngagementValues.PreHire =>
            "(e.lifecycle_state = 'Pre-hire' OR e.employment_status = 'Pre-hire')",
        EmployeeEngagementValues.Active =>
            """
            (
                e.is_draft = FALSE
                AND COALESCE(e.lifecycle_state, '') NOT IN ('Pre-hire', 'Terminated', 'Resigned', 'Suspended')
                AND COALESCE(e.employment_status, '') NOT IN ('Draft', 'Pre-hire', 'Terminated', 'Resigned', 'Suspended', 'Inactive')
                AND (
                    e.lifecycle_state IN ('Active', 'On Leave')
                    OR e.employment_status IN ('Active', 'Probation', 'On Leave')
                )
            )
            """,
        EmployeeEngagementValues.Suspended =>
            "(e.lifecycle_state = 'Suspended' OR e.employment_status = 'Suspended')",
        EmployeeEngagementValues.Terminated =>
            "(e.lifecycle_state = 'Terminated' OR e.employment_status = 'Terminated')",
        EmployeeEngagementValues.Resigned =>
            "(e.lifecycle_state = 'Resigned' OR e.employment_status = 'Resigned')",
        EmployeeEngagementValues.Inactive =>
            "e.employment_status = 'Inactive'",
        _ => "TRUE",
    };

    private static string BuildWorkStatesSql(bool wantsProbation, bool wantsOnLeave)
    {
        if (wantsProbation && wantsOnLeave)
        {
            return """
                   (
                       e.employment_status = 'Probation'
                       OR (e.probation_end_date IS NOT NULL AND e.probation_end_date >= @StatusFilterToday AND e.lifecycle_state = 'Active')
                       OR e.lifecycle_state = 'On Leave'
                       OR e.employment_status = 'On Leave'
                   )
                   """;
        }

        if (wantsProbation)
        {
            return """
                   (
                       e.employment_status = 'Probation'
                       OR (e.probation_end_date IS NOT NULL AND e.probation_end_date >= @StatusFilterToday AND e.lifecycle_state = 'Active')
                   )
                   """;
        }

        return "(e.lifecycle_state = 'On Leave' OR e.employment_status = 'On Leave')";
    }

    private static bool MatchesEngagement(EmployeeEntity e, string engagement, DateOnly today) =>
        engagement switch
        {
            EmployeeEngagementValues.Draft => e.IsDraft
                || string.Equals(e.EmploymentStatus, EmploymentStatusValues.Draft, StringComparison.OrdinalIgnoreCase),
            EmployeeEngagementValues.PreHire =>
                string.Equals(e.LifecycleState, EmployeeLifecycleStates.PreHire, StringComparison.OrdinalIgnoreCase)
                || string.Equals(e.EmploymentStatus, EmploymentStatusValues.PreHire, StringComparison.OrdinalIgnoreCase),
            EmployeeEngagementValues.Active => EmployeeStatusFilter.IsActiveEngagement(e),
            EmployeeEngagementValues.Suspended =>
                string.Equals(e.LifecycleState, EmployeeLifecycleStates.Suspended, StringComparison.OrdinalIgnoreCase)
                || string.Equals(e.EmploymentStatus, EmploymentStatusValues.Suspended, StringComparison.OrdinalIgnoreCase),
            EmployeeEngagementValues.Terminated =>
                string.Equals(e.LifecycleState, EmployeeLifecycleStates.Terminated, StringComparison.OrdinalIgnoreCase)
                || string.Equals(e.EmploymentStatus, EmploymentStatusValues.Terminated, StringComparison.OrdinalIgnoreCase),
            EmployeeEngagementValues.Resigned =>
                string.Equals(e.LifecycleState, EmployeeLifecycleStates.Resigned, StringComparison.OrdinalIgnoreCase)
                || string.Equals(e.EmploymentStatus, EmploymentStatusValues.Resigned, StringComparison.OrdinalIgnoreCase),
            EmployeeEngagementValues.Inactive =>
                string.Equals(e.EmploymentStatus, EmploymentStatusValues.Inactive, StringComparison.OrdinalIgnoreCase),
            _ => true,
        };

    private static bool IsOnProbation(EmployeeEntity e, DateOnly today) =>
        EmployeeStatusFilter.IsOnProbation(e, today);

    private static bool IsOnLeave(EmployeeEntity e) =>
        EmployeeStatusFilter.IsOnLeave(e);

    public static IQueryable<EmployeeEntity> ApplySort(
        IQueryable<EmployeeEntity> query, string? sortBy, string? sortOrder)
    {
        var desc = string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase);
        var key = sortBy?.Trim().ToLowerInvariant();

        return key switch
        {
            "employeecode" or "employeeid" or "id" => desc
                ? query.OrderByDescending(e => e.EmployeeCode)
                : query.OrderBy(e => e.EmployeeCode),
            "department" => desc
                ? query.OrderByDescending(e => e.Department!.Name)
                : query.OrderBy(e => e.Department!.Name),
            "status" => desc
                ? query.OrderByDescending(e => e.EmploymentStatus)
                : query.OrderBy(e => e.EmploymentStatus),
            "employmenttype" or "type" => desc
                ? query.OrderByDescending(e => e.EmploymentType)
                : query.OrderBy(e => e.EmploymentType),
            "jobtitle" => desc
                ? query.OrderByDescending(e => e.JobTitle)
                : query.OrderBy(e => e.JobTitle),
            _ => desc
                ? query.OrderByDescending(e => e.LastName).ThenByDescending(e => e.FirstName)
                : query.OrderBy(e => e.LastName).ThenBy(e => e.FirstName),
        };
    }
}
