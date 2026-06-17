using ZelosHR.Api.Entities.Countries;
using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Entities.Files;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Pagination;

namespace ZelosHR.Api.Entities.Leave;

public class LeaveService
{
    private readonly ILeaveRepository _leave;
    private readonly IEmployeeLookup _employees;
    private readonly ICpUserRepository _cpUsers;
    private readonly HrDocumentPresignedUrlService _profileUrls;

    public LeaveService(
        ILeaveRepository leave,
        IEmployeeLookup employees,
        ICpUserRepository cpUsers,
        HrDocumentPresignedUrlService profileUrls)
    {
        _leave = leave;
        _employees = employees;
        _cpUsers = cpUsers;
        _profileUrls = profileUrls;
    }

    public async Task<Respons<LeaveSummaryDto>> GetSummaryAsync(
        string tenantId, string orgId, CancellationToken ct = default)
    {
        var summary = await _leave.GetSummaryScopedAsync(tenantId, orgId, ct);
        return Respons<LeaveSummaryDto>.Ok(summary);
    }

    public async Task<Respons<LeaveDashboardDto>> GetDashboardAsync(
        string tenantId, string orgId, CancellationToken ct = default) =>
        Respons<LeaveDashboardDto>.Ok(await BuildDashboardAsync(tenantId, orgId, ct));

    private async Task<LeaveDashboardDto> BuildDashboardAsync(
        string tenantId, string orgId, CancellationToken ct)
    {
        var summary = await _leave.GetSummaryScopedAsync(tenantId, orgId, ct);
        var onLeaveToday = await _leave.ListOnLeaveTodayScopedAsync(tenantId, orgId, 5, ct);
        var pendingApprovals = await _leave.ListPendingApprovalsScopedAsync(tenantId, orgId, 5, ct);
        var leavingThisWeek = await _leave.ListLeavingThisWeekScopedAsync(tenantId, orgId, 10, ct);
        var widgets = await MapDashboardWidgetsAsync(
            onLeaveToday, pendingApprovals, leavingThisWeek, tenantId, orgId, ct);

        return new LeaveDashboardDto
        {
            Summary = new LeaveDashboardSummaryDto
            {
                OnLeaveToday = summary.OnLeaveToday,
                PendingApprovals = summary.PendingRequests,
                LeavingThisWeek = summary.LeavingThisWeek,
                LowBalanceAlert = summary.LowBalanceAlert,
            },
            OnLeaveToday = widgets.OnLeaveToday,
            PendingApprovals = widgets.PendingApprovals,
            LeavingThisWeek = widgets.LeavingThisWeek,
        };
    }

    private async Task<(
        IReadOnlyList<LeaveDashboardOnLeaveItemDto> OnLeaveToday,
        IReadOnlyList<LeaveDashboardPendingItemDto> PendingApprovals,
        IReadOnlyList<LeaveDashboardLeavingItemDto> LeavingThisWeek)> MapDashboardWidgetsAsync(
        IReadOnlyList<LeaveRequestRawRow> onLeaveToday,
        IReadOnlyList<LeaveRequestRawRow> pendingApprovals,
        IReadOnlyList<LeaveRequestRawRow> leavingThisWeek,
        string tenantId,
        string orgId,
        CancellationToken ct)
    {
        var allRows = onLeaveToday.Concat(pendingApprovals).Concat(leavingThisWeek).ToList();
        if (allRows.Count == 0)
            return ([], [], []);

        var employeeIds = allRows
            .Select(r => Guid.TryParse(r.EmployeeId, out var id) ? id : (Guid?)null)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct();
        var employees = await _employees.ResolveLeaveContextsAsync(employeeIds, tenantId, orgId, ct);
        var leaveTypes = LeaveMapper.MergeLeaveTypeLookups(
            LeaveMapper.IndexTypes(await _leave.ListAllTypesScopedAsync(tenantId, orgId, false, ct)),
            allRows.Select(r => (r.LeaveTypeId, r.LeaveTypeName)));
        var userNames = await LeaveMapper.ResolveApproverNamesAsync(
            _cpUsers, LeaveMapper.CollectUserIds(allRows), tenantId, ct);
        var profileUrlsByEmployeeId = await ResolveEmployeeProfileUrlsAsync(employees, ct);

        return (
            onLeaveToday
                .Select(row => LeaveMapper.MapDashboardOnLeaveItem(
                    row, employees, leaveTypes, userNames, profileUrlsByEmployeeId))
                .ToList(),
            pendingApprovals
                .Select(row => LeaveMapper.MapDashboardPendingItem(
                    row, employees, leaveTypes, userNames, profileUrlsByEmployeeId))
                .ToList(),
            leavingThisWeek
                .Select(row => LeaveMapper.MapDashboardLeavingItem(
                    row, employees, leaveTypes, userNames, profileUrlsByEmployeeId))
                .ToList());
    }

