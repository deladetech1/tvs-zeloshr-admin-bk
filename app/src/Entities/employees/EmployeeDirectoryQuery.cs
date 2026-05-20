namespace ZelosHR.Api.Entities.Employees;

/// <summary>Query parameters for the employee directory list (matches frontend filters).</summary>
public sealed class EmployeeDirectoryQuery
{
    public string? Search { get; init; }
    public Guid? DepartmentId { get; init; }
    public Guid? BranchId { get; init; }
    public string? EmploymentType { get; init; }
    public string? Status { get; init; }
    public string SortBy { get; init; } = "name";
    public string SortOrder { get; init; } = "asc";
    public int Page { get; init; } = 1;
    public int Size { get; init; } = 10;

    /// <summary>When false, Terminated/Resigned are excluded unless status filter is set.</summary>
    public bool IncludeInactive { get; init; }
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

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            conditions.Add("e.employment_status = @EmploymentStatus");
            parameters["EmploymentStatus"] = query.Status.Trim();
        }
        else if (!query.IncludeInactive)
        {
            conditions.Add("e.employment_status NOT IN ('Terminated', 'Resigned')");
        }

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
}
