using Dapper;
using Microsoft.Extensions.Options;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Formatting;
using ZelosHR.Api.Shared.Pagination;

namespace ZelosHR.Api.Entities.Departments;

public class DepartmentsService
{
    private readonly IDatabaseManager _database;
    private readonly AppSettings _settings;

    public DepartmentsService(IDatabaseManager database, IOptions<AppSettings> settings)
    {
        _database = database;
        _settings = settings.Value;
    }

    public async Task<Respons<OrganisationSummaryDto>> GetSummaryAsync(
        string tenantId,
        string orgId,
        CancellationToken ct = default)
    {
        await using var connection = await _database.GetConnectionAsync(ct);

        var summary = await connection.QuerySingleAsync<OrganisationSummaryDto>(
            """
            SELECT
                (SELECT COUNT(*)::int FROM zeloshr.zhr_departments
                 WHERE tenant_id = @TenantId AND org_id = @OrgId AND is_archived = FALSE) AS DepartmentCount,
                (SELECT COUNT(*)::int FROM zeloshr.zhr_branches
                 WHERE tenant_id = @TenantId AND org_id = @OrgId AND is_archived = FALSE) AS BranchCount,
                (SELECT COUNT(*)::int FROM zeloshr.zhr_departments
                 WHERE tenant_id = @TenantId AND org_id = @OrgId AND is_archived = TRUE) AS ArchivedCount
            """,
            new { TenantId = tenantId, OrgId = orgId });

        return Respons<OrganisationSummaryDto>.Ok(summary);
    }

    public async Task<Respons<DepartmentListDto>> ListDepartmentsAsync(
        string? search,
        string sortBy,
        string sortOrder,
        bool includeArchived,
        int page,
        int size,
        string tenantId,
        string orgId,
        CancellationToken ct = default)
    {
        var paging = PagedQuery.From(page, size);
        await using var connection = await _database.GetConnectionAsync(ct);

        var conditions = new List<string> { "d.tenant_id = @TenantId", "d.org_id = @OrgId" };
        if (!includeArchived)
            conditions.Add("d.is_archived = FALSE");

        var parameters = new DynamicParameters();
        parameters.Add("TenantId", tenantId);
        parameters.Add("OrgId", orgId);
        if (!string.IsNullOrWhiteSpace(search) && search.Trim().Length >= 3)
        {
            conditions.Add("d.name ILIKE @Search");
            parameters.Add("Search", $"%{search.Trim()}%");
        }

        var where = string.Join(" AND ", conditions);
        var orderColumn = sortBy.Equals("employeeCount", StringComparison.OrdinalIgnoreCase)
            ? "EmployeeCount"
            : "d.name";
        var direction = sortOrder.Equals("desc", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";

        var total = await connection.ExecuteScalarAsync<int>(
            $"SELECT COUNT(*)::int FROM zeloshr.zhr_departments d WHERE {where}",
            parameters);

        parameters.Add("Limit", paging.Size);
        parameters.Add("Offset", paging.Offset);

        var rows = await connection.QueryAsync<DepartmentRow>(
            $"""
            SELECT
                d.id AS Id,
                d.name AS Name,
                d.parent_department_id AS ParentDepartmentId,
                pd.name AS ParentDepartmentName,
                d.is_archived AS IsArchived,
                h.id AS HeadId,
                h.first_name AS HeadFirstName,
                h.last_name AS HeadLastName,
                h.job_title AS HeadJobTitle,
                (
                    SELECT COUNT(*)::int FROM {_settings.EmployeesTable} e
                    WHERE e.department_id = d.id AND e.is_deleted = FALSE
                ) AS EmployeeCount
            FROM zeloshr.zhr_departments d
            LEFT JOIN zeloshr.zhr_departments pd ON pd.id = d.parent_department_id
            LEFT JOIN {_settings.EmployeesTable} h ON h.id = d.head_of_department_id
            WHERE {where}
            ORDER BY {orderColumn} {direction}
            LIMIT @Limit OFFSET @Offset
            """,
            parameters);

        var items = rows.Select(r => new DepartmentListItemDto
        {
            DepartmentId = r.Id.ToString(),
            Name = r.Name,
            ParentDepartmentId = r.ParentDepartmentId?.ToString(),
            ParentDepartmentName = r.ParentDepartmentName,
            HeadOfDepartment = r.HeadId is null
                ? null
                : new DepartmentHeadDto
                {
                    EmployeeId = r.HeadId.Value.ToString(),
                    FullName = NameFormatting.BuildFullName(r.HeadFirstName!, null, r.HeadLastName!),
                    JobTitle = r.HeadJobTitle,
                    Initials = NameFormatting.BuildInitials(r.HeadFirstName!, r.HeadLastName!),
                },
            EmployeeCount = r.EmployeeCount,
            IsArchived = r.IsArchived,
            HierarchyLevel = r.ParentDepartmentId is null ? 0 : 1,
        }).ToList();

        var summary = (await GetSummaryAsync(tenantId, orgId, ct)).Data ?? new OrganisationSummaryDto();

        return Respons<DepartmentListDto>.Ok(
            new DepartmentListDto
            {
                Summary = summary,
                Items = items,
                ShowingLabel = $"Showing {Math.Min(paging.Offset + items.Count, total)} of {total} departments",
            },
            pagination: new PaginationMeta
            {
                Page = paging.Page,
                Size = paging.Size,
                Total = total,
                HasNext = paging.Offset + items.Count < total,
            });
    }

    private sealed class DepartmentRow
    {
        public Guid Id { get; init; }
        public required string Name { get; init; }
        public Guid? ParentDepartmentId { get; init; }
        public string? ParentDepartmentName { get; init; }
        public bool IsArchived { get; init; }
        public Guid? HeadId { get; init; }
        public string? HeadFirstName { get; init; }
        public string? HeadLastName { get; init; }
        public string? HeadJobTitle { get; init; }
        public int EmployeeCount { get; init; }
    }
}