    private async Task<LeavePersonalSummaryDto> BuildPersonalSummaryAsync(
        (Guid EmployeeId, string OrgId, EmployeeDisplayInfo Display) employee,
        string tenantId,
        CancellationToken ct)
    {
        var balances = await EnrichBalancesAsync(
            await _leave.ListBalancesScopedAsync(tenantId, employee.OrgId, employee.EmployeeId, null, ct),
            tenantId,
            employee.OrgId,
            ct);
        var pendingTotal = await _leave.CountEmployeeRequestsScopedAsync(
            tenantId, employee.OrgId, employee.EmployeeId, LeaveRequestStatuses.Pending, null, ct);
        var yearStart = new DateOnly(DateTime.UtcNow.Year, 1, 1);
        var approvedThisYear = await _leave.CountEmployeeRequestsScopedAsync(
            tenantId, employee.OrgId, employee.EmployeeId, LeaveRequestStatuses.Approved, yearStart, ct);

        return new LeavePersonalSummaryDto
        {
            TotalRemainingDays = balances.Sum(b => b.RemainingDays),
            PendingRequests = pendingTotal,
            ApprovedThisYear = approvedThisYear,
            Balances = balances,
        };
    }

    public async Task<Respons<LeaveListDto>> ListAsync(
        LeaveRequestListQuery query, string tenantId, string orgId, CancellationToken ct = default)
    {
        var paging = PagedQuery.From(query.Page, query.Size);
        var listQuery = query with { Page = paging.Page, Size = paging.Size };
        var (requests, total) = await _leave.ListRequestsScopedAsync(tenantId, orgId, listQuery, ct);
        var summary = await _leave.GetSummaryScopedAsync(tenantId, orgId, ct);
        var items = await EnrichRequestsAsync(requests, tenantId, orgId, ct);

        return Respons<LeaveListDto>.Ok(
            new LeaveListDto { Summary = summary, Items = items },
            pagination: new PaginationMeta
            {
                Page = paging.Page,
                Size = paging.Size,
                Total = total,
                HasNext = paging.Offset + items.Count < total,
            });
    }

    public async Task<Respons<LeaveApprovalListDto>> ListApprovalsAsync(
        LeaveApprovalListQuery query, string tenantId, string orgId, CancellationToken ct = default)
    {
        var paging = PagedQuery.From(query.Page, query.Size);
        var tab = string.IsNullOrWhiteSpace(query.Tab) ? LeaveApprovalListTabs.Pending : query.Tab.Trim();
        var listQuery = new LeaveRequestListQuery
        {
            Search = query.Search,
            LeaveTypeId = query.LeaveTypeId,
            DepartmentId = query.DepartmentId,
            FromDate = query.FromDate,
            ToDate = query.ToDate,
            Tab = tab,
            SortBy = query.SortBy ?? "name",
            SortOrder = query.SortOrder ?? "asc",
            Page = paging.Page,
            Size = paging.Size,
        };

        var (requests, total) = await _leave.ListRequestsScopedAsync(tenantId, orgId, listQuery, ct);
        var summary = await _leave.GetSummaryScopedAsync(tenantId, orgId, ct);
        var items = await EnrichApprovalListAsync(requests, tenantId, orgId, ct);

        return Respons<LeaveApprovalListDto>.Ok(
            new LeaveApprovalListDto { PendingCount = summary.PendingFinalApprovals, Items = items },
            pagination: new PaginationMeta
            {
                Page = paging.Page,
                Size = paging.Size,
                Total = total,
                HasNext = paging.Offset + items.Count < total,
            });
    }

    public async Task<Respons<LeaveCalendarDto>> GetCalendarAsync(
        LeaveCalendarQuery query, string tenantId, string orgId, CancellationToken ct = default)
    {
        var paging = PagedQuery.From(query.Page, query.Size);
        var calendarQuery = query with { Page = paging.Page, Size = paging.Size };
        var scoped = await _leave.ListCalendarScopedAsync(tenantId, orgId, calendarQuery, ct);

        var employees = await _employees.ResolveLeaveContextsAsync(scoped.EmployeeIds, tenantId, orgId, ct);
        var profileUrlsByEmployeeId = await ResolveEmployeeProfileUrlsAsync(employees, ct);

        var allLeaveRows = scoped.LeaveByEmployeeId.Values.SelectMany(rows => rows).ToList();
        var leaveTypes = LeaveMapper.MergeLeaveTypeLookups(
            LeaveMapper.IndexTypes(await _leave.ListAllTypesScopedAsync(tenantId, orgId, false, ct)),
            allLeaveRows.Select(r => (r.LeaveTypeId, r.LeaveTypeName)));

        var items = scoped.EmployeeIds
            .SelectMany(employeeId =>
            {
                scoped.LeaveByEmployeeId.TryGetValue(employeeId, out var leaveRows);
                return LeaveMapper.ExpandCalendarItems(
                    employeeId,
                    employees,
                    profileUrlsByEmployeeId,
                    leaveRows ?? [],
                    leaveTypes);
            })
            .ToList();

        return Respons<LeaveCalendarDto>.Ok(
            new LeaveCalendarDto
            {
                View = scoped.View,
                AnchorDate = scoped.AnchorDate,
                FromDate = scoped.FromDate,
                ToDate = scoped.ToDate,
                Items = items,
            },
            pagination: new PaginationMeta
            {
                Page = paging.Page,
                Size = paging.Size,
                Total = scoped.TotalEmployees,
                HasNext = paging.Offset + scoped.EmployeeIds.Count < scoped.TotalEmployees,
            });
    }

