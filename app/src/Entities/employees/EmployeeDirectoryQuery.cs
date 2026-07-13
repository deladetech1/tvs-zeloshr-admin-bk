using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Persistence.Entities;
using ZelosHR.Api.Shared.Constants;

namespace ZelosHR.Api.Entities.Employees;

/// <summary>Query parameters for the employee directory list (matches frontend filters).</summary>
public sealed class EmployeeDirectoryQuery
{
    [FromQuery(Name = PlatformQueryParams.Search)]
    public string? Search { get; init; }

    [FromQuery(Name = PlatformQueryParams.DepartmentId)]
    public Guid? DepartmentId { get; init; }

    [FromQuery(Name = PlatformQueryParams.BranchId)]
    public Guid? BranchId { get; init; }

    [FromQuery(Name = PlatformQueryParams.EmploymentType)]
    [SwaggerAllowedValues(typeof(EmployeeFieldOptions), nameof(EmployeeFieldOptions.EmploymentTypes))]
    public string? EmploymentType { get; init; }

    [FromQuery(Name = PlatformQueryParams.LifecycleState)]
    [SwaggerAllowedValues(typeof(EmployeeFieldOptions), nameof(EmployeeFieldOptions.LifecycleStatesAll))]
    public string? LifecycleState { get; init; }

    [FromQuery(Name = PlatformQueryParams.WorkLocation)]
    public string? WorkLocation { get; init; }

    [FromQuery(Name = PlatformQueryParams.EmploymentStatus)]
    [SwaggerAllowedValues(typeof(EmployeeFieldOptions), nameof(EmployeeFieldOptions.EmploymentStatuses),
        Description = "Exact match on stored employment_status (Draft, Active, Probation, …).")]
    public string? Status { get; init; }

    /// <summary>Smart filter using simple commands — e.g. <c>active</c>, <c>probation</c>, <c>on_leave</c>.</summary>
    [FromQuery(Name = PlatformQueryParams.StatusFilter)]
    [SwaggerAllowedValues(typeof(EmployeeFieldOptions), nameof(EmployeeFieldOptions.ListStatusFilters))]
    public string? StatusFilter { get; init; }

    /// <summary>Advanced composite filter (internal / tests). Prefer <see cref="StatusFilter"/> on list API.</summary>
    public string? Engagement { get; init; }

    /// <summary>Advanced overlay filter (internal / tests). Prefer <see cref="StatusFilter"/> on list API.</summary>
    public string[]? WorkStates { get; init; }

    [FromQuery(Name = PlatformQueryParams.SortBy)]
    [SwaggerAllowedValues(typeof(EmployeeFieldOptions), nameof(EmployeeFieldOptions.DirectorySortBy))]
    public string SortBy { get; init; } = "name";

    [FromQuery(Name = PlatformQueryParams.SortOrder)]
    [SwaggerAllowedValues(typeof(EmployeeFieldOptions), nameof(EmployeeFieldOptions.SortOrder))]
    public string SortOrder { get; init; } = "asc";

    [FromQuery(Name = PlatformQueryParams.Page)]
    public int Page { get; init; } = 1;

    [FromQuery(Name = PlatformQueryParams.Size)]
    public int Size { get; init; } = 10;

    /// <summary>When false, Terminated/Resigned are excluded unless status filter is set.</summary>
    [FromQuery(Name = PlatformQueryParams.IncludeInactive)]
    public bool IncludeInactive { get; init; }

    /// <summary>Employment start on or after this date (uses start_date or employment_start_date).</summary>
    [FromQuery(Name = PlatformQueryParams.StartDate)]
    public DateOnly? StartDate { get; init; }

    /// <summary>Employment start on or before this date (uses start_date or employment_start_date).</summary>
    [FromQuery(Name = PlatformQueryParams.EndDate)]
    public DateOnly? EndDate { get; init; }

    /// <summary>When true, only employees with at least one active non-draft direct report (<c>reports_to_id</c>).</summary>
    [FromQuery(Name = PlatformQueryParams.IsLineManager)]
    public bool? IsLineManager { get; init; }

    /// <summary>When true, only employees who head at least one non-archived department.</summary>
    [FromQuery(Name = PlatformQueryParams.IsHeadOfDepartment)]
    public bool? IsHeadOfDepartment { get; init; }
}

