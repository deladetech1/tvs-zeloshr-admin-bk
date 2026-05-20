using Microsoft.EntityFrameworkCore;
using ZelosHR.Api.Persistence.Entities;

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

    public static IQueryable<EmployeeEntity> ApplyFilters(
        IQueryable<EmployeeEntity> query, EmployeeDirectoryQuery directoryQuery)
    {
        if (!string.IsNullOrWhiteSpace(directoryQuery.Search)
            && directoryQuery.Search.Trim().Length >= MinimumSearchLength)
        {
            var pattern = $"%{directoryQuery.Search.Trim()}%";
            query = query.Where(e =>
                EF.Functions.ILike(e.FirstName, pattern)
                || EF.Functions.ILike(e.LastName, pattern)
                || EF.Functions.ILike(e.EmployeeCode, pattern)
                || (e.JobTitle != null && EF.Functions.ILike(e.JobTitle, pattern))
                || EF.Functions.ILike(
                    e.FirstName + " " + (e.MiddleName != null ? e.MiddleName + " " : "") + e.LastName,
                    pattern));
        }

        if (directoryQuery.DepartmentId.HasValue)
            query = query.Where(e => e.DepartmentId == directoryQuery.DepartmentId);

        if (directoryQuery.BranchId.HasValue)
            query = query.Where(e => e.BranchId == directoryQuery.BranchId);

        if (!string.IsNullOrWhiteSpace(directoryQuery.EmploymentType))
            query = query.Where(e => e.EmploymentType == directoryQuery.EmploymentType.Trim());

        if (!string.IsNullOrWhiteSpace(directoryQuery.Status))
            query = query.Where(e => e.EmploymentStatus == directoryQuery.Status.Trim());
        else if (!directoryQuery.IncludeInactive)
            query = query.Where(e => e.EmploymentStatus != "Terminated" && e.EmploymentStatus != "Resigned");

        return query;
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
