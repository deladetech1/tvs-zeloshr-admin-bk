using Dapper;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Pagination;

namespace ZelosHR.Api.Entities.Onboarding;

public class OnboardingService
{
    private readonly IDatabaseManager _database;
    private readonly EmployeesService _employees;

    public OnboardingService(IDatabaseManager database, EmployeesService employees)
    {
        _database = database;
        _employees = employees;
    }

    public async Task<Respons<OnboardingSummaryDto>> GetSummaryAsync(string tenantId, string orgId, CancellationToken ct)
    {
        await using var c = await _database.GetConnectionAsync(ct);
        var s = await c.QuerySingleAsync<OnboardingSummaryDto>(
            """
            SELECT
                COUNT(*) FILTER (WHERE status = 'Pending')::int AS PendingTasks,
                COUNT(*) FILTER (WHERE status = 'In progress')::int AS InProgress,
                COUNT(*) FILTER (WHERE status = 'Completed')::int AS Completed,
                COUNT(*) FILTER (WHERE status != 'Completed' AND due_date < CURRENT_DATE)::int AS Overdue
            FROM zeloshr.zhr_onboarding_tasks WHERE tenant_id = @TenantId AND org_id = @OrgId
            """, new { TenantId = tenantId, OrgId = orgId });
        return Respons<OnboardingSummaryDto>.Ok(s);
    }

    public async Task<Respons<OnboardingListDto>> ListAsync(
        string? search, string? status, int page, int size, string tenantId, string orgId, CancellationToken ct)
    {
        var paging = PagedQuery.From(page, size);
        await using var c = await _database.GetConnectionAsync(ct);
        var conditions = new List<string> { "tenant_id = @TenantId", "org_id = @OrgId" };
        var p = new DynamicParameters(new { TenantId = tenantId, OrgId = orgId });
        if (!string.IsNullOrWhiteSpace(search) && search.Length >= 3)
        { conditions.Add("(employee_full_name ILIKE @Search OR task_name ILIKE @Search)"); p.Add("Search", $"%{search}%"); }
        if (!string.IsNullOrWhiteSpace(status) && status != "all") { conditions.Add("status = @Status"); p.Add("Status", status); }
        var where = string.Join(" AND ", conditions);
        p.Add("Limit", paging.Size); p.Add("Offset", paging.Offset);
        var total = await c.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM zeloshr.zhr_onboarding_tasks WHERE {where}", p);
        var items = (await c.QueryAsync<OnboardingTaskListItemDto>(
            $"""
            SELECT id::text AS TaskId, employee_id::text AS EmployeeId, employee_full_name AS EmployeeFullName,
                   task_name AS TaskName, category AS Category, due_date AS DueDate, status AS Status, assigned_to AS AssignedTo
            FROM zeloshr.zhr_onboarding_tasks WHERE {where} ORDER BY due_date LIMIT @Limit OFFSET @Offset
            """, p)).ToList();
        var summary = (await GetSummaryAsync(tenantId, orgId, ct)).Data ?? new OnboardingSummaryDto();
        return Respons<OnboardingListDto>.Ok(new OnboardingListDto { Summary = summary, Items = items },
            pagination: new PaginationMeta { Page = paging.Page, Size = paging.Size, Total = total, HasNext = paging.Offset + items.Count < total });
    }