public static class EmployeeDirectoryQueryBuilder
{
    public const int MinimumSearchLength = 3;

    /// <summary>Join required when <see cref="Build"/> search filter is used (links platform identity).</summary>
    public const string PlatformUserSearchJoin =
        "LEFT JOIN core_platform.cp_users cu ON cu.id = e.user_id AND cu.tenant_id = e.tenant_id";

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
                    e.middle_name ILIKE @Search OR
                    COALESCE(e.full_name, '') ILIKE @Search OR
                    e.employee_code ILIKE @Search OR
                    COALESCE(e.job_title, '') ILIKE @Search OR
                    COALESCE(e.work_email, '') ILIKE @Search OR
                    COALESCE(e.personal_email, '') ILIKE @Search OR
                    TRIM(CONCAT(e.first_name, ' ', COALESCE(e.middle_name || ' ', ''), e.last_name)) ILIKE @Search OR
                    cu.fullname ILIKE @Search OR
                    cu.email ILIKE @Search OR
                    cu.contact ILIKE @Search
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

        var statusFilters = EmployeeStatusFilter.ResolveDirectoryFilters(
            query.Status, query.StatusFilter, query.Engagement, query.WorkStates);

        if (!string.IsNullOrWhiteSpace(statusFilters.ExactEmploymentStatus))
        {
            conditions.Add("e.employment_status = @EmploymentStatus");
            parameters["EmploymentStatus"] = statusFilters.ExactEmploymentStatus;
        }
        else if (!query.IncludeInactive && !statusFilters.HasCompositeFilter)
        {
            conditions.Add("e.employment_status NOT IN ('Terminated', 'Resigned')");
        }

        AppendCompositeStatusSql(conditions, parameters, statusFilters);

        EmployeeRoleFlags.AppendSqlFilters(
            conditions,
            query.IsLineManager,
            query.IsHeadOfDepartment,
            employeesTable,
            "zeloshr.zhr_departments");

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

    internal static bool HasActiveSearch(EmployeeDirectoryQuery directoryQuery) =>
        !string.IsNullOrWhiteSpace(directoryQuery.Search)
        && directoryQuery.Search.Trim().Length >= MinimumSearchLength;

