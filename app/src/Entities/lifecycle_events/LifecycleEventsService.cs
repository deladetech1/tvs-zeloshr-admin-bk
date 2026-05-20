using Dapper;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Pagination;

namespace ZelosHR.Api.Entities.LifecycleEvents;

public class LifecycleEventsService
{
    private readonly IDatabaseManager _database;
    private readonly EmployeesService _employees;

    public LifecycleEventsService(IDatabaseManager database, EmployeesService employees)
    {
        _database = database;
        _employees = employees;
    }

    public async Task<Respons<LifecycleEventSummaryDto>> GetSummaryAsync(
        string tenantId,
        string orgId,
        CancellationToken ct = default)
    {
        await using var connection = await _database.GetConnectionAsync(ct);

        var summary = await connection.QuerySingleAsync<LifecycleEventSummaryDto>(
            """
            SELECT
                COUNT(*) FILTER (WHERE urgency = 'Overdue')::int AS OverdueCount,
                COUNT(*) FILTER (WHERE urgency = 'Critical')::int AS CriticalCount,
                COUNT(*) FILTER (WHERE status IN ('Pending', 'Awaiting Manager'))::int AS PendingActionCount,
                COUNT(*)::int AS TotalEventsCount
            FROM zeloshr.zhr_lifecycle_events
            WHERE tenant_id = @TenantId AND org_id = @OrgId
            """,
            new { TenantId = tenantId, OrgId = orgId });

        return Respons<LifecycleEventSummaryDto>.Ok(summary);
    }

    public async Task<Respons<LifecycleEventListDto>> ListAsync(
        string? search,
        string? eventType,
        string? urgency,
        string? department,
        int page,
        int size,
        string tenantId,
        string orgId,
        CancellationToken ct = default)
    {
        var paging = PagedQuery.From(page, size);
        await using var connection = await _database.GetConnectionAsync(ct);

        var conditions = new List<string> { "tenant_id = @TenantId", "org_id = @OrgId" };
        var parameters = new DynamicParameters(new { TenantId = tenantId, OrgId = orgId });

        if (!string.IsNullOrWhiteSpace(search) && search.Trim().Length >= 3)
        {
            conditions.Add("(employee_full_name ILIKE @Search OR event_type ILIKE @Search)");
            parameters.Add("Search", $"%{search.Trim()}%");
        }

        if (!string.IsNullOrWhiteSpace(eventType) && !eventType.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            conditions.Add("event_type = @EventType");
            parameters.Add("EventType", eventType.Trim());
        }

        if (!string.IsNullOrWhiteSpace(urgency) && !urgency.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            conditions.Add("urgency = @Urgency");
            parameters.Add("Urgency", urgency.Trim());
        }

        if (!string.IsNullOrWhiteSpace(department) && !department.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            conditions.Add("department_name ILIKE @Department");
            parameters.Add("Department", $"%{department.Trim()}%");
        }

        var where = string.Join(" AND ", conditions);
        parameters.Add("Limit", paging.Size);
        parameters.Add("Offset", paging.Offset);

        var total = await connection.ExecuteScalarAsync<int>(
            $"SELECT COUNT(*)::int FROM zeloshr.zhr_lifecycle_events WHERE {where}",
            parameters);

        var items = (await connection.QueryAsync<LifecycleEventListItemDto>(
            $"""
            SELECT
                id::text AS LifecycleEventId,
                employee_id::text AS EmployeeId,
                employee_full_name AS EmployeeFullName,
                event_type AS EventType,
                department_name AS DepartmentName,
                branch_name AS BranchName,
                due_date AS DueDate,
                status AS Status,
                urgency AS Urgency
            FROM zeloshr.zhr_lifecycle_events
            WHERE {where}
            ORDER BY due_date ASC
            LIMIT @Limit OFFSET @Offset
            """,
            parameters)).ToList();

        var summary = (await GetSummaryAsync(tenantId, orgId, ct)).Data ?? new LifecycleEventSummaryDto();

        return Respons<LifecycleEventListDto>.Ok(
            new LifecycleEventListDto { Summary = summary, Items = items },
            pagination: new PaginationMeta
            {
                Page = paging.Page,
                Size = paging.Size,
                Total = total,
                HasNext = paging.Offset + items.Count < total,
            });
    }

