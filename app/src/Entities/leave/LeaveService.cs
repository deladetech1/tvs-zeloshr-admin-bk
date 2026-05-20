using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Pagination;

namespace ZelosHR.Api.Entities.Leave;

public class LeaveService
{
    private readonly ILeaveRepository _leave;
    private readonly EmployeesService _employees;

    public LeaveService(ILeaveRepository leave, EmployeesService employees)
    {
        _leave = leave;
        _employees = employees;
    }

    public async Task<Respons<LeaveSummaryDto>> GetSummaryAsync(
        string tenantId, string orgId, CancellationToken ct = default)
    {
        var summary = await _leave.GetSummaryScopedAsync(tenantId, orgId, ct);
        return Respons<LeaveSummaryDto>.Ok(summary);
    }

    public async Task<Respons<LeaveListDto>> ListAsync(
        string? search, string? status, string? leaveType,
        int page, int size, string tenantId, string orgId, CancellationToken ct = default)
    {
        var paging = PagedQuery.From(page, size);
        var (requests, balances, total) = await _leave.ListScopedAsync(
            tenantId, orgId, search, status, leaveType, paging.Page, paging.Size, ct);
        var summary = await _leave.GetSummaryScopedAsync(tenantId, orgId, ct);

        return Respons<LeaveListDto>.Ok(
            new LeaveListDto { Summary = summary, Requests = requests, Balances = balances },
            pagination: new PaginationMeta
            {
                Page = paging.Page,
                Size = paging.Size,
                Total = total,
                HasNext = paging.Offset + requests.Count < total,
            });
    }

    public async Task<Respons<LeaveRequestListItemDto>> GetRequestByIdAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default)
    {
        var row = await _leave.GetRequestByIdScopedAsync(id, tenantId, orgId, ct);
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

        var id = await _leave.CreateRequestScopedAsync(
            tenantId,
            orgId,
            data.EmployeeId,
            employee.FullName,
            data.LeaveType!.Trim(),
            data.StartDate,
            data.EndDate,
            data.DaysRequested,
            ct);

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

        var hasStatus = !string.IsNullOrWhiteSpace(data.Status);
        var hasApprover = data.ApproverName is not null;
        if (!hasStatus && !hasApprover)
            return Respons<LeaveRequestListItemDto>.Fail("No fields to update.", statusCode: 400);

        var updated = await _leave.UpdateRequestScopedAsync(
            id,
            tenantId,
            orgId,
            hasStatus ? data.Status : null,
            hasApprover ? data.ApproverName : null,
            ct);

        if (updated is null)
        {
            var exists = await _leave.GetRequestByIdScopedAsync(id, tenantId, orgId, ct);
            return exists is null
                ? Respons<LeaveRequestListItemDto>.Fail("Leave request not found.", statusCode: 404)
                : Respons<LeaveRequestListItemDto>.Fail("No fields to update.", statusCode: 400);
        }

        return Respons<LeaveRequestListItemDto>.Ok(updated);
    }

    public async Task<Respons<object>> DeleteRequestAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default)
    {
        if (!await _leave.DeletePendingRequestScopedAsync(id, tenantId, orgId, ct))
            return Respons<object>.Fail("Leave request not found or cannot be deleted (only Pending).", statusCode: 404);

        return Respons<object>.Ok(new { leaveRequestId = id.ToString() }, "Leave request deleted.");
    }
}
