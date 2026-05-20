using Dapper;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Pagination;

namespace ZelosHR.Api.Entities.Disciplinary;

public class DisciplinaryService
{
    private readonly IDatabaseManager _database;
    private readonly EmployeesService _employees;

    public DisciplinaryService(IDatabaseManager database, EmployeesService employees)
    {
        _database = database;
        _employees = employees;
    }

    public async Task<Respons<DisciplinarySummaryDto>> GetSummaryAsync(string tenantId, string orgId, CancellationToken ct)
    {
        await using var c = await _database.GetConnectionAsync(ct);
        var s = await c.QuerySingleAsync<DisciplinarySummaryDto>(
            """
            SELECT
                COUNT(*) FILTER (WHERE status = 'Open')::int AS OpenCases,
                COUNT(*) FILTER (WHERE severity = 'High')::int AS HighSeverity,
                COUNT(*) FILTER (WHERE status = 'Closed')::int AS ClosedCases,
                COUNT(*)::int AS TotalCases
            FROM zeloshr.zhr_disciplinary_cases WHERE tenant_id = @TenantId AND org_id = @OrgId
            """, new { TenantId = tenantId, OrgId = orgId });
        return Respons<DisciplinarySummaryDto>.Ok(s);
    }

    public async Task<Respons<DisciplinaryListDto>> ListAsync(
        string? search, string? status, string? severity,
        int page, int size, string tenantId, string orgId, CancellationToken ct)
    {
        var paging = PagedQuery.From(page, size);
        await using var c = await _database.GetConnectionAsync(ct);
        var conditions = new List<string> { "tenant_id = @TenantId", "org_id = @OrgId" };
        var p = new DynamicParameters(new { TenantId = tenantId, OrgId = orgId });
        if (!string.IsNullOrWhiteSpace(search) && search.Length >= 3)
        { conditions.Add("employee_full_name ILIKE @Search"); p.Add("Search", $"%{search}%"); }
        if (!string.IsNullOrWhiteSpace(status) && status != "all") { conditions.Add("status = @Status"); p.Add("Status", status); }
        if (!string.IsNullOrWhiteSpace(severity) && severity != "all") { conditions.Add("severity = @Severity"); p.Add("Severity", severity); }
        var where = string.Join(" AND ", conditions);
        p.Add("Limit", paging.Size); p.Add("Offset", paging.Offset);
        var total = await c.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM zeloshr.zhr_disciplinary_cases WHERE {where}", p);
        var items = (await c.QueryAsync<DisciplinaryCaseListItemDto>(
            $"""
            SELECT id::text AS CaseId, employee_id::text AS EmployeeId, employee_full_name AS EmployeeFullName,
                   case_type AS CaseType, severity AS Severity, status AS Status, opened_at AS OpenedAt, description AS Description
            FROM zeloshr.zhr_disciplinary_cases WHERE {where} ORDER BY opened_at DESC LIMIT @Limit OFFSET @Offset
            """, p)).ToList();
        var summary = (await GetSummaryAsync(tenantId, orgId, ct)).Data ?? new DisciplinarySummaryDto();
        return Respons<DisciplinaryListDto>.Ok(new DisciplinaryListDto { Summary = summary, Items = items },
            pagination: new PaginationMeta { Page = paging.Page, Size = paging.Size, Total = total, HasNext = paging.Offset + items.Count < total });
    }