    public async Task<Respons<LeaveRequestDetailDto>> GetRequestByIdAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default)
    {
        var row = await _leave.GetRequestRawByIdScopedAsync(id, tenantId, orgId, ct);
        if (row is null)
            return Respons<LeaveRequestDetailDto>.Fail("Leave request not found.", statusCode: 404);

        var items = await EnrichRequestsAsync([row], tenantId, orgId, ct);
        var item = items[0];

        var holidayDates = await _leave.GetPublicHolidayDatesInRangeScopedAsync(
            tenantId, orgId, row.StartDate, row.EndDate, null, null, ct);
        var workingDays = LeaveWorkingDaysCalculator.CountWorkingDays(row.StartDate, row.EndDate, holidayDates);
        var holidaysInRange = LeaveWorkingDaysCalculator.CountPublicHolidaysInRange(
            row.StartDate, row.EndDate, holidayDates);

        var approverNames = await LeaveMapper.ResolveApproverNamesAsync(
            _cpUsers, LeaveMapper.CollectUserIds(row), tenantId, ct);

        return Respons<LeaveRequestDetailDto>.Ok(
            LeaveMapper.ToDetail(
                item,
                row,
                workingDays,
                holidaysInRange,
                LeaveMapper.BuildBalanceImpact(row.RemainingDays, row.DaysRequested),
                approverNames));
    }

    public async Task<Respons<LeaveRequestDetailDto>> CreateRequestAsync(
        CreateLeaveRequestDto data,
        string tenantId,
        string orgId,
        string? actorUserId = null,
        CancellationToken ct = default)
    {
        var validation = ValidateRequestDates(data.StartDate, data.EndDate, data.DaysRequested);
        if (validation is not null)
            return validation;

        var leaveType = await _leave.GetTypeByIdScopedAsync(data.LeaveTypeId, tenantId, orgId, ct);
        if (leaveType is null)
            return Respons<LeaveRequestDetailDto>.ValidationError(
                new Dictionary<string, string> { ["leaveTypeId"] = "Leave type not found." });

        var employeeContexts = await _employees.ResolveLeaveContextsAsync(
            [data.EmployeeId], tenantId, orgId, ct);
        if (!employeeContexts.TryGetValue(data.EmployeeId, out var employeeContext))
            return Respons<LeaveRequestDetailDto>.ValidationError(
                new Dictionary<string, string> { ["employeeId"] = "Employee not found." });

        var balanceCheck = await ValidateSufficientBalanceAsync(
            tenantId, orgId, data.EmployeeId, data.LeaveTypeId, data.DaysRequested, ct);
        if (balanceCheck is not null)
            return balanceCheck;

        var id = await _leave.CreateRequestScopedAsync(
            tenantId,
            orgId,
            data.EmployeeId,
            employeeContext.FullName,
            data.LeaveTypeId,
            leaveType.Name,
            data.StartDate,
            data.EndDate,
            data.DaysRequested,
            data.Notes,
            ResolveInitialApprovalStage(employeeContext),
            actorUserId,
            ct);

        return await GetRequestByIdAsync(id, tenantId, orgId, ct);
    }

    public async Task<Respons<LeaveRequestDetailDto>> UpdateRequestAsync(
        Guid id,
        UpdateLeaveRequestDto data,
        string tenantId,
        string orgId,
        string? actorUserId = null,
        CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(data.Status)
            && !LeaveFieldOptions.RequestStatuses.Any(
                s => s.Equals(data.Status, StringComparison.OrdinalIgnoreCase)))
            return Respons<LeaveRequestDetailDto>.ValidationError(
                new Dictionary<string, string> { ["status"] = "Invalid status." });

        var hasStatus = !string.IsNullOrWhiteSpace(data.Status);
        var hasNotes = data.Notes is not null;
        if (!hasStatus && !hasNotes)
            return Respons<LeaveRequestDetailDto>.EmptyUpdateRequest();

        var updated = await _leave.UpdateRequestScopedAsync(
            id, tenantId, orgId, hasStatus ? data.Status : null, hasNotes ? data.Notes : null, actorUserId, ct);

        if (updated is null)
        {
            var exists = await _leave.GetRequestRawByIdScopedAsync(id, tenantId, orgId, ct);
            return exists is null
                ? Respons<LeaveRequestDetailDto>.Fail("Leave request not found.", statusCode: 404)
                : Respons<LeaveRequestDetailDto>.EmptyUpdateRequest();
        }

        return await GetRequestByIdAsync(id, tenantId, orgId, ct);
    }

    public async Task<Respons<LeaveRequestDetailDto>> ApproveRequestAsync(
        Guid id, string? approverPlatformUserId, string tenantId, string orgId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(approverPlatformUserId))
            return Respons<LeaveRequestDetailDto>.Fail("Authenticated user id is required.", statusCode: 401);

        var existing = await _leave.GetRequestRawByIdScopedAsync(id, tenantId, orgId, ct);
        if (existing is null)
            return Respons<LeaveRequestDetailDto>.Fail("Leave request not found.", statusCode: 404);
        if (!existing.Status.Equals(LeaveRequestStatuses.Pending, StringComparison.OrdinalIgnoreCase))
            return Respons<LeaveRequestDetailDto>.Fail("Only pending requests can be approved.", statusCode: 409);

        if (!Guid.TryParse(existing.EmployeeId, out var employeeId))
            return Respons<LeaveRequestDetailDto>.ValidationError(
                new Dictionary<string, string> { ["employeeId"] = "Leave request has no employee." });

        var employeeContexts = await _employees.ResolveLeaveContextsAsync(
            [employeeId], tenantId, orgId, ct);
        if (!employeeContexts.TryGetValue(employeeId, out var requestEmployee))
            return Respons<LeaveRequestDetailDto>.ValidationError(
                new Dictionary<string, string> { ["employeeId"] = "Employee not found." });

        var approverEmployee = await _employees.ResolveByPlatformUserAsync(
            approverPlatformUserId, tenantId, orgId, ct);

        if (existing.ApprovalStage.Equals(LeaveApprovalStages.PendingFinal, StringComparison.OrdinalIgnoreCase)
            && Guid.TryParse(existing.LeaveTypeId, out var leaveTypeId))
        {
            var balanceCheck = await ValidateSufficientBalanceAsync(
                tenantId, orgId, employeeId, leaveTypeId, existing.DaysRequested, ct);
            if (balanceCheck is not null)
                return balanceCheck;
        }

        var updated = await _leave.AdvanceApprovalScopedAsync(
            id,
            tenantId,
            orgId,
            approverPlatformUserId.Trim(),
            approverEmployee?.EmployeeId,
            requestEmployee,
            ct);

        if (updated is null)
            return Respons<LeaveRequestDetailDto>.Fail(
                "Leave request could not be approved at the current workflow stage.", statusCode: 409);

        var message = updated.ApprovalStage.Equals(LeaveApprovalStages.Approved, StringComparison.OrdinalIgnoreCase)
            ? "Leave request approved."
            : "Approval recorded. Request advanced to the next stage.";

        var result = await GetRequestByIdAsync(id, tenantId, orgId, ct);
        if (!result.Success || result.Data is null)
            return result;

        return Respons<LeaveRequestDetailDto>.Ok(result.Data, message, result.StatusCode);
    }

    public async Task<Respons<LeaveRequestDetailDto>> RejectRequestAsync(
        Guid id, string? approverId, string? notes, string tenantId, string orgId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(approverId))
            return Respons<LeaveRequestDetailDto>.Fail("Authenticated user id is required.", statusCode: 401);

        var existing = await _leave.GetRequestRawByIdScopedAsync(id, tenantId, orgId, ct);
        if (existing is null)
            return Respons<LeaveRequestDetailDto>.Fail("Leave request not found.", statusCode: 404);
        if (!existing.Status.Equals(LeaveRequestStatuses.Pending, StringComparison.OrdinalIgnoreCase))
            return Respons<LeaveRequestDetailDto>.Fail("Only pending requests can be rejected.", statusCode: 409);

        var updated = await _leave.RejectRequestScopedAsync(id, tenantId, orgId, approverId.Trim(), notes, ct);
        if (updated is null)
            return Respons<LeaveRequestDetailDto>.Fail("Leave request could not be rejected.", statusCode: 409);

        var result = await GetRequestByIdAsync(id, tenantId, orgId, ct);
        if (!result.Success || result.Data is null)
            return result;

        return Respons<LeaveRequestDetailDto>.Ok(result.Data, "Leave request rejected.", result.StatusCode);
    }

    public async Task<Respons<object>> DeleteRequestAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default)
    {
        if (!await _leave.DeletePendingRequestScopedAsync(id, tenantId, orgId, ct))
            return Respons<object>.Fail("Leave request not found or cannot be deleted (only Pending).", statusCode: 404);

        return Respons<object>.Ok(new { leaveRequestId = id.ToString() }, "Leave request deleted.");
    }

    public async Task<Respons<LeavePersonalSummaryDto>> GetMySummaryAsync(
        Guid employeeId, string tenantId, string orgId, CancellationToken ct = default)
    {
        var employee = await ResolveEmployeeAsync(employeeId, tenantId, orgId, ct);
        if (employee is null)
            return Respons<LeavePersonalSummaryDto>.Fail("Employee not found.", statusCode: 404);

        return Respons<LeavePersonalSummaryDto>.Ok(await BuildPersonalSummaryAsync(employee.Value, tenantId, ct));
    }

    public async Task<Respons<LeaveMyRequestListDto>> ListMyRequestsAsync(
        Guid employeeId, string? status, int page, int size,
        string tenantId, string orgId, CancellationToken ct = default)
    {
        var paging = PagedQuery.From(page, size);
        var employee = await ResolveEmployeeAsync(employeeId, tenantId, orgId, ct);
        if (employee is null)
            return Respons<LeaveMyRequestListDto>.Fail("Employee not found.", statusCode: 404);

        var (requests, total) = await _leave.ListRequestsScopedAsync(
            tenantId,
            employee.Value.OrgId,
            new LeaveRequestListQuery
            {
                Status = status,
                EmployeeId = employee.Value.EmployeeId,
                Page = paging.Page,
                Size = paging.Size,
            },
            ct);

        var items = await EnrichRequestsAsync(requests, tenantId, employee.Value.OrgId, ct);
        return Respons<LeaveMyRequestListDto>.Ok(
            new LeaveMyRequestListDto { Items = items },
            pagination: new PaginationMeta
            {
                Page = paging.Page,
                Size = paging.Size,
                Total = total,
                HasNext = paging.Offset + items.Count < total,
            });
    }

    public async Task<Respons<LeaveBalanceListDto>> ListMyBalancesAsync(
        Guid employeeId, string tenantId, string orgId, CancellationToken ct = default)
    {
        var employee = await ResolveEmployeeAsync(employeeId, tenantId, orgId, ct);
        if (employee is null)
            return Respons<LeaveBalanceListDto>.Fail("Employee not found.", statusCode: 404);

        var items = await EnrichBalancesAsync(
            await _leave.ListBalancesScopedAsync(tenantId, employee.Value.OrgId, employee.Value.EmployeeId, null, ct),
            tenantId,
            employee.Value.OrgId,
            ct);
        return Respons<LeaveBalanceListDto>.Ok(new LeaveBalanceListDto { Items = items });
    }

    public async Task<Respons<LeaveRequestDetailDto>> CreateMyRequestAsync(
        CreateMyLeaveRequestDto data,
        Guid employeeId,
        string? actorUserId,
        string tenantId,
        string orgId,
        CancellationToken ct = default)
    {
        var employee = await ResolveEmployeeAsync(employeeId, tenantId, orgId, ct);
        if (employee is null)
            return Respons<LeaveRequestDetailDto>.Fail("Employee not found.", statusCode: 404);

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
            employee.Value.OrgId,
            actorUserId,
            ct);
    }

    public async Task<Respons<LeaveBalanceListDto>> ListBalancesAsync(
        Guid? employeeId, Guid? leaveTypeId, string tenantId, string orgId, CancellationToken ct = default)
    {
        var items = await EnrichBalancesAsync(
            await _leave.ListBalancesScopedAsync(tenantId, orgId, employeeId, leaveTypeId, ct),
            tenantId,
            orgId,
            ct);
        return Respons<LeaveBalanceListDto>.Ok(new LeaveBalanceListDto { Items = items });
    }

    public async Task<Respons<LeaveBalanceListItemDto>> GetBalanceByIdAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default)
    {
        var row = await _leave.GetBalanceScopedAsync(id, tenantId, orgId, ct);
        if (row is null)
            return Respons<LeaveBalanceListItemDto>.Fail("Leave balance not found.", statusCode: 404);

        var items = await EnrichBalancesAsync([row], tenantId, orgId, ct);
        return Respons<LeaveBalanceListItemDto>.Ok(items[0]);
    }

    public async Task<Respons<LeaveBalanceListItemDto>> CreateBalanceAsync(
        CreateLeaveBalanceDto data, string tenantId, string orgId, string? actorUserId = null, CancellationToken ct = default)
    {
        var leaveType = await _leave.GetTypeByIdScopedAsync(data.LeaveTypeId, tenantId, orgId, ct);
        if (leaveType is null)
            return Respons<LeaveBalanceListItemDto>.ValidationError(
                new Dictionary<string, string> { ["leaveTypeId"] = "Leave type not found." });

        var employeeContexts = await _employees.ResolveLeaveContextsAsync(
            [data.EmployeeId], tenantId, orgId, ct);
        if (!employeeContexts.TryGetValue(data.EmployeeId, out var employee))
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
            actorUserId,
            ct);

        return await GetBalanceByIdAsync(id, tenantId, orgId, ct);
    }

    public async Task<Respons<LeaveBalanceListItemDto>> UpdateBalanceAsync(
        Guid id, UpdateLeaveBalanceDto data, string tenantId, string orgId, string? actorUserId = null, CancellationToken ct = default)
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

        var updated = await _leave.UpdateBalanceScopedAsync(id, tenantId, orgId, data.EntitledDays, data.UsedDays, actorUserId, ct);
        return updated is null
            ? Respons<LeaveBalanceListItemDto>.EmptyUpdateRequest()
            : await GetBalanceByIdAsync(id, tenantId, orgId, ct);
    }

    public async Task<Respons<LeaveTypeListDto>> ListTypesAsync(
        LeaveTypeListQuery query,
        string tenantId,
        string orgId,
        CancellationToken ct = default)
    {
        var paging = PagedQuery.From(query.Page, query.Size);
        var (rows, total) = await _leave.ListTypesScopedAsync(
            tenantId, orgId, query, paging.Page, paging.Size, ct);
        var items = await EnrichTypesAsync(rows, tenantId, ct);

        return Respons<LeaveTypeListDto>.Ok(
            new LeaveTypeListDto { Items = items },
            pagination: new PaginationMeta
            {
                Page = paging.Page,
                Size = paging.Size,
                Total = total,
                HasNext = paging.Offset + items.Count < total,
            });
    }

    public async Task<Respons<LeaveTypeListItemDto>> GetTypeByIdAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default)
    {
        var row = await _leave.GetTypeByIdScopedAsync(id, tenantId, orgId, ct);
        if (row is null)
            return Respons<LeaveTypeListItemDto>.Fail("Leave type not found.", statusCode: 404);

        var items = await EnrichTypesAsync([row], tenantId, ct);
        return Respons<LeaveTypeListItemDto>.Ok(items[0]);
    }

    public async Task<Respons<LeaveTypeListItemDto>> CreateTypeAsync(
        CreateLeaveTypeDto data, string tenantId, string orgId, string? actorUserId = null, CancellationToken ct = default)
    {
        if (LeaveTypePolicy.ValidateCreate(data) is { } validationErrors)
            return Respons<LeaveTypeListItemDto>.ValidationError(validationErrors);

        var name = data.Name!.Trim();
        if (await _leave.TypeNameExistsScopedAsync(tenantId, orgId, name, null, ct))
            return Respons<LeaveTypeListItemDto>.ValidationError(
                new Dictionary<string, string> { ["name"] = "A leave type with this name already exists." });

        var id = await _leave.CreateTypeScopedAsync(tenantId, orgId, data, actorUserId, ct);
        return await GetTypeByIdAsync(id, tenantId, orgId, ct);
    }

    public async Task<Respons<LeaveTypeListItemDto>> UpdateTypeAsync(
        Guid id, CreateLeaveTypeDto data, string tenantId, string orgId, string? actorUserId = null, CancellationToken ct = default)
    {
        if (LeaveTypePolicy.ValidateSave(data) is { } validationErrors)
            return Respons<LeaveTypeListItemDto>.ValidationError(validationErrors);

        if (await _leave.TypeNameExistsScopedAsync(tenantId, orgId, data.Name!.Trim(), id, ct))
            return Respons<LeaveTypeListItemDto>.ValidationError(
                new Dictionary<string, string> { ["name"] = "A leave type with this name already exists." });

        var updated = await _leave.UpdateTypeScopedAsync(id, tenantId, orgId, data, actorUserId, ct);
        if (updated is null)
            return Respons<LeaveTypeListItemDto>.Fail("Leave type not found.", statusCode: 404);

        var items = await EnrichTypesAsync([updated], tenantId, ct);
        return Respons<LeaveTypeListItemDto>.Ok(items[0]);
    }

    public async Task<Respons<LeaveTypeListItemDto>> ArchiveTypeAsync(
        Guid id, string tenantId, string orgId, string? actorUserId = null, CancellationToken ct = default)
    {
        var archived = await _leave.ArchiveTypeScopedAsync(id, tenantId, orgId, actorUserId, ct);
        if (archived is null)
            return Respons<LeaveTypeListItemDto>.Fail("Leave type not found.", statusCode: 404);

        var items = await EnrichTypesAsync([archived], tenantId, ct);
        return Respons<LeaveTypeListItemDto>.Ok(items[0], detail: "Leave type archived.");
    }

    public async Task<Respons<object>> DeleteTypeAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default)
    {
        var exists = await _leave.GetTypeByIdScopedAsync(id, tenantId, orgId, ct);
        if (exists is null)
            return Respons<object>.Fail("Leave type not found.", statusCode: 404);

        if (await _leave.TypeInUseScopedAsync(id, tenantId, orgId, ct))
            return Respons<object>.Fail(
                "Leave type is referenced by leave requests or balances. Archive it instead.",
                statusCode: 409);

        if (!await _leave.DeleteTypeScopedAsync(id, tenantId, orgId, ct))
            return Respons<object>.Fail("Leave type could not be deleted.", statusCode: 404);

        return Respons<object>.Ok(new { leaveTypeId = id.ToString() }, "Leave type deleted.");
    }

    public async Task<Respons<PublicHolidayListDto>> ListHolidaysAsync(
        PublicHolidayListQuery query,
        string tenantId, string orgId, CancellationToken ct = default)
    {
        string? countryCode = null;
        if (!string.IsNullOrWhiteSpace(query.Country))
        {
            if (!CountryCatalog.TryGetByName(query.Country, out var country))
            {
                return Respons<PublicHolidayListDto>.ValidationError(
                    new Dictionary<string, string> { ["country"] = "Country not found." });
            }

            countryCode = country.Code;
        }

        if (query.Year is < 1 or > 9999)
        {
            return Respons<PublicHolidayListDto>.ValidationError(
                new Dictionary<string, string> { ["year"] = "Year must be a calendar year (e.g. 2026)." });
        }

        int? listYear = query.Year;

        var paging = PagedQuery.From(query.Page, query.Size);
        var (items, total) = await _leave.ListHolidaysScopedAsync(
            tenantId, orgId, query.Search, countryCode, listYear, paging.Page, paging.Size, ct);

        return Respons<PublicHolidayListDto>.Ok(
            new PublicHolidayListDto { Items = await EnrichHolidaysAsync(items, tenantId, listYear, ct) },
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
        if (row is null)
            return Respons<PublicHolidayListItemDto>.Fail("Public holiday not found.", statusCode: 404);

        var items = await EnrichHolidaysAsync([row], tenantId, listYear: null, ct);
        return Respons<PublicHolidayListItemDto>.Ok(items[0]);
    }

    public async Task<Respons<PublicHolidayListItemDto>> CreateHolidayAsync(
        CreatePublicHolidayDto data, string tenantId, string orgId, string? actorUserId = null, CancellationToken ct = default)
    {
        if (!CountryCatalog.TryGetByName(data.Country, out var country))
        {
            return Respons<PublicHolidayListItemDto>.ValidationError(
                new Dictionary<string, string> { ["country"] = "Country not found." });
        }

        var id = await _leave.CreateHolidayScopedAsync(tenantId, orgId, country.Code, data, actorUserId, ct);
        return await GetHolidayByIdAsync(id, tenantId, orgId, ct);
    }

    public async Task<Respons<PublicHolidayListItemDto>> UpdateHolidayAsync(
        Guid id, CreatePublicHolidayDto data, string tenantId, string orgId, string? actorUserId = null, CancellationToken ct = default)
    {
        if (!CountryCatalog.TryGetByName(data.Country, out var country))
        {
            return Respons<PublicHolidayListItemDto>.ValidationError(
                new Dictionary<string, string> { ["country"] = "Country not found." });
        }

        var updated = await _leave.UpdateHolidayScopedAsync(id, tenantId, orgId, country.Code, data, actorUserId, ct);
        if (updated is null)
            return Respons<PublicHolidayListItemDto>.Fail("Public holiday not found.", statusCode: 404);

        var items = await EnrichHolidaysAsync([updated], tenantId, listYear: null, ct);
        return Respons<PublicHolidayListItemDto>.Ok(items[0]);
    }

    public async Task<Respons<object>> DeleteHolidayAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default)
    {
        if (!await _leave.DeleteHolidayScopedAsync(id, tenantId, orgId, ct))
            return Respons<object>.Fail("Public holiday not found.", statusCode: 404);

        return Respons<object>.Ok(new { holidayId = id.ToString() }, "Public holiday removed.");
    }

    private async Task<IReadOnlyList<LeaveRequestListItemDto>> EnrichRequestsAsync(
        IReadOnlyList<LeaveRequestRawRow> rows,
        string tenantId,
        string orgId,
        CancellationToken ct)
    {
        if (rows.Count == 0)
            return [];

        var employeeIds = rows
            .Select(r => Guid.TryParse(r.EmployeeId, out var id) ? id : (Guid?)null)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct();
        var employees = await _employees.ResolveLeaveContextsAsync(employeeIds, tenantId, orgId, ct);

        var leaveTypes = LeaveMapper.MergeLeaveTypeLookups(
            LeaveMapper.IndexTypes(await _leave.ListAllTypesScopedAsync(tenantId, orgId, false, ct)),
            rows.Select(r => (r.LeaveTypeId, r.LeaveTypeName)));

        var approverNames = await LeaveMapper.ResolveApproverNamesAsync(
            _cpUsers, LeaveMapper.CollectUserIds(rows), tenantId, ct);

        var profileUrlsByEmployeeId = await ResolveEmployeeProfileUrlsAsync(employees, ct);

        return rows
            .Select(row => LeaveMapper.MapRequest(row, employees, leaveTypes, approverNames, profileUrlsByEmployeeId))
            .ToList();
    }

    private async Task<IReadOnlyList<LeaveApprovalListItemDto>> EnrichApprovalListAsync(
        IReadOnlyList<LeaveRequestRawRow> rows,
        string tenantId,
        string orgId,
        CancellationToken ct)
    {
        if (rows.Count == 0)
            return [];

        var employeeIds = rows
            .Select(r => Guid.TryParse(r.EmployeeId, out var id) ? id : (Guid?)null)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct();
        var employees = await _employees.ResolveLeaveContextsAsync(employeeIds, tenantId, orgId, ct);

        var leaveTypes = LeaveMapper.MergeLeaveTypeLookups(
            LeaveMapper.IndexTypes(await _leave.ListAllTypesScopedAsync(tenantId, orgId, false, ct)),
            rows.Select(r => (r.LeaveTypeId, r.LeaveTypeName)));

        var approverUsers = await _cpUsers.GetByIdsAsync(LeaveMapper.CollectUserIds(rows), tenantId, ct);
        var approverNames = approverUsers
            .Where(kvp => !string.IsNullOrWhiteSpace(kvp.Value.FullName))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.FullName);

        var profileUrlsByEmployeeId = await ResolveEmployeeProfileUrlsAsync(employees, ct);
        var approverProfileUrlsByUserId = await ResolveApproverProfileUrlsAsync(approverUsers, ct);

        return rows
            .Select(row => LeaveMapper.MapApprovalListItem(
                row,
                employees,
                leaveTypes,
                approverNames,
                approverUsers,
                approverProfileUrlsByUserId,
                profileUrlsByEmployeeId))
            .ToList();
    }

    private async Task<IReadOnlyDictionary<string, DocumentReadDto?>> ResolveApproverProfileUrlsAsync(
        IReadOnlyDictionary<string, CpUserDto> approverUsers,
        CancellationToken ct)
    {
        if (approverUsers.Count == 0)
            return new Dictionary<string, DocumentReadDto?>();

        var storedRefs = approverUsers.Values
            .Select(u => u.ProfilePic)
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(r => r!.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (storedRefs.Count == 0)
            return approverUsers.Keys.ToDictionary(id => id, _ => (DocumentReadDto?)null);

        var profileUrlMap = await _profileUrls.ResolveDocumentReadsAsync(storedRefs, ct);
        return approverUsers.ToDictionary(
            kvp => kvp.Key,
            kvp =>
            {
                var stored = kvp.Value.ProfilePic?.Trim();
                return stored is null ? null : profileUrlMap.GetValueOrDefault(stored);
            });
    }

    private async Task<IReadOnlyDictionary<Guid, DocumentReadDto?>> ResolveEmployeeProfileUrlsAsync(
        IReadOnlyDictionary<Guid, EmployeeLeaveContext> employees,
        CancellationToken ct)
    {
        if (employees.Count == 0)
            return new Dictionary<Guid, DocumentReadDto?>();

        var storedRefs = employees.Values
            .Select(e => e.StoredProfileReference)
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(r => r!.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (storedRefs.Count == 0)
            return employees.Keys.ToDictionary(id => id, _ => (DocumentReadDto?)null);

        var profileUrlMap = await _profileUrls.ResolveDocumentReadsAsync(storedRefs, ct);
        return employees.ToDictionary(
            kvp => kvp.Key,
            kvp =>
            {
                var stored = kvp.Value.StoredProfileReference?.Trim();
                return stored is null ? null : profileUrlMap.GetValueOrDefault(stored);
            });
    }

    private async Task<IReadOnlyList<LeaveBalanceListItemDto>> EnrichBalancesAsync(
        IReadOnlyList<LeaveBalanceRawRow> rows,
        string tenantId,
        string orgId,
        CancellationToken ct)
    {
        if (rows.Count == 0)
            return [];

        var employeeIds = rows
            .Select(r => Guid.TryParse(r.EmployeeId, out var id) ? id : (Guid?)null)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct();
        var employees = await _employees.ResolveLeaveContextsAsync(employeeIds, tenantId, orgId, ct);

        var leaveTypes = LeaveMapper.MergeLeaveTypeLookups(
            LeaveMapper.IndexTypes(await _leave.ListAllTypesScopedAsync(tenantId, orgId, false, ct)),
            rows.Select(r => (r.LeaveTypeId, r.LeaveTypeName)));

        var profileUrlsByEmployeeId = await ResolveEmployeeProfileUrlsAsync(employees, ct);

        var userNames = await LeaveMapper.ResolveApproverNamesAsync(
            _cpUsers, LeaveMapper.CollectUserIds(rows), tenantId, ct);

        return rows
            .Select(row => LeaveMapper.MapBalance(row, employees, leaveTypes, userNames, profileUrlsByEmployeeId))
            .ToList();
    }

    private async Task<IReadOnlyList<LeaveTypeListItemDto>> EnrichTypesAsync(
        IReadOnlyList<LeaveTypeListItemDto> items,
        string tenantId,
        CancellationToken ct)
    {
        if (items.Count == 0)
            return items;

        var userNames = await LeaveMapper.ResolveApproverNamesAsync(
            _cpUsers,
            LeaveAuditFields.CollectUserIds(items.Select(i => i.CreatedById), items.Select(i => i.UpdatedById)),
            tenantId,
            ct);

        return items
            .Select(item => LeaveMapper.EnrichTypeAudit(item, userNames))
            .ToList();
    }

    private async Task<IReadOnlyList<PublicHolidayListItemDto>> EnrichHolidaysAsync(
        IReadOnlyList<PublicHolidayListItemDto> items,
        string tenantId,
        int? listYear,
        CancellationToken ct)
    {
        if (items.Count == 0)
            return items;

        var userNames = await LeaveMapper.ResolveApproverNamesAsync(
            _cpUsers,
            LeaveAuditFields.CollectUserIds(items.Select(i => i.CreatedById), items.Select(i => i.UpdatedById)),
            tenantId,
            ct);

        return items
            .Select(item =>
            {
                var enriched = LeaveMapper.EnrichHolidayAudit(item, userNames);
                if (!listYear.HasValue)
                    return enriched;

                return LeaveMapper.WithOccurrenceDate(enriched, listYear.Value);
            })
            .ToList();
    }

    private async Task<(Guid EmployeeId, string OrgId, EmployeeDisplayInfo Display)?> ResolveEmployeeAsync(
        Guid employeeId, string tenantId, string orgId, CancellationToken ct) =>
        await _employees.ResolveEmployeeAsync(employeeId, tenantId, orgId, ct);

    private static string ResolveInitialApprovalStage(EmployeeLeaveContext employee)
    {
        if (employee.LineManagerEmployeeId.HasValue)
            return LeaveApprovalStages.PendingLineManager;
        if (employee.HeadOfDepartmentEmployeeId.HasValue)
            return LeaveApprovalStages.PendingHeadOfDepartment;
        return LeaveApprovalStages.PendingFinal;
    }

    private static Respons<LeaveRequestDetailDto>? ValidateRequestDates(
        DateOnly startDate, DateOnly endDate, decimal daysRequested)
    {
        if (endDate < startDate)
            return Respons<LeaveRequestDetailDto>.ValidationError(
                new Dictionary<string, string> { ["endDate"] = "End date must be on or after start date." });
        if (daysRequested <= 0)
            return Respons<LeaveRequestDetailDto>.ValidationError(
                new Dictionary<string, string> { ["daysRequested"] = "Days requested must be greater than zero." });
        return null;
    }

    private async Task<Respons<LeaveRequestDetailDto>?> ValidateSufficientBalanceAsync(
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
            return Respons<LeaveRequestDetailDto>.ValidationError(
                new Dictionary<string, string>
                {
                    ["daysRequested"] = $"Insufficient leave balance. Remaining: {balance.RemainingDays} day(s).",
                });

        return null;
    }
}
