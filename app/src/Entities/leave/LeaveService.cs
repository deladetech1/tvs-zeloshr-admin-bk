using Dapper;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Pagination;

namespace ZelosHR.Api.Entities.Leave;

public class LeaveService
{
    private readonly IDatabaseManager _database;
    private readonly EmployeesService _employees;

    public LeaveService(IDatabaseManager database, EmployeesService employees)
    {
        _database = database;
        _employees = employees;
    }

    public async Task<Respons<LeaveSummaryDto>> GetSummaryAsync(
        string tenantId, string orgId, CancellationToken ct = default)
    {
        await using var connection = await _database.GetConnectionAsync(ct);
        var today = DateTime.UtcNow.Date;

        var summary = await connection.QuerySingleAsync<LeaveSummaryDto>(
            """
            SELECT
                COUNT(*) FILTER (WHERE status = 'Pending')::int AS PendingRequests,
                COUNT(*) FILTER (WHERE status = 'Approved'
                    AND DATE_TRUNC('month', submitted_at) = DATE_TRUNC('month', NOW()))::int AS ApprovedThisMonth,
                COUNT(*) FILTER (WHERE status = 'Approved'
                    AND @Today BETWEEN start_date AND end_date)::int AS OnLeaveToday,
                COUNT(*)::int AS TotalRequests
            FROM zeloshr.zhr_leave_requests
            WHERE tenant_id = @TenantId AND org_id = @OrgId
            """,
            new { TenantId = tenantId, OrgId = orgId, Today = today });

        return Respons<LeaveSummaryDto>.Ok(summary);
    }

    public async Task<Respons<LeaveListDto>> ListAsync(
        string? search, string? status, string? leaveType,
        int page, int size, string tenantId, string orgId, CancellationToken ct = default)
    {
        var paging = PagedQuery.From(page, size);
        await using var connection = await _database.GetConnectionAsync(ct);

        var conditions = new List<string> { "tenant_id = @TenantId", "org_id = @OrgId" };
        var parameters = new DynamicParameters(new { TenantId = tenantId, OrgId = orgId });

        if (!string.IsNullOrWhiteSpace(search) && search.Trim().Length >= 3)
        {
            conditions.Add("employee_full_name ILIKE @Search");
            parameters.Add("Search", $"%{search.Trim()}%");
        }
        if (!string.IsNullOrWhiteSpace(status) && !status.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            conditions.Add("status = @Status");
            parameters.Add("Status", status);
        }
        if (!string.IsNullOrWhiteSpace(leaveType) && !leaveType.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            conditions.Add("leave_type = @LeaveType");
            parameters.Add("LeaveType", leaveType);
        }

        var where = string.Join(" AND ", conditions);
        parameters.Add("Limit", paging.Size);
        parameters.Add("Offset", paging.Offset);

        var total = await connection.ExecuteScalarAsync<int>(
            $"SELECT COUNT(*)::int FROM zeloshr.zhr_leave_requests WHERE {where}", parameters);

        var requests = (await connection.QueryAsync<LeaveRequestListItemDto>(
            $"""
            SELECT
                id::text AS LeaveRequestId,
                employee_id::text AS EmployeeId,
                employee_full_name AS EmployeeFullName,
                leave_type AS LeaveType,
                start_date AS StartDate,
                end_date AS EndDate,
                days_requested AS DaysRequested,
                status AS Status,
                approver_name AS ApproverName
            FROM zeloshr.zhr_leave_requests
            WHERE {where}
            ORDER BY submitted_at DESC
            LIMIT @Limit OFFSET @Offset
            """,
            parameters)).ToList();

        var balances = (await connection.QueryAsync<LeaveBalanceListItemDto>(
            """
            SELECT
                employee_id::text AS EmployeeId,
                employee_full_name AS EmployeeFullName,
                leave_type AS LeaveType,
                entitled_days AS EntitledDays,
                used_days AS UsedDays,
                remaining_days AS RemainingDays
            FROM zeloshr.zhr_leave_balances
            WHERE tenant_id = @TenantId AND org_id = @OrgId
            ORDER BY employee_full_name
            """,
            new { TenantId = tenantId, OrgId = orgId })).ToList();

        var summary = (await GetSummaryAsync(tenantId, orgId, ct)).Data ?? new LeaveSummaryDto();

        return Respons<LeaveListDto>.Ok(
            new LeaveListDto { Summary = summary, Requests = requests, Balances = balances },
            pagination: new PaginationMeta
            {
                Page = paging.Page, Size = paging.Size, Total = total,
                HasNext = paging.Offset + requests.Count < total,
            });
    }