    public async Task<Respons<LifecycleEventListItemDto>> GetByIdAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default) =>
        await QueryOneAsync(id, tenantId, orgId, ct);

    public async Task<Respons<LifecycleEventListItemDto>> CreateAsync(
        CreateLifecycleEventDto data, string tenantId, string orgId, CancellationToken ct = default)
    {
        var emp = await _employees.GetByIdAsync(data.EmployeeId, tenantId, orgId, ct);
        if (!emp.Success || emp.Data is null)
            return Respons<LifecycleEventListItemDto>.ValidationError(
                new Dictionary<string, string> { ["employeeId"] = "Employee not found." });

        var status = string.IsNullOrWhiteSpace(data.Status) ? "Pending" : data.Status.Trim();
        var urgency = string.IsNullOrWhiteSpace(data.Urgency) ? "Upcoming" : data.Urgency.Trim();

        await using var connection = await _database.GetConnectionAsync(ct);
        var id = await connection.QuerySingleAsync<Guid>(
            """
            INSERT INTO zeloshr.zhr_lifecycle_events (
                tenant_id, org_id, employee_id, employee_full_name, event_type,
                department_name, branch_name, due_date, status, urgency
            )
            VALUES (
                @TenantId, @OrgId, @EmployeeId, @FullName, @EventType,
                @Dept, @Branch, @DueDate, @Status, @Urgency
            )
            RETURNING id
            """,
            new
            {
                TenantId = tenantId,
                OrgId = orgId,
                EmployeeId = data.EmployeeId,
                FullName = emp.Data.FullName,
                EventType = data.EventType!.Trim(),
                Dept = emp.Data.DepartmentName,
                Branch = emp.Data.BranchName,
                data.DueDate,
                Status = status,
                Urgency = urgency,
            });

        return await QueryOneAsync(id, tenantId, orgId, ct);
    }

    public async Task<Respons<LifecycleEventListItemDto>> UpdateAsync(
        Guid id, UpdateLifecycleEventDto data, string tenantId, string orgId, CancellationToken ct = default)
    {
        var sets = new List<string> { "updated_at = NOW()" };
        var p = new DynamicParameters(new { Id = id, TenantId = tenantId, OrgId = orgId });
        if (!string.IsNullOrWhiteSpace(data.EventType)) { sets.Add("event_type = @EventType"); p.Add("EventType", data.EventType.Trim()); }
        if (data.DueDate.HasValue) { sets.Add("due_date = @DueDate"); p.Add("DueDate", data.DueDate.Value); }
        if (!string.IsNullOrWhiteSpace(data.Status)) { sets.Add("status = @Status"); p.Add("Status", data.Status.Trim()); }
        if (!string.IsNullOrWhiteSpace(data.Urgency)) { sets.Add("urgency = @Urgency"); p.Add("Urgency", data.Urgency.Trim()); }
        if (sets.Count == 1)
            return Respons<LifecycleEventListItemDto>.Fail("No fields to update.", statusCode: 400);

        await using var connection = await _database.GetConnectionAsync(ct);
        if (await connection.ExecuteAsync(
                $"UPDATE zeloshr.zhr_lifecycle_events SET {string.Join(", ", sets)} WHERE id = @Id AND tenant_id = @TenantId AND org_id = @OrgId", p) == 0)
            return Respons<LifecycleEventListItemDto>.Fail("Lifecycle event not found.", statusCode: 404);

        return await QueryOneAsync(id, tenantId, orgId, ct);
    }

    public async Task<Respons<object>> DeleteAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default)
    {
        await using var connection = await _database.GetConnectionAsync(ct);
        var n = await connection.ExecuteAsync(
            "DELETE FROM zeloshr.zhr_lifecycle_events WHERE id = @Id AND tenant_id = @TenantId AND org_id = @OrgId",
            new { Id = id, TenantId = tenantId, OrgId = orgId });
        return n == 0
            ? Respons<object>.Fail("Lifecycle event not found.", statusCode: 404)
            : Respons<object>.Ok(new { lifecycleEventId = id.ToString() }, "Lifecycle event deleted.");
    }

    private async Task<Respons<LifecycleEventListItemDto>> QueryOneAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct)
    {
        await using var connection = await _database.GetConnectionAsync(ct);
        var row = await connection.QuerySingleOrDefaultAsync<LifecycleEventListItemDto>(
            """
            SELECT
                id::text AS LifecycleEventId,
                employee_id::text AS EmployeeId,
                employee_full_name AS EmployeeFullName,
                event_type AS EventType,
                department_name AS DepartmentName,
                branch_name AS BranchName,
                due_date AS DueDate,
                status AS Status,
                urgency AS Urgency
            FROM zeloshr.zhr_lifecycle_events
            WHERE id = @Id AND tenant_id = @TenantId AND org_id = @OrgId
            """,
            new { Id = id, TenantId = tenantId, OrgId = orgId });
        return row is null
            ? Respons<LifecycleEventListItemDto>.Fail("Lifecycle event not found.", statusCode: 404)
            : Respons<LifecycleEventListItemDto>.Ok(row);
    }
}