    public async Task<Respons<DisciplinaryCaseListItemDto>> GetByIdAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default) =>
        await QueryOneAsync(id, tenantId, orgId, ct);

    public async Task<Respons<DisciplinaryCaseListItemDto>> CreateAsync(
        CreateDisciplinaryCaseDto data, string tenantId, string orgId, CancellationToken ct = default)
    {
        var emp = await _employees.ResolveEmployeeDisplayAsync(data.EmployeeId, tenantId, orgId, ct);
        if (emp is null)
            return Respons<DisciplinaryCaseListItemDto>.ValidationError(
                new Dictionary<string, string> { ["employeeId"] = "Employee not found." });

        var status = string.IsNullOrWhiteSpace(data.Status) ? "Open" : data.Status.Trim();
        var openedAt = data.OpenedAt ?? DateOnly.FromDateTime(DateTime.UtcNow);

        await using var c = await _database.GetConnectionAsync(ct);
        var id = await c.QuerySingleAsync<Guid>(
            """
            INSERT INTO zeloshr.zhr_disciplinary_cases (
                tenant_id, org_id, employee_id, employee_full_name, case_type, severity, status, opened_at, description
            )
            VALUES (
                @TenantId, @OrgId, @EmployeeId, @FullName, @CaseType, @Severity, @Status, @OpenedAt, @Description
            )
            RETURNING id
            """,
            new
            {
                TenantId = tenantId,
                OrgId = orgId,
                EmployeeId = data.EmployeeId,
                FullName = emp.FullName,
                CaseType = data.CaseType!.Trim(),
                Severity = data.Severity!.Trim(),
                Status = status,
                OpenedAt = openedAt,
                Description = string.IsNullOrWhiteSpace(data.Description) ? null : data.Description.Trim(),
            });

        return await QueryOneAsync(id, tenantId, orgId, ct);
    }

    public async Task<Respons<DisciplinaryCaseListItemDto>> UpdateAsync(
        Guid id, UpdateDisciplinaryCaseDto data, string tenantId, string orgId, CancellationToken ct = default)
    {
        var sets = new List<string>();
        var p = new DynamicParameters(new { Id = id, TenantId = tenantId, OrgId = orgId });
        if (!string.IsNullOrWhiteSpace(data.CaseType)) { sets.Add("case_type = @CaseType"); p.Add("CaseType", data.CaseType.Trim()); }
        if (!string.IsNullOrWhiteSpace(data.Severity)) { sets.Add("severity = @Severity"); p.Add("Severity", data.Severity.Trim()); }
        if (data.OpenedAt.HasValue) { sets.Add("opened_at = @OpenedAt"); p.Add("OpenedAt", data.OpenedAt.Value); }
        if (data.Description is not null) { sets.Add("description = @Description"); p.Add("Description", string.IsNullOrWhiteSpace(data.Description) ? null : data.Description.Trim()); }
        if (!string.IsNullOrWhiteSpace(data.Status)) { sets.Add("status = @Status"); p.Add("Status", data.Status.Trim()); }
        if (sets.Count == 0)
            return Respons<DisciplinaryCaseListItemDto>.Fail("No fields to update.", statusCode: 400);

        await using var c = await _database.GetConnectionAsync(ct);
        if (await c.ExecuteAsync(
                $"UPDATE zeloshr.zhr_disciplinary_cases SET {string.Join(", ", sets)} WHERE id = @Id AND tenant_id = @TenantId AND org_id = @OrgId", p) == 0)
            return Respons<DisciplinaryCaseListItemDto>.Fail("Disciplinary case not found.", statusCode: 404);

        return await QueryOneAsync(id, tenantId, orgId, ct);
    }

    public async Task<Respons<object>> DeleteAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default)
    {
        await using var c = await _database.GetConnectionAsync(ct);
        var n = await c.ExecuteAsync(
            "DELETE FROM zeloshr.zhr_disciplinary_cases WHERE id = @Id AND tenant_id = @TenantId AND org_id = @OrgId",
            new { Id = id, TenantId = tenantId, OrgId = orgId });
        return n == 0
            ? Respons<object>.Fail("Disciplinary case not found.", statusCode: 404)
            : Respons<object>.Ok(new { caseId = id.ToString() }, "Disciplinary case deleted.");
    }

    private async Task<Respons<DisciplinaryCaseListItemDto>> QueryOneAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct)
    {
        await using var c = await _database.GetConnectionAsync(ct);
        var row = await c.QuerySingleOrDefaultAsync<DisciplinaryCaseListItemDto>(
            """
            SELECT id::text AS CaseId, employee_id::text AS EmployeeId, employee_full_name AS EmployeeFullName,
                   case_type AS CaseType, severity AS Severity, status AS Status, opened_at AS OpenedAt, description AS Description
            FROM zeloshr.zhr_disciplinary_cases
            WHERE id = @Id AND tenant_id = @TenantId AND org_id = @OrgId
            """,
            new { Id = id, TenantId = tenantId, OrgId = orgId });
        return row is null
            ? Respons<DisciplinaryCaseListItemDto>.Fail("Disciplinary case not found.", statusCode: 404)
            : Respons<DisciplinaryCaseListItemDto>.Ok(row);
    }
}