    public async Task<Respons<LeaveRequestListItemDto>> GetRequestByIdAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default)
    {
        await using var connection = await _database.GetConnectionAsync(ct);
        var row = await connection.QuerySingleOrDefaultAsync<LeaveRequestListItemDto>(
            """
            SELECT
                id::text AS LeaveRequestId,
                employee_id::text AS EmployeeId,
                employee_full_name AS EmployeeFullName,
                leave_type AS LeaveType,
                start_date AS StartDate,
                end_date AS EndDate,
                days_requested AS DaysRequested,
                status AS Status,
                approver_name AS ApproverName
            FROM zeloshr.zhr_leave_requests
            WHERE id = @Id AND tenant_id = @TenantId AND org_id = @OrgId
            """,
            new { Id = id, TenantId = tenantId, OrgId = orgId });

        return row is null
            ? Respons<LeaveRequestListItemDto>.Fail("Leave request not found.", statusCode: 404)
            : Respons<LeaveRequestListItemDto>.Ok(row);
    }

    public async Task<Respons<LeaveRequestListItemDto>> CreateRequestAsync(
        CreateLeaveRequestDto data,
        string tenantId,
        string orgId,
        CancellationToken ct = default)
    {
        if (data.EndDate < data.StartDate)
            return Respons<LeaveRequestListItemDto>.ValidationError(
                new Dictionary<string, string> { ["endDate"] = "End date must be on or after start date." });

        var employee = await _employees.ResolveEmployeeDisplayAsync(data.EmployeeId, tenantId, orgId, ct);
        if (employee is null)
            return Respons<LeaveRequestListItemDto>.ValidationError(
                new Dictionary<string, string> { ["employeeId"] = "Employee not found." });

        await using var connection = await _database.GetConnectionAsync(ct);
        var id = await connection.QuerySingleAsync<Guid>(
            """
            INSERT INTO zeloshr.zhr_leave_requests (
                tenant_id, org_id, employee_id, employee_full_name, leave_type,
                start_date, end_date, days_requested, status
            )
            VALUES (
                @TenantId, @OrgId, @EmployeeId, @EmployeeFullName, @LeaveType,
                @StartDate, @EndDate, @DaysRequested, 'Pending'
            )
            RETURNING id
            """,
            new
            {
                TenantId = tenantId,
                OrgId = orgId,
                EmployeeId = data.EmployeeId,
                EmployeeFullName = employee.FullName,
                LeaveType = data.LeaveType!.Trim(),
                data.StartDate,
                data.EndDate,
                data.DaysRequested,
            });

        return await GetRequestByIdAsync(id, tenantId, orgId, ct);
    }

    public async Task<Respons<LeaveRequestListItemDto>> UpdateRequestAsync(
        Guid id,
        UpdateLeaveRequestDto data,
        string tenantId,
        string orgId,
        CancellationToken ct = default)
    {
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "Pending", "Approved", "Rejected", "Cancelled" };
        if (!string.IsNullOrWhiteSpace(data.Status) && !allowed.Contains(data.Status))
            return Respons<LeaveRequestListItemDto>.ValidationError(
                new Dictionary<string, string> { ["status"] = "Invalid status." });

        var sets = new List<string>();
        var parameters = new DynamicParameters(new { Id = id, TenantId = tenantId, OrgId = orgId });
        if (!string.IsNullOrWhiteSpace(data.Status))
        {
            sets.Add("status = @Status");
            parameters.Add("Status", data.Status.Trim());
        }
        if (data.ApproverName is not null)
        {
            sets.Add("approver_name = @ApproverName");
            parameters.Add("ApproverName", string.IsNullOrWhiteSpace(data.ApproverName) ? null : data.ApproverName.Trim());
        }

        if (sets.Count == 0)
            return Respons<LeaveRequestListItemDto>.Fail("No fields to update.", statusCode: 400);

        await using var connection = await _database.GetConnectionAsync(ct);
        var affected = await connection.ExecuteAsync(
            $"""
            UPDATE zeloshr.zhr_leave_requests SET {string.Join(", ", sets)}
            WHERE id = @Id AND tenant_id = @TenantId AND org_id = @OrgId
            """,
            parameters);

        if (affected == 0)
            return Respons<LeaveRequestListItemDto>.Fail("Leave request not found.", statusCode: 404);

        return await GetRequestByIdAsync(id, tenantId, orgId, ct);
    }

    public async Task<Respons<object>> DeleteRequestAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default)
    {
        await using var connection = await _database.GetConnectionAsync(ct);
        var affected = await connection.ExecuteAsync(
            """
            DELETE FROM zeloshr.zhr_leave_requests
            WHERE id = @Id AND tenant_id = @TenantId AND org_id = @OrgId AND status = 'Pending'
            """,
            new { Id = id, TenantId = tenantId, OrgId = orgId });

        if (affected == 0)
            return Respons<object>.Fail("Leave request not found or cannot be deleted (only Pending).", statusCode: 404);

        return Respons<object>.Ok(new { leaveRequestId = id.ToString() }, "Leave request deleted.");
    }
}