    public static IQueryable<EmployeeEntity> ApplyFilters(
        IQueryable<EmployeeEntity> query,
        EmployeeDirectoryQuery directoryQuery,
        IQueryable<CpUserEntity>? platformUsers = null,
        string? tenantId = null,
        string? orgId = null,
        IQueryable<EmployeeEntity>? roleLookupEmployees = null,
        IQueryable<DepartmentEntity>? roleLookupDepartments = null)
    {
        if (HasActiveSearch(directoryQuery))
        {
            var pattern = $"%{directoryQuery.Search!.Trim()}%";
            query = ApplySearchFilter(query, platformUsers, tenantId, pattern);
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

        var statusFilters = EmployeeStatusFilter.ResolveDirectoryFilters(
            directoryQuery.Status,
            directoryQuery.StatusFilter,
            directoryQuery.Engagement,
            directoryQuery.WorkStates);

        if (!string.IsNullOrWhiteSpace(statusFilters.ExactEmploymentStatus))
        {
            var exact = statusFilters.ExactEmploymentStatus;
            query = query.Where(e => e.EmploymentStatus == exact);
        }
        else if (!directoryQuery.IncludeInactive
                 && (!statusFilters.HasCompositeFilter || HasActiveSearch(directoryQuery)))
        {
            query = query.Where(e => e.EmploymentStatus != EmploymentStatusValues.Terminated
                && e.EmploymentStatus != EmploymentStatusValues.Resigned);
        }

        if (!HasActiveSearch(directoryQuery))
            query = ApplyCompositeStatusFilters(query, statusFilters);

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

        if (roleLookupEmployees is not null
            && roleLookupDepartments is not null
            && !string.IsNullOrWhiteSpace(tenantId)
            && !string.IsNullOrWhiteSpace(orgId)
            && (directoryQuery.IsLineManager is not null || directoryQuery.IsHeadOfDepartment is not null))
        {
            query = EmployeeRoleFlags.ApplyFilters(
                query,
                directoryQuery.IsLineManager,
                directoryQuery.IsHeadOfDepartment,
                roleLookupEmployees,
                roleLookupDepartments,
                tenantId,
                orgId);
        }

        return query;
    }

    private static IQueryable<EmployeeEntity> ApplySearchFilter(
        IQueryable<EmployeeEntity> query,
        IQueryable<CpUserEntity>? platformUsers,
        string? tenantId,
        string pattern)
    {
        if (platformUsers is not null && !string.IsNullOrWhiteSpace(tenantId))
        {
            var platformUserIds = platformUsers
                .Where(u => u.TenantId == tenantId
                    && (EF.Functions.ILike(u.Fullname, pattern)
                        || EF.Functions.ILike(u.Email, pattern)
                        || EF.Functions.ILike(u.Contact, pattern)))
                .Select(u => u.Id);

            return query.Where(e =>
                (e.FullName != "" && EF.Functions.ILike(e.FullName, pattern))
                || (e.FirstName != null && EF.Functions.ILike(e.FirstName, pattern))
                || (e.LastName != null && EF.Functions.ILike(e.LastName, pattern))
                || (e.MiddleName != null && EF.Functions.ILike(e.MiddleName, pattern))
                || EF.Functions.ILike(e.EmployeeCode, pattern)
                || (e.JobTitle != null && EF.Functions.ILike(e.JobTitle, pattern))
                || (e.WorkEmail != null && EF.Functions.ILike(e.WorkEmail, pattern))
                || (e.PersonalEmail != null && EF.Functions.ILike(e.PersonalEmail, pattern))
                || (e.PersonalPhone != null && EF.Functions.ILike(e.PersonalPhone, pattern))
                || (e.Phone != null && EF.Functions.ILike(e.Phone, pattern))
                || EF.Functions.ILike(
                    (e.FirstName ?? "") + " " + (e.MiddleName != null ? e.MiddleName + " " : "") + (e.LastName ?? ""),
                    pattern)
                || (e.UserId != null && platformUserIds.Contains(e.UserId)));
        }

        return query.Where(e =>
            (e.FullName != "" && EF.Functions.ILike(e.FullName, pattern))
            || (e.FirstName != null && EF.Functions.ILike(e.FirstName, pattern))
            || (e.LastName != null && EF.Functions.ILike(e.LastName, pattern))
            || (e.MiddleName != null && EF.Functions.ILike(e.MiddleName, pattern))
            || EF.Functions.ILike(e.EmployeeCode, pattern)
            || (e.JobTitle != null && EF.Functions.ILike(e.JobTitle, pattern))
            || (e.WorkEmail != null && EF.Functions.ILike(e.WorkEmail, pattern))
            || (e.PersonalEmail != null && EF.Functions.ILike(e.PersonalEmail, pattern))
            || (e.PersonalPhone != null && EF.Functions.ILike(e.PersonalPhone, pattern))
            || (e.Phone != null && EF.Functions.ILike(e.Phone, pattern))
            || EF.Functions.ILike(
                (e.FirstName ?? "") + " " + (e.MiddleName != null ? e.MiddleName + " " : "") + (e.LastName ?? ""),
                pattern));
    }

    private static IQueryable<EmployeeEntity> ApplyCompositeStatusFilters(
        IQueryable<EmployeeEntity> query, EmployeeStatusFilter.ResolvedStatusFilters statusFilters)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        if (statusFilters.OrBranches.Count > 0)
            return ApplyOrBranchesEf(query, statusFilters.OrBranches, today);

        if (statusFilters.Engagement is { } engagement)
            query = WhereEngagementEf(query, engagement);

        if (statusFilters.WorkStates.Count > 0)
            query = WhereWorkStatesEf(query, statusFilters.WorkStates, today);

        return query;
    }

    private static IQueryable<EmployeeEntity> ApplyOrBranchesEf(
        IQueryable<EmployeeEntity> query,
        IReadOnlyList<EmployeeStatusFilter.StatusOrBranch> branches,
        DateOnly today)
    {
        if (IsActiveAndPreHireOrBranches(branches))
            return WhereActiveOrPreHireEf(query);

        if (branches.Count == 1)
            return WhereStatusBranchEf(query, branches[0], today);

        IQueryable<EmployeeEntity>? combined = null;
        foreach (var branch in branches)
        {
            var branchQuery = WhereStatusBranchEf(query, branch, today);
            combined = combined is null ? branchQuery : combined.Union(branchQuery);
        }

        return combined ?? query;
    }

