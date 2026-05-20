using Dapper;
using Microsoft.Extensions.Options;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Formatting;
using ZelosHR.Api.Shared.Pagination;

namespace ZelosHR.Api.Entities.Employees;

public class EmployeesDirectoryService
{
    private readonly IDatabaseManager _database;
    private readonly AppSettings _settings;

    public EmployeesDirectoryService(IDatabaseManager database, IOptions<AppSettings> settings)
    {
        _database = database;
        _settings = settings.Value;
    }

    public async Task<Respons<EmployeeDirectorySummaryDto>> GetSummaryAsync(
        string tenantId,
        string orgId,
        CancellationToken ct = default)
    {
        await using var connection = await _database.GetConnectionAsync(ct);

        var summary = await connection.QuerySingleAsync<EmployeeDirectorySummaryDto>(
            $"""
            SELECT
                COUNT(*)::int AS TotalEmployees,
                COUNT(*) FILTER (WHERE employment_status = 'Active')::int AS ActiveEmployees,
                COUNT(*) FILTER (WHERE employment_status = 'Probation')::int AS OnProbation,
                COUNT(*) FILTER (
                    WHERE employment_type = 'Contractor'
                       OR contract_type = 'Fixed-term'
                )::int AS OnContract
            FROM {_settings.EmployeesTable}
            WHERE tenant_id = @TenantId AND org_id = @OrgId AND is_deleted = FALSE
            """,
            new { TenantId = tenantId, OrgId = orgId });

        return Respons<EmployeeDirectorySummaryDto>.Ok(summary);
    }

    public async Task<Respons<EmployeeDirectoryListDto>> GetDirectoryAsync(
        EmployeeDirectoryQuery query,
        string tenantId,
        string orgId,
        CancellationToken ct = default)
    {
        var paging = PagedQuery.From(query.Page, query.Size);
        var (whereClause, parameters) = EmployeeDirectoryQueryBuilder.Build(
            query, _settings.EmployeesTable, tenantId, orgId);
        var orderBy = EmployeeDirectoryQueryBuilder.BuildOrderBy(query.SortBy, query.SortOrder);

        await using var connection = await _database.GetConnectionAsync(ct);

        var countSql = $"""
            SELECT COUNT(*)::int
            FROM {_settings.EmployeesTable} e
            LEFT JOIN zeloshr.zhr_departments d ON d.id = e.department_id
            WHERE {whereClause}
            """;

        var total = await connection.ExecuteScalarAsync<int>(countSql, parameters);

        var listSql = $"""
            SELECT
                e.id AS Id,
                e.employee_code AS EmployeeCode,
                e.first_name AS FirstName,
                e.middle_name AS MiddleName,
                e.last_name AS LastName,
                e.job_title AS JobTitle,
                e.department_id AS DepartmentId,
                d.name AS DepartmentName,
                e.branch_id AS BranchId,
                b.name AS BranchName,
                e.employment_type AS EmploymentType,
                e.manager_id AS ManagerId,
                TRIM(CONCAT(m.first_name, ' ', m.last_name)) AS ManagerName,
                e.employment_status AS Status
            FROM {_settings.EmployeesTable} e
            LEFT JOIN zeloshr.zhr_departments d ON d.id = e.department_id
            LEFT JOIN zeloshr.zhr_branches b ON b.id = e.branch_id
            LEFT JOIN {_settings.EmployeesTable} m ON m.id = e.manager_id
            WHERE {whereClause}
            {orderBy}
            LIMIT @Limit OFFSET @Offset
            """;

        parameters["Limit"] = paging.Size;
        parameters["Offset"] = paging.Offset;

        var rows = await connection.QueryAsync<EmployeeDirectoryRow>(listSql, parameters);

        var items = rows.Select(MapRow).ToList();
        var summaryResult = await GetSummaryAsync(tenantId, orgId, ct);

        var list = new EmployeeDirectoryListDto
        {
            Summary = summaryResult.Data ?? new EmployeeDirectorySummaryDto(),
            Items = items,
            EmptyMessage = items.Count == 0
                ? "No employees found matching your search"
                : null,
        };

        var pagination = new PaginationMeta
        {
            Page = paging.Page,
            Size = paging.Size,
            Total = total,
            HasNext = paging.Offset + items.Count < total,
        };

        return Respons<EmployeeDirectoryListDto>.Ok(list, pagination: pagination);
    }

    public async Task<Respons<EmployeeFilterOptionsDto>> GetFilterOptionsAsync(
        string tenantId,
        string orgId,
        CancellationToken ct = default)
    {
        await using var connection = await _database.GetConnectionAsync(ct);

        var departments = await connection.QueryAsync<FilterOptionDto>(
            """
            SELECT id::text AS Id, name AS Name
            FROM zeloshr.zhr_departments
            WHERE tenant_id = @TenantId AND org_id = @OrgId AND is_archived = FALSE
            ORDER BY name
            """,
            new { TenantId = tenantId, OrgId = orgId });

        var branches = await connection.QueryAsync<FilterOptionDto>(
            """
            SELECT id::text AS Id, name AS Name
            FROM zeloshr.zhr_branches
            WHERE tenant_id = @TenantId AND org_id = @OrgId AND is_archived = FALSE
            ORDER BY name
            """,
            new { TenantId = tenantId, OrgId = orgId });

        return Respons<EmployeeFilterOptionsDto>.Ok(new EmployeeFilterOptionsDto
        {
            Departments = departments.ToList(),
            Branches = branches.ToList(),
            EmploymentTypes = ["Full-time", "Part-time", "Contractor", "Casual"],
            Statuses = ["Active", "Probation", "On Leave", "Suspended", "Resigned", "Terminated"],
        });
    }

    private static EmployeeDirectoryItemDto MapRow(EmployeeDirectoryRow row) =>
        new()
        {
            EmployeeId = row.Id.ToString(),
            EmployeeCode = row.EmployeeCode,
            FullName = NameFormatting.BuildFullName(row.FirstName, row.MiddleName, row.LastName),
            Initials = NameFormatting.BuildInitials(row.FirstName, row.LastName),
            JobTitle = row.JobTitle,
            DepartmentId = row.DepartmentId?.ToString(),
            DepartmentName = row.DepartmentName,
            BranchId = row.BranchId?.ToString(),
            BranchName = row.BranchName,
            EmploymentType = row.EmploymentType,
            ManagerId = row.ManagerId?.ToString(),
            ManagerName = string.IsNullOrWhiteSpace(row.ManagerName) ? null : row.ManagerName,
            Status = row.Status,
        };

    private sealed class EmployeeDirectoryRow
    {
        public Guid Id { get; init; }
        public required string EmployeeCode { get; init; }
        public required string FirstName { get; init; }
        public string? MiddleName { get; init; }
        public required string LastName { get; init; }
        public string? JobTitle { get; init; }
        public Guid? DepartmentId { get; init; }
        public string? DepartmentName { get; init; }
        public Guid? BranchId { get; init; }
        public string? BranchName { get; init; }
        public string? EmploymentType { get; init; }
        public Guid? ManagerId { get; init; }
        public string? ManagerName { get; init; }
        public required string Status { get; init; }
    }
}