    public async Task<Respons<OnboardingTaskListItemDto>> GetByIdAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default) =>
        await QueryOneAsync(id, tenantId, orgId, ct);

    public async Task<Respons<OnboardingTaskListItemDto>> CreateAsync(
        CreateOnboardingTaskDto data, string tenantId, string orgId, CancellationToken ct = default)
    {
        var emp = await _employees.ResolveEmployeeDisplayAsync(data.EmployeeId, tenantId, orgId, ct);
        if (emp is null)
            return Respons<OnboardingTaskListItemDto>.ValidationError(
                new Dictionary<string, string> { ["employeeId"] = "Employee not found." });

        var status = string.IsNullOrWhiteSpace(data.Status) ? "Pending" : data.Status.Trim();

        await using var c = await _database.GetConnectionAsync(ct);
        var id = await c.QuerySingleAsync<Guid>(
            """
            INSERT INTO zeloshr.zhr_onboarding_tasks (
                tenant_id, org_id, employee_id, employee_full_name, task_name, category, due_date, status, assigned_to
            )
            VALUES (
                @TenantId, @OrgId, @EmployeeId, @FullName, @TaskName, @Category, @DueDate, @Status, @AssignedTo
            )
            RETURNING id
            """,
            new
            {
                TenantId = tenantId,
                OrgId = orgId,
                EmployeeId = data.EmployeeId,
                FullName = emp.FullName,
                TaskName = data.TaskName!.Trim(),
                Category = data.Category!.Trim(),
                data.DueDate,
                Status = status,
                AssignedTo = string.IsNullOrWhiteSpace(data.AssignedTo) ? null : data.AssignedTo.Trim(),
            });

        return await QueryOneAsync(id, tenantId, orgId, ct);
    }

    public async Task<Respons<OnboardingTaskListItemDto>> UpdateAsync(
        Guid id, UpdateOnboardingTaskDto data, string tenantId, string orgId, CancellationToken ct = default)
    {
        var sets = new List<string>();
        var p = new DynamicParameters(new { Id = id, TenantId = tenantId, OrgId = orgId });
        if (!string.IsNullOrWhiteSpace(data.TaskName)) { sets.Add("task_name = @TaskName"); p.Add("TaskName", data.TaskName.Trim()); }
        if (!string.IsNullOrWhiteSpace(data.Category)) { sets.Add("category = @Category"); p.Add("Category", data.Category.Trim()); }
        if (data.DueDate.HasValue) { sets.Add("due_date = @DueDate"); p.Add("DueDate", data.DueDate.Value); }
        if (!string.IsNullOrWhiteSpace(data.Status)) { sets.Add("status = @Status"); p.Add("Status", data.Status.Trim()); }
        if (data.AssignedTo is not null) { sets.Add("assigned_to = @AssignedTo"); p.Add("AssignedTo", string.IsNullOrWhiteSpace(data.AssignedTo) ? null : data.AssignedTo.Trim()); }
        if (sets.Count == 0)
            return Respons<OnboardingTaskListItemDto>.Fail("No fields to update.", statusCode: 400);

        await using var c = await _database.GetConnectionAsync(ct);
        if (await c.ExecuteAsync(
                $"UPDATE zeloshr.zhr_onboarding_tasks SET {string.Join(", ", sets)} WHERE id = @Id AND tenant_id = @TenantId AND org_id = @OrgId", p) == 0)
            return Respons<OnboardingTaskListItemDto>.Fail("Onboarding task not found.", statusCode: 404);

        return await QueryOneAsync(id, tenantId, orgId, ct);
    }

    public async Task<Respons<object>> DeleteAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default)
    {
        await using var c = await _database.GetConnectionAsync(ct);
        var n = await c.ExecuteAsync(
            "DELETE FROM zeloshr.zhr_onboarding_tasks WHERE id = @Id AND tenant_id = @TenantId AND org_id = @OrgId",
            new { Id = id, TenantId = tenantId, OrgId = orgId });
        return n == 0
            ? Respons<object>.Fail("Onboarding task not found.", statusCode: 404)
            : Respons<object>.Ok(new { taskId = id.ToString() }, "Onboarding task deleted.");
    }

    private async Task<Respons<OnboardingTaskListItemDto>> QueryOneAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct)
    {
        await using var c = await _database.GetConnectionAsync(ct);
        var row = await c.QuerySingleOrDefaultAsync<OnboardingTaskListItemDto>(
            """
            SELECT id::text AS TaskId, employee_id::text AS EmployeeId, employee_full_name AS EmployeeFullName,
                   task_name AS TaskName, category AS Category, due_date AS DueDate, status AS Status, assigned_to AS AssignedTo
            FROM zeloshr.zhr_onboarding_tasks
            WHERE id = @Id AND tenant_id = @TenantId AND org_id = @OrgId
            """,
            new { Id = id, TenantId = tenantId, OrgId = orgId });
        return row is null
            ? Respons<OnboardingTaskListItemDto>.Fail("Onboarding task not found.", statusCode: 404)
            : Respons<OnboardingTaskListItemDto>.Ok(row);
    }
}