    private static bool IsActiveAndPreHireOrBranches(IReadOnlyList<EmployeeStatusFilter.StatusOrBranch> branches) =>
        branches.Count == 2
        && branches.Any(branch =>
            branch.Engagement == EmployeeEngagementValues.Active && branch.WorkStates.Count == 0)
        && branches.Any(branch =>
            branch.Engagement == EmployeeEngagementValues.PreHire && branch.WorkStates.Count == 0);

    private static IQueryable<EmployeeEntity> WhereActiveOrPreHireEf(IQueryable<EmployeeEntity> query) =>
        query.Where(e =>
            e.LifecycleState == EmployeeLifecycleStates.PreHire
            || e.EmploymentStatus == EmploymentStatusValues.PreHire
            || (
                !e.IsDraft
                && e.LifecycleState != EmployeeLifecycleStates.PreHire
                && e.EmploymentStatus != EmploymentStatusValues.PreHire
                && e.LifecycleState != EmployeeLifecycleStates.Terminated
                && e.EmploymentStatus != EmploymentStatusValues.Terminated
                && e.LifecycleState != EmployeeLifecycleStates.Resigned
                && e.EmploymentStatus != EmploymentStatusValues.Resigned
                && e.LifecycleState != EmployeeLifecycleStates.Suspended
                && e.EmploymentStatus != EmploymentStatusValues.Suspended
                && e.EmploymentStatus != EmploymentStatusValues.Inactive
                && e.EmploymentStatus != EmploymentStatusValues.Draft
                && (
                    e.LifecycleState == EmployeeLifecycleStates.Active
                    || e.LifecycleState == EmployeeLifecycleStates.OnLeave
                    || e.EmploymentStatus == EmploymentStatusValues.Active
                    || e.EmploymentStatus == EmploymentStatusValues.Probation
                    || e.EmploymentStatus == EmploymentStatusValues.OnLeave)));

    private static IQueryable<EmployeeEntity> WhereStatusBranchEf(
        IQueryable<EmployeeEntity> query,
        EmployeeStatusFilter.StatusOrBranch branch,
        DateOnly today)
    {
        if (branch.Engagement is { } engagement)
            query = WhereEngagementEf(query, engagement);

        if (branch.WorkStates.Count > 0)
            query = WhereWorkStatesEf(query, branch.WorkStates, today);

        return query;
    }

    private static IQueryable<EmployeeEntity> WhereEngagementEf(
        IQueryable<EmployeeEntity> query, string engagement) =>
        engagement switch
        {
            EmployeeEngagementValues.Draft => query.Where(e =>
                e.IsDraft || e.EmploymentStatus == EmploymentStatusValues.Draft),
            EmployeeEngagementValues.PreHire => query.Where(e =>
                e.LifecycleState == EmployeeLifecycleStates.PreHire
                || e.EmploymentStatus == EmploymentStatusValues.PreHire),
            EmployeeEngagementValues.Active => query.Where(e =>
                !e.IsDraft
                && e.LifecycleState != EmployeeLifecycleStates.PreHire
                && e.EmploymentStatus != EmploymentStatusValues.PreHire
                && e.LifecycleState != EmployeeLifecycleStates.Terminated
                && e.EmploymentStatus != EmploymentStatusValues.Terminated
                && e.LifecycleState != EmployeeLifecycleStates.Resigned
                && e.EmploymentStatus != EmploymentStatusValues.Resigned
                && e.LifecycleState != EmployeeLifecycleStates.Suspended
                && e.EmploymentStatus != EmploymentStatusValues.Suspended
                && e.EmploymentStatus != EmploymentStatusValues.Inactive
                && e.EmploymentStatus != EmploymentStatusValues.Draft
                && (
                    e.LifecycleState == EmployeeLifecycleStates.Active
                    || e.LifecycleState == EmployeeLifecycleStates.OnLeave
                    || e.EmploymentStatus == EmploymentStatusValues.Active
                    || e.EmploymentStatus == EmploymentStatusValues.Probation
                    || e.EmploymentStatus == EmploymentStatusValues.OnLeave)),
            EmployeeEngagementValues.Suspended => query.Where(e =>
                e.LifecycleState == EmployeeLifecycleStates.Suspended
                || e.EmploymentStatus == EmploymentStatusValues.Suspended),
            EmployeeEngagementValues.Terminated => query.Where(e =>
                e.LifecycleState == EmployeeLifecycleStates.Terminated
                || e.EmploymentStatus == EmploymentStatusValues.Terminated),
            EmployeeEngagementValues.Resigned => query.Where(e =>
                e.LifecycleState == EmployeeLifecycleStates.Resigned
                || e.EmploymentStatus == EmploymentStatusValues.Resigned),
            EmployeeEngagementValues.Inactive => query.Where(e =>
                e.EmploymentStatus == EmploymentStatusValues.Inactive),
            _ => query,
        };

