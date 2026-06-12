using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Pagination;

namespace ZelosHR.Api.Entities.Leave;

public class LeaveService
{
    private readonly ILeaveRepository _leave;
    private readonly IEmployeeLookup _employees;

    public LeaveService(ILeaveRepository leave, IEmployeeLookup employees)
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
        string? search, string? status, Guid? leaveTypeId, Guid? employeeId,
        int page, int size, string tenantId, string orgId, CancellationToken ct = default)
    {
        var paging = PagedQuery.From(page, size);
        var (requests, total) = await _leave.ListRequestsScopedAsync(
            tenantId, orgId, search, status, leaveTypeId, employeeId, paging.Page, paging.Size, ct);
        var summary = await _leave.GetSummaryScopedAsync(tenantId, orgId, ct);

        return Respons<LeaveListDto>.Ok(
            new LeaveListDto { Summary = summary, Requests = requests },
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
        var validation = ValidateRequestDates(data.StartDate, data.EndDate, data.DaysRequested);
        if (validation is not null)
            return validation;

        var leaveType = await _leave.GetTypeByIdScopedAsync(data.LeaveTypeId, tenantId, orgId, ct);
        if (leaveType is null)
            return Respons<LeaveRequestListItemDto>.ValidationError(
                new Dictionary<string, string> { ["leaveTypeId"] = "Leave type not found." });

        var employee = await _employees.ResolveEmployeeDisplayAsync(data.EmployeeId, tenantId, orgId, ct);
        if (employee is null)
            return Respons<LeaveRequestListItemDto>.ValidationError(
                new Dictionary<string, string> { ["employeeId"] = "Employee not found." });

        var balanceCheck = await ValidateSufficientBalanceAsync(
            tenantId, orgId, data.EmployeeId, data.LeaveTypeId, data.DaysRequested, ct);
        if (balanceCheck is not null)
            return balanceCheck;

        var id = await _leave.CreateRequestScopedAsync(
            tenantId,
            orgId,
            data.EmployeeId,
            employee.FullName,
            data.LeaveTypeId,
            leaveType.Name,
            data.StartDate,
            data.EndDate,
            data.DaysRequested,
            data.Notes,
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
        if (!string.IsNullOrWhiteSpace(data.Status)
            && !LeaveFieldOptions.RequestStatuses.Any(
                s => s.Equals(data.Status, StringComparison.OrdinalIgnoreCase)))
            return Respons<LeaveRequestListItemDto>.ValidationError(
                new Dictionary<string, string> { ["status"] = "Invalid status." });

        var hasStatus = !string.IsNullOrWhiteSpace(data.Status);
        var hasNotes = data.Notes is not null;
        if (!hasStatus && !hasNotes)
            return Respons<LeaveRequestListItemDto>.EmptyUpdateRequest();

        var updated = await _leave.UpdateRequestScopedAsync(
            id,
            tenantId,
            orgId,
            hasStatus ? data.Status : null,
            hasNotes ? data.Notes : null,
            ct);

        if (updated is null)
        {
            var exists = await _leave.GetRequestByIdScopedAsync(id, tenantId, orgId, ct);
            return exists is null
                ? Respons<LeaveRequestListItemDto>.Fail("Leave request not found.", statusCode: 404)
                : Respons<LeaveRequestListItemDto>.EmptyUpdateRequest();
        }

        return Respons<LeaveRequestListItemDto>.Ok(updated);
    }

    public async Task<Respons<LeaveRequestListItemDto>> ApproveRequestAsync(
        Guid id, string? approverId, string tenantId, string orgId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(approverId))
            return Respons<LeaveRequestListItemDto>.Fail("Authenticated user id is required.", statusCode: 401);

        var existing = await _leave.GetRequestByIdScopedAsync(id, tenantId, orgId, ct);
        if (existing is null)
            return Respons<LeaveRequestListItemDto>.Fail("Leave request not found.", statusCode: 404);
        if (!existing.Status.Equals(LeaveRequestStatuses.Pending, StringComparison.OrdinalIgnoreCase))
            return Respons<LeaveRequestListItemDto>.Fail("Only pending requests can be approved.", statusCode: 409);

        if (!Guid.TryParse(existing.LeaveTypeId, out var leaveTypeId))
            return Respons<LeaveRequestListItemDto>.ValidationError(
                new Dictionary<string, string> { ["leaveTypeId"] = "Leave request has no leave type." });

        var balanceCheck = await ValidateSufficientBalanceAsync(
            tenantId, orgId, Guid.Parse(existing.EmployeeId), leaveTypeId, existing.DaysRequested, ct);
        if (balanceCheck is not null)
            return balanceCheck;

        var updated = await _leave.ApproveRequestScopedAsync(id, tenantId, orgId, approverId.Trim(), ct);
        return updated is null
            ? Respons<LeaveRequestListItemDto>.Fail("Leave request could not be approved.", statusCode: 409)
            : Respons<LeaveRequestListItemDto>.Ok(updated, "Leave request approved.");
    }

    public async Task<Respons<LeaveRequestListItemDto>> RejectRequestAsync(
        Guid id, string? approverId, string? notes, string tenantId, string orgId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(approverId))
            return Respons<LeaveRequestListItemDto>.Fail("Authenticated user id is required.", statusCode: 401);

        var existing = await _leave.GetRequestByIdScopedAsync(id, tenantId, orgId, ct);
        if (existing is null)
            return Respons<LeaveRequestListItemDto>.Fail("Leave request not found.", statusCode: 404);
        if (!existing.Status.Equals(LeaveRequestStatuses.Pending, StringComparison.OrdinalIgnoreCase))
            return Respons<LeaveRequestListItemDto>.Fail("Only pending requests can be rejected.", statusCode: 409);

        var updated = await _leave.RejectRequestScopedAsync(id, tenantId, orgId, approverId.Trim(), notes, ct);
        return updated is null
            ? Respons<LeaveRequestListItemDto>.Fail("Leave request could not be rejected.", statusCode: 409)
            : Respons<LeaveRequestListItemDto>.Ok(updated, "Leave request rejected.");
    }

    public async Task<Respons<object>> DeleteRequestAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default)
    {
        if (!await _leave.DeletePendingRequestScopedAsync(id, tenantId, orgId, ct))
            return Respons<object>.Fail("Leave request not found or cannot be deleted (only Pending).", statusCode: 404);

        return Respons<object>.Ok(new { leaveRequestId = id.ToString() }, "Leave request deleted.");
    }

    public async Task<Respons<LeaveMySummaryDto>> GetMySummaryAsync(
        string? platformUserId, string tenantId, string orgId, CancellationToken ct = default)
    {
        var employee = await ResolveMyEmployeeAsync(platformUserId, tenantId, orgId, ct);
        if (employee is null)
            return Respons<LeaveMySummaryDto>.Fail("No employee profile linked to this user.", statusCode: 404);

        var balances = await _leave.ListBalancesScopedAsync(tenantId, orgId, employee.Value.EmployeeId, null, ct);
        var pendingTotal = await _leave.CountEmployeeRequestsScopedAsync(
            tenantId, orgId, employee.Value.EmployeeId, LeaveRequestStatuses.Pending, null, ct);
        var yearStart = new DateOnly(DateTime.UtcNow.Year, 1, 1);
        var approvedThisYear = await _leave.CountEmployeeRequestsScopedAsync(
            tenantId, orgId, employee.Value.EmployeeId, LeaveRequestStatuses.Approved, yearStart, ct);

        return Respons<LeaveMySummaryDto>.Ok(new LeaveMySummaryDto
        {
            TotalRemainingDays = balances.Sum(b => b.RemainingDays),
            PendingRequests = pendingTotal,
            ApprovedThisYear = approvedThisYear,
            Balances = balances,
        });
    }

    public async Task<Respons<LeaveMyRequestListDto>> ListMyRequestsAsync(
        string? platformUserId, string? status, int page, int size,
        string tenantId, string orgId, CancellationToken ct = default)
    {
        var employee = await ResolveMyEmployeeAsync(platformUserId, tenantId, orgId, ct);
        if (employee is null)
            return Respons<LeaveMyRequestListDto>.Fail("No employee profile linked to this user.", statusCode: 404);

        var paging = PagedQuery.From(page, size);
        var (requests, total) = await _leave.ListRequestsScopedAsync(
            tenantId, orgId, null, status, null, employee.Value.EmployeeId, paging.Page, paging.Size, ct);

        return Respons<LeaveMyRequestListDto>.Ok(
            new LeaveMyRequestListDto { Requests = requests },
            pagination: new PaginationMeta
            {
                Page = paging.Page,
                Size = paging.Size,
                Total = total,
                HasNext = paging.Offset + requests.Count < total,
            });
    }

    public async Task<Respons<LeaveBalanceListDto>> ListMyBalancesAsync(
        string? platformUserId, string tenantId, string orgId, CancellationToken ct = default)
    {
        var employee = await ResolveMyEmployeeAsync(platformUserId, tenantId, orgId, ct);
        if (employee is null)
            return Respons<LeaveBalanceListDto>.Fail("No employee profile linked to this user.", statusCode: 404);

        var items = await _leave.ListBalancesScopedAsync(tenantId, orgId, employee.Value.EmployeeId, null, ct);
        return Respons<LeaveBalanceListDto>.Ok(new LeaveBalanceListDto { Items = items });
    }

    public async Task<Respons<LeaveRequestListItemDto>> CreateMyRequestAsync(
        CreateMyLeaveRequestDto data,
        string? platformUserId,
        string tenantId,
        string orgId,
        CancellationToken ct = default)
    {
        var employee = await ResolveMyEmployeeAsync(platformUserId, tenantId, orgId, ct);
        if (employee is null)
            return Respons<LeaveRequestListItemDto>.Fail("No employee profile linked to this user.", statusCode: 404);

        return await CreateRequestAsync(
            new CreateLeaveRequestDto
            {
                EmployeeId = employee.Value.EmployeeId,
                LeaveTypeId = data.LeaveTypeId,
                StartDate = data.StartDate,
                EndDate = data.EndDate,
                DaysRequested = data.DaysRequested,
                Notes = data.Notes,
            },
            tenantId,
            orgId,
            ct);
    }

    public async Task<Respons<LeaveBalanceListDto>> ListBalancesAsync(
        Guid? employeeId, Guid? leaveTypeId, string tenantId, string orgId, CancellationToken ct = default)
    {
        var items = await _leave.ListBalancesScopedAsync(tenantId, orgId, employeeId, leaveTypeId, ct);
        return Respons<LeaveBalanceListDto>.Ok(new LeaveBalanceListDto { Items = items });
    }

    public async Task<Respons<LeaveBalanceListItemDto>> GetBalanceByIdAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default)
    {
        var row = await _leave.GetBalanceScopedAsync(id, tenantId, orgId, ct);
        return row is null
            ? Respons<LeaveBalanceListItemDto>.Fail("Leave balance not found.", statusCode: 404)
            : Respons<LeaveBalanceListItemDto>.Ok(row);
    }

    public async Task<Respons<LeaveBalanceListItemDto>> CreateBalanceAsync(
        CreateLeaveBalanceDto data, string tenantId, string orgId, CancellationToken ct = default)
    {
        var leaveType = await _leave.GetTypeByIdScopedAsync(data.LeaveTypeId, tenantId, orgId, ct);
        if (leaveType is null)
            return Respons<LeaveBalanceListItemDto>.ValidationError(
                new Dictionary<string, string> { ["leaveTypeId"] = "Leave type not found." });

        var employee = await _employees.ResolveEmployeeDisplayAsync(data.EmployeeId, tenantId, orgId, ct);
        if (employee is null)
            return Respons<LeaveBalanceListItemDto>.ValidationError(
                new Dictionary<string, string> { ["employeeId"] = "Employee not found." });

        if (data.UsedDays > data.EntitledDays)
            return Respons<LeaveBalanceListItemDto>.ValidationError(
                new Dictionary<string, string> { ["usedDays"] = "Used days cannot exceed entitled days." });

        var existing = await _leave.GetBalanceForEmployeeScopedAsync(
            tenantId, orgId, data.EmployeeId, data.LeaveTypeId, ct);
        if (existing is not null)
            return Respons<LeaveBalanceListItemDto>.ValidationError(
                new Dictionary<string, string> { ["leaveTypeId"] = "A balance for this employee and leave type already exists." });

        var id = await _leave.CreateBalanceScopedAsync(
            tenantId,
            orgId,
            data.EmployeeId,
            employee.FullName,
            data.LeaveTypeId,
            leaveType.Name,
            data.EntitledDays,
            data.UsedDays,
            ct);

        return await GetBalanceByIdAsync(id, tenantId, orgId, ct);
    }

    public async Task<Respons<LeaveBalanceListItemDto>> UpdateBalanceAsync(
        Guid id, UpdateLeaveBalanceDto data, string tenantId, string orgId, CancellationToken ct = default)
    {
        if (!data.EntitledDays.HasValue && !data.UsedDays.HasValue)
            return Respons<LeaveBalanceListItemDto>.EmptyUpdateRequest();

        var current = await _leave.GetBalanceScopedAsync(id, tenantId, orgId, ct);
        if (current is null)
            return Respons<LeaveBalanceListItemDto>.Fail("Leave balance not found.", statusCode: 404);

        var entitled = data.EntitledDays ?? current.EntitledDays;
        var used = data.UsedDays ?? current.UsedDays;
        if (used > entitled)
            return Respons<LeaveBalanceListItemDto>.ValidationError(
                new Dictionary<string, string> { ["usedDays"] = "Used days cannot exceed entitled days." });

        var updated = await _leave.UpdateBalanceScopedAsync(id, tenantId, orgId, data.EntitledDays, data.UsedDays, ct);
        return updated is null
            ? Respons<LeaveBalanceListItemDto>.EmptyUpdateRequest()
            : Respons<LeaveBalanceListItemDto>.Ok(updated);
    }

    public async Task<Respons<LeaveTypeListDto>> ListTypesAsync(
        string? countryCode, bool activeOnly, string tenantId, string orgId, CancellationToken ct = default)
    {
        var items = await _leave.ListTypesScopedAsync(tenantId, orgId, countryCode, activeOnly, ct);
        return Respons<LeaveTypeListDto>.Ok(new LeaveTypeListDto { Items = items });
    }

    public async Task<Respons<LeaveTypeListItemDto>> GetTypeByIdAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default)
    {
        var row = await _leave.GetTypeByIdScopedAsync(id, tenantId, orgId, ct);
        return row is null
            ? Respons<LeaveTypeListItemDto>.Fail("Leave type not found.", statusCode: 404)
            : Respons<LeaveTypeListItemDto>.Ok(row);
    }

    public async Task<Respons<LeaveTypeListItemDto>> CreateTypeAsync(
        CreateLeaveTypeDto data, string tenantId, string orgId, CancellationToken ct = default)
    {
        var name = data.Name!.Trim();
        if (await _leave.TypeNameExistsScopedAsync(tenantId, orgId, name, null, ct))
            return Respons<LeaveTypeListItemDto>.ValidationError(
                new Dictionary<string, string> { ["name"] = "A leave type with this name already exists." });

        var id = await _leave.CreateTypeScopedAsync(tenantId, orgId, data, ct);
        return await GetTypeByIdAsync(id, tenantId, orgId, ct);
    }

    public async Task<Respons<LeaveTypeListItemDto>> UpdateTypeAsync(
        Guid id, UpdateLeaveTypeDto data, string tenantId, string orgId, CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(data.Name)
            && await _leave.TypeNameExistsScopedAsync(tenantId, orgId, data.Name.Trim(), id, ct))
            return Respons<LeaveTypeListItemDto>.ValidationError(
                new Dictionary<string, string> { ["name"] = "A leave type with this name already exists." });

        var updated = await _leave.UpdateTypeScopedAsync(id, tenantId, orgId, data, ct);
        if (updated is null)
        {
            var exists = await _leave.GetTypeByIdScopedAsync(id, tenantId, orgId, ct);
            return exists is null
                ? Respons<LeaveTypeListItemDto>.Fail("Leave type not found.", statusCode: 404)
                : Respons<LeaveTypeListItemDto>.EmptyUpdateRequest();
        }

        return Respons<LeaveTypeListItemDto>.Ok(updated);
    }

    public async Task<Respons<object>> DeleteTypeAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default)
    {
        if (!await _leave.DeleteTypeScopedAsync(id, tenantId, orgId, ct))
            return Respons<object>.Fail("Leave type not found.", statusCode: 404);

        return Respons<object>.Ok(new { leaveTypeId = id.ToString() }, "Leave type removed or deactivated.");
    }

    public async Task<Respons<PublicHolidayListDto>> ListHolidaysAsync(
        string? countryCode, int? year, Guid? branchId, int page, int size,
        string tenantId, string orgId, CancellationToken ct = default)
    {
        var paging = PagedQuery.From(page, size);
        var (items, total) = await _leave.ListHolidaysScopedAsync(
            tenantId, orgId, countryCode, year, branchId, paging.Page, paging.Size, ct);

        return Respons<PublicHolidayListDto>.Ok(
            new PublicHolidayListDto { Items = items },
            pagination: new PaginationMeta
            {
                Page = paging.Page,
                Size = paging.Size,
                Total = total,
                HasNext = paging.Offset + items.Count < total,
            });
    }

    public async Task<Respons<PublicHolidayListItemDto>> GetHolidayByIdAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default)
    {
        var row = await _leave.GetHolidayByIdScopedAsync(id, tenantId, orgId, ct);
        return row is null
            ? Respons<PublicHolidayListItemDto>.Fail("Public holiday not found.", statusCode: 404)
            : Respons<PublicHolidayListItemDto>.Ok(row);
    }

    public async Task<Respons<PublicHolidayListItemDto>> CreateHolidayAsync(
        CreatePublicHolidayDto data, string tenantId, string orgId, CancellationToken ct = default)
    {
        var id = await _leave.CreateHolidayScopedAsync(tenantId, orgId, data, ct);
        return await GetHolidayByIdAsync(id, tenantId, orgId, ct);
    }

    public async Task<Respons<PublicHolidayListItemDto>> UpdateHolidayAsync(
        Guid id, UpdatePublicHolidayDto data, string tenantId, string orgId, CancellationToken ct = default)
    {
        var updated = await _leave.UpdateHolidayScopedAsync(id, tenantId, orgId, data, ct);
        if (updated is null)
        {
            var exists = await _leave.GetHolidayByIdScopedAsync(id, tenantId, orgId, ct);
            return exists is null
                ? Respons<PublicHolidayListItemDto>.Fail("Public holiday not found.", statusCode: 404)
                : Respons<PublicHolidayListItemDto>.EmptyUpdateRequest();
        }

        return Respons<PublicHolidayListItemDto>.Ok(updated);
    }

    public async Task<Respons<object>> DeleteHolidayAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default)
    {
        if (!await _leave.DeleteHolidayScopedAsync(id, tenantId, orgId, ct))
            return Respons<object>.Fail("Public holiday not found.", statusCode: 404);

        return Respons<object>.Ok(new { holidayId = id.ToString() }, "Public holiday removed.");
    }

    private async Task<(Guid EmployeeId, EmployeeDisplayInfo Display)?> ResolveMyEmployeeAsync(
        string? platformUserId, string tenantId, string orgId, CancellationToken ct) =>
        string.IsNullOrWhiteSpace(platformUserId)
            ? null
            : await _employees.ResolveByPlatformUserAsync(platformUserId, tenantId, orgId, ct);

    private static Respons<LeaveRequestListItemDto>? ValidateRequestDates(
        DateOnly startDate, DateOnly endDate, decimal daysRequested)
    {
        if (endDate < startDate)
            return Respons<LeaveRequestListItemDto>.ValidationError(
                new Dictionary<string, string> { ["endDate"] = "End date must be on or after start date." });
        if (daysRequested <= 0)
            return Respons<LeaveRequestListItemDto>.ValidationError(
                new Dictionary<string, string> { ["daysRequested"] = "Days requested must be greater than zero." });
        return null;
    }

    private async Task<Respons<LeaveRequestListItemDto>?> ValidateSufficientBalanceAsync(
        string tenantId,
        string orgId,
        Guid employeeId,
        Guid leaveTypeId,
        decimal daysRequested,
        CancellationToken ct)
    {
        var balance = await _leave.GetBalanceForEmployeeScopedAsync(tenantId, orgId, employeeId, leaveTypeId, ct);
        if (balance is null)
            return null;

        if (balance.RemainingDays < daysRequested)
            return Respons<LeaveRequestListItemDto>.ValidationError(
                new Dictionary<string, string>
                {
                    ["daysRequested"] = $"Insufficient leave balance. Remaining: {balance.RemainingDays} day(s).",
                });

        return null;
    }
}