    private static IQueryable<EmployeeEntity> WhereWorkStatesEf(
        IQueryable<EmployeeEntity> query,
        IReadOnlyList<string> workStates,
        DateOnly today)
    {
        var wantsProbation = workStates.Contains(
            EmployeeWorkStateValues.Probation, StringComparer.OrdinalIgnoreCase);
        var wantsOnLeave = workStates.Contains(
            EmployeeWorkStateValues.OnLeave, StringComparer.OrdinalIgnoreCase);

        if (!wantsProbation && !wantsOnLeave)
            return query;

        if (wantsProbation && wantsOnLeave)
        {
            return query.Where(e =>
                e.EmploymentStatus == EmploymentStatusValues.Probation
                || (e.ProbationEndDate != null
                    && e.ProbationEndDate >= today
                    && e.LifecycleState == EmployeeLifecycleStates.Active)
                || e.LifecycleState == EmployeeLifecycleStates.OnLeave
                || e.EmploymentStatus == EmploymentStatusValues.OnLeave);
        }

        if (wantsProbation)
        {
            return query.Where(e =>
                e.EmploymentStatus == EmploymentStatusValues.Probation
                || (e.ProbationEndDate != null
                    && e.ProbationEndDate >= today
                    && e.LifecycleState == EmployeeLifecycleStates.Active));
        }

        return query.Where(e =>
            e.LifecycleState == EmployeeLifecycleStates.OnLeave
            || e.EmploymentStatus == EmploymentStatusValues.OnLeave);
    }

    private static void AppendCompositeStatusSql(
        List<string> conditions,
        Dictionary<string, object?> parameters,
        EmployeeStatusFilter.ResolvedStatusFilters statusFilters)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        parameters["StatusFilterToday"] = today;

        if (statusFilters.OrBranches.Count > 0)
        {
            var branchSql = statusFilters.OrBranches
                .Select(branch => BuildBranchSql(branch))
                .ToList();
            conditions.Add($"({string.Join(" OR ", branchSql)})");
            return;
        }

        if (statusFilters.Engagement is { } engagement)
            conditions.Add(BuildEngagementSql(engagement));

        if (statusFilters.WorkStates.Count == 0)
            return;

        var wantsProbation = statusFilters.WorkStates.Contains(
            EmployeeWorkStateValues.Probation, StringComparer.OrdinalIgnoreCase);
        var wantsOnLeave = statusFilters.WorkStates.Contains(
            EmployeeWorkStateValues.OnLeave, StringComparer.OrdinalIgnoreCase);
        conditions.Add(BuildWorkStatesSql(wantsProbation, wantsOnLeave));
    }

    private static string BuildBranchSql(EmployeeStatusFilter.StatusOrBranch branch)
    {
        var parts = new List<string>();
        if (branch.Engagement is { } engagement)
            parts.Add(BuildEngagementSql(engagement));

        if (branch.WorkStates.Count > 0)
        {
            var wantsProbation = branch.WorkStates.Contains(
                EmployeeWorkStateValues.Probation, StringComparer.OrdinalIgnoreCase);
            var wantsOnLeave = branch.WorkStates.Contains(
                EmployeeWorkStateValues.OnLeave, StringComparer.OrdinalIgnoreCase);
            parts.Add(BuildWorkStatesSql(wantsProbation, wantsOnLeave));
        }

        return parts.Count switch
        {
            0 => "TRUE",
            1 => parts[0],
            _ => $"({string.Join(" AND ", parts)})",
        };
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
