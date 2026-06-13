using Microsoft.AspNetCore.Mvc;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Authorization;
using ZelosHR.Api.Shared.Constants;
using ZelosHR.Api.Shared.Tenant;
using ZelosHR.Api.Shared.Validation;

namespace ZelosHR.Api.Entities.Leave;

/// <summary>Leave management — admin requests, My Leave, balances, types, and public holidays.</summary>
[ApiController]
[ApiExplorerSettings(GroupName = SwaggerGroups.Leave)]
[Route("api/v1/leave")]
[Produces("application/json")]
public class LeaveController : ControllerBase
{
    private readonly LeaveService _service;
    private readonly ITenantContextAccessor _tenant;

    public LeaveController(LeaveService service, ITenantContextAccessor tenant)
    {
        _service = service;
        _tenant = tenant;
    }

    /// <summary>Admin dashboard counts (pending, on leave today, etc.).</summary>
    [HttpGet("statistics")]
    [RequiresZelosHrPermission(ZelosHrPermissions.LeaveGet)]
    [ProducesResponseType(typeof(Respons<LeaveSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Respons<LeaveSummaryDto>>> Statistics(CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.GetSummaryAsync(ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Leave Management landing page — KPI cards and dashboard widgets (on leave today, pending approvals, leaving this week).</summary>
    [HttpGet("summary")]
    [RequiresZelosHrPermission(ZelosHrPermissions.LeaveGet)]
    [ProducesResponseType(typeof(Respons<LeaveDashboardDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Respons<LeaveDashboardDto>>> Summary(CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.GetDashboardAsync(ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Leave Management dashboard widgets (on leave today, pending approvals, leaving this week).</summary>
    [HttpGet("dashboard")]
    [RequiresZelosHrPermission(ZelosHrPermissions.LeaveGet)]
    [ProducesResponseType(typeof(Respons<LeaveDashboardDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Respons<LeaveDashboardDto>>> Dashboard(CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.GetDashboardAsync(ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>List leave requests (admin Leave Management / Approvals). Filters: search, employee_id, employee_code, leave_request_id, department_id, branch_id, leave_type_id, status, approval_stage, from_date/to_date, submitted_from_date/submitted_to_date.</summary>
    [HttpGet("requests/list")]
    [RequiresZelosHrPermission(ZelosHrPermissions.LeaveGet)]
    [ProducesResponseType(typeof(Respons<LeaveListDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Respons<LeaveListDto>>> ListRequests(
        [FromQuery] string? search,
        [FromQuery]
        [SwaggerAllowedValues(typeof(LeaveFieldOptions), nameof(LeaveFieldOptions.RequestStatuses))]
        string? status,
        [FromQuery]
        [SwaggerAllowedValues(typeof(LeaveFieldOptions), nameof(LeaveFieldOptions.ApprovalStages))]
        string? approvalStage,
        [FromQuery(Name = PlatformQueryParams.LeaveRequestId)] Guid? leaveRequestId,
        [FromQuery(Name = PlatformQueryParams.LeaveTypeId)] Guid? leaveTypeId,
        [FromQuery(Name = PlatformQueryParams.EmployeeId)] Guid? employeeId,
        [FromQuery(Name = "employee_code")] string? employeeCode,
        [FromQuery(Name = PlatformQueryParams.DepartmentId)] Guid? departmentId,
        [FromQuery(Name = PlatformQueryParams.BranchId)] Guid? branchId,
        [FromQuery(Name = "from_date")] DateOnly? fromDate,
        [FromQuery(Name = "to_date")] DateOnly? toDate,
        [FromQuery(Name = "submitted_from_date")] DateOnly? submittedFromDate,
        [FromQuery(Name = "submitted_to_date")] DateOnly? submittedToDate,
        [FromQuery] int page = 1,
        [FromQuery] int size = 20,
        CancellationToken ct = default)
    {
        var ctx = _tenant.Current;
        var result = await _service.ListAsync(
            new LeaveRequestListQuery
            {
                Search = search,
                Status = status,
                ApprovalStage = approvalStage,
                LeaveRequestId = leaveRequestId,
                LeaveTypeId = leaveTypeId,
                EmployeeId = employeeId,
                EmployeeCode = employeeCode,
                DepartmentId = departmentId,
                BranchId = branchId,
                FromDate = fromDate,
                ToDate = toDate,
                SubmittedFromDate = submittedFromDate,
                SubmittedToDate = submittedToDate,
                Page = page,
                Size = size,
            },
            ctx.TenantId,
            ctx.OrgId,
            ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Get a single leave request by id (detail modal).</summary>
    [HttpGet("requests/get")]
    [RequiresZelosHrPermission(ZelosHrPermissions.LeaveGet)]
    [ProducesResponseType(typeof(Respons<LeaveRequestDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<LeaveRequestDetailDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Respons<LeaveRequestDetailDto>>> GetRequest(
        [FromQuery(Name = PlatformQueryParams.LeaveRequestId)] Guid leaveRequestId,
        CancellationToken ct)
    {
        if (QueryParamValidation.BadRequestIfEmptyGuid<LeaveRequestDetailDto>(
                leaveRequestId, PlatformQueryParams.LeaveRequestId) is { } missingId)
            return missingId;

        var ctx = _tenant.Current;
        var result = await _service.GetRequestByIdAsync(leaveRequestId, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Create a leave request on behalf of an employee (admin).</summary>
    [HttpPost("requests/add")]
    [RequiresZelosHrPermission(ZelosHrPermissions.LeaveCreate)]
    [ProducesResponseType(typeof(Respons<LeaveRequestDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<LeaveRequestDetailDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Respons<LeaveRequestDetailDto>>> CreateRequest(
        [FromBody] CreateLeaveRequestDto body,
        CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.CreateRequestAsync(body, ctx.TenantId, ctx.OrgId, ctx.UserId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Partial update of leave request status or notes.</summary>
    [HttpPut("requests/update")]
    [RequiresZelosHrPermission(ZelosHrPermissions.LeaveUpdate)]
    [ProducesResponseType(typeof(Respons<LeaveRequestDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<LeaveRequestDetailDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Respons<LeaveRequestDetailDto>>> UpdateRequest(
        [FromQuery(Name = PlatformQueryParams.LeaveRequestId)] Guid leaveRequestId,
        [FromBody] UpdateLeaveRequestDto body,
        CancellationToken ct)
    {
        if (QueryParamValidation.BadRequestIfEmptyGuid<LeaveRequestDetailDto>(
                leaveRequestId, PlatformQueryParams.LeaveRequestId) is { } missingId)
            return missingId;

        var ctx = _tenant.Current;
        var result = await _service.UpdateRequestAsync(leaveRequestId, body, ctx.TenantId, ctx.OrgId, ctx.UserId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Advance approval (line manager, head of department, or final). Final stage deducts balance.</summary>
    [HttpPost("requests/approve")]
    [RequiresZelosHrPermission(ZelosHrPermissions.LeaveUpdate)]
    [ProducesResponseType(typeof(Respons<LeaveRequestDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<LeaveRequestDetailDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Respons<LeaveRequestDetailDto>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Respons<LeaveRequestDetailDto>>> ApproveRequest(
        [FromQuery(Name = PlatformQueryParams.LeaveRequestId)] Guid leaveRequestId,
        CancellationToken ct)
    {
        if (QueryParamValidation.BadRequestIfEmptyGuid<LeaveRequestDetailDto>(
                leaveRequestId, PlatformQueryParams.LeaveRequestId) is { } missingId)
            return missingId;

        var ctx = _tenant.Current;
        var result = await _service.ApproveRequestAsync(
            leaveRequestId, ctx.UserId, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Reject a pending leave request. Approver is the authenticated user.</summary>
    [HttpPost("requests/reject")]
    [RequiresZelosHrPermission(ZelosHrPermissions.LeaveUpdate)]
    [ProducesResponseType(typeof(Respons<LeaveRequestDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<LeaveRequestDetailDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Respons<LeaveRequestDetailDto>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Respons<LeaveRequestDetailDto>>> RejectRequest(
        [FromQuery(Name = PlatformQueryParams.LeaveRequestId)] Guid leaveRequestId,
        [FromBody] RejectLeaveRequestDto? body,
        CancellationToken ct)
    {
        if (QueryParamValidation.BadRequestIfEmptyGuid<LeaveRequestDetailDto>(
                leaveRequestId, PlatformQueryParams.LeaveRequestId) is { } missingId)
            return missingId;

        var ctx = _tenant.Current;
        var result = await _service.RejectRequestAsync(
            leaveRequestId, ctx.UserId, body?.Notes, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Delete a pending leave request.</summary>
    [HttpDelete("requests/delete")]
    [RequiresZelosHrPermission(ZelosHrPermissions.LeaveDelete)]
    [ProducesResponseType(typeof(Respons<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Respons<object>>> DeleteRequest(
        [FromQuery(Name = PlatformQueryParams.LeaveRequestId)] Guid leaveRequestId,
        CancellationToken ct)
    {
        if (QueryParamValidation.BadRequestIfEmptyGuid<object>(
                leaveRequestId, PlatformQueryParams.LeaveRequestId) is { } missingId)
            return missingId;

        var ctx = _tenant.Current;
        var result = await _service.DeleteRequestAsync(leaveRequestId, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>My Leave — personal remaining days, counts, and balances for the logged-in employee.</summary>
    [HttpGet("my/summary")]
    [RequiresZelosHrPermission(ZelosHrPermissions.LeaveGet)]
    [ProducesResponseType(typeof(Respons<LeavePersonalSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<LeavePersonalSummaryDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Respons<LeavePersonalSummaryDto>>> MySummary(CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.GetMySummaryAsync(ctx.UserId, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>List leave requests for the logged-in employee (My Leave).</summary>
    [HttpGet("my/requests/list")]
    [RequiresZelosHrPermission(ZelosHrPermissions.LeaveGet)]
    [ProducesResponseType(typeof(Respons<LeaveMyRequestListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<LeaveMyRequestListDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Respons<LeaveMyRequestListDto>>> MyRequests(
        [FromQuery]
        [SwaggerAllowedValues(typeof(LeaveFieldOptions), nameof(LeaveFieldOptions.RequestStatuses))]
        string? status,
        [FromQuery] int page = 1,
        [FromQuery] int size = 20,
        CancellationToken ct = default)
    {
        var ctx = _tenant.Current;
        var result = await _service.ListMyRequestsAsync(ctx.UserId, status, page, size, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>List leave balances for the logged-in employee (nested employee and leave_type on each row).</summary>
    [HttpGet("my/balances/list")]
    [RequiresZelosHrPermission(ZelosHrPermissions.LeaveGet)]
    [ProducesResponseType(typeof(Respons<LeaveBalanceListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<LeaveBalanceListDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Respons<LeaveBalanceListDto>>> MyBalances(CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.ListMyBalancesAsync(ctx.UserId, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Submit a leave request for the logged-in employee (My Leave).</summary>
    [HttpPost("my/requests/add")]
    [RequiresZelosHrPermission(ZelosHrPermissions.LeaveCreate)]
    [ProducesResponseType(typeof(Respons<LeaveRequestDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<LeaveRequestDetailDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Respons<LeaveRequestDetailDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Respons<LeaveRequestDetailDto>>> CreateMyRequest(
        [FromBody] CreateMyLeaveRequestDto body,
        CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.CreateMyRequestAsync(body, ctx.UserId, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>List leave balances (admin), optionally filtered; rows include nested display refs.</summary>
    [HttpGet("balances/list")]
    [RequiresZelosHrPermission(ZelosHrPermissions.LeaveGet)]
    [ProducesResponseType(typeof(Respons<LeaveBalanceListDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Respons<LeaveBalanceListDto>>> ListBalances(
        [FromQuery(Name = PlatformQueryParams.EmployeeId)] Guid? employeeId,
        [FromQuery(Name = PlatformQueryParams.LeaveTypeId)] Guid? leaveTypeId,
        CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.ListBalancesAsync(employeeId, leaveTypeId, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Get a single leave balance row with nested employee and leave_type refs.</summary>
    [HttpGet("balances/get")]
    [RequiresZelosHrPermission(ZelosHrPermissions.LeaveGet)]
    [ProducesResponseType(typeof(Respons<LeaveBalanceListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<LeaveBalanceListItemDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Respons<LeaveBalanceListItemDto>>> GetBalance(
        [FromQuery(Name = PlatformQueryParams.LeaveBalanceId)] Guid leaveBalanceId,
        CancellationToken ct)
    {
        if (QueryParamValidation.BadRequestIfEmptyGuid<LeaveBalanceListItemDto>(
                leaveBalanceId, PlatformQueryParams.LeaveBalanceId) is { } missingId)
            return missingId;

        var ctx = _tenant.Current;
        var result = await _service.GetBalanceByIdAsync(leaveBalanceId, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Assign leave entitlement to an employee (admin).</summary>
    [HttpPost("balances/add")]
    [RequiresZelosHrPermission(ZelosHrPermissions.LeaveAdmin)]
    [ProducesResponseType(typeof(Respons<LeaveBalanceListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<LeaveBalanceListItemDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Respons<LeaveBalanceListItemDto>>> CreateBalance(
        [FromBody] CreateLeaveBalanceDto body,
        CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.CreateBalanceAsync(body, ctx.TenantId, ctx.OrgId, ctx.UserId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Adjust entitled or used days on a balance row (admin).</summary>
    [HttpPut("balances/update")]
    [RequiresZelosHrPermission(ZelosHrPermissions.LeaveAdmin)]
    [ProducesResponseType(typeof(Respons<LeaveBalanceListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<LeaveBalanceListItemDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Respons<LeaveBalanceListItemDto>>> UpdateBalance(
        [FromQuery(Name = PlatformQueryParams.LeaveBalanceId)] Guid leaveBalanceId,
        [FromBody] UpdateLeaveBalanceDto body,
        CancellationToken ct)
    {
        if (QueryParamValidation.BadRequestIfEmptyGuid<LeaveBalanceListItemDto>(
                leaveBalanceId, PlatformQueryParams.LeaveBalanceId) is { } missingId)
            return missingId;

        var ctx = _tenant.Current;
        var result = await _service.UpdateBalanceAsync(leaveBalanceId, body, ctx.TenantId, ctx.OrgId, ctx.UserId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>List configured leave types (Settings).</summary>
    [HttpGet("types/list")]
    [RequiresZelosHrPermission(ZelosHrPermissions.LeaveGet)]
    [ProducesResponseType(typeof(Respons<LeaveTypeListDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Respons<LeaveTypeListDto>>> ListTypes(
        [FromQuery] string? countryCode,
        [FromQuery] bool activeOnly = true,
        CancellationToken ct = default)
    {
        var ctx = _tenant.Current;
        var result = await _service.ListTypesAsync(countryCode, activeOnly, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Get a leave type by id.</summary>
    [HttpGet("types/get")]
    [RequiresZelosHrPermission(ZelosHrPermissions.LeaveGet)]
    [ProducesResponseType(typeof(Respons<LeaveTypeListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<LeaveTypeListItemDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Respons<LeaveTypeListItemDto>>> GetType(
        [FromQuery(Name = PlatformQueryParams.LeaveTypeId)] Guid leaveTypeId,
        CancellationToken ct)
    {
        if (QueryParamValidation.BadRequestIfEmptyGuid<LeaveTypeListItemDto>(
                leaveTypeId, PlatformQueryParams.LeaveTypeId) is { } missingId)
            return missingId;

        var ctx = _tenant.Current;
        var result = await _service.GetTypeByIdAsync(leaveTypeId, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Create a leave type (admin).</summary>
    [HttpPost("types/add")]
    [RequiresZelosHrPermission(ZelosHrPermissions.LeaveAdmin)]
    [ProducesResponseType(typeof(Respons<LeaveTypeListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<LeaveTypeListItemDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Respons<LeaveTypeListItemDto>>> CreateType(
        [FromBody] CreateLeaveTypeDto body,
        CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.CreateTypeAsync(body, ctx.TenantId, ctx.OrgId, ctx.UserId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Update a leave type (admin).</summary>
    [HttpPut("types/update")]
    [RequiresZelosHrPermission(ZelosHrPermissions.LeaveAdmin)]
    [ProducesResponseType(typeof(Respons<LeaveTypeListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<LeaveTypeListItemDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Respons<LeaveTypeListItemDto>>> UpdateType(
        [FromQuery(Name = PlatformQueryParams.LeaveTypeId)] Guid leaveTypeId,
        [FromBody] UpdateLeaveTypeDto body,
        CancellationToken ct)
    {
        if (QueryParamValidation.BadRequestIfEmptyGuid<LeaveTypeListItemDto>(
                leaveTypeId, PlatformQueryParams.LeaveTypeId) is { } missingId)
            return missingId;

        var ctx = _tenant.Current;
        var result = await _service.UpdateTypeAsync(leaveTypeId, body, ctx.TenantId, ctx.OrgId, ctx.UserId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Delete or deactivate a leave type (admin).</summary>
    [HttpDelete("types/delete")]
    [RequiresZelosHrPermission(ZelosHrPermissions.LeaveAdmin)]
    [ProducesResponseType(typeof(Respons<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Respons<object>>> DeleteType(
        [FromQuery(Name = PlatformQueryParams.LeaveTypeId)] Guid leaveTypeId,
        CancellationToken ct)
    {
        if (QueryParamValidation.BadRequestIfEmptyGuid<object>(
                leaveTypeId, PlatformQueryParams.LeaveTypeId) is { } missingId)
            return missingId;

        var ctx = _tenant.Current;
        var result = await _service.DeleteTypeAsync(leaveTypeId, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>List public holidays by country, year, and optional branch.</summary>
    [HttpGet("holidays/list")]
    [RequiresZelosHrPermission(ZelosHrPermissions.LeaveGet)]
    [ProducesResponseType(typeof(Respons<PublicHolidayListDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Respons<PublicHolidayListDto>>> ListHolidays(
        [FromQuery] string? countryCode,
        [FromQuery] int? year,
        [FromQuery(Name = PlatformQueryParams.BranchId)] Guid? branchId,
        [FromQuery] int page = 1,
        [FromQuery] int size = 50,
        CancellationToken ct = default)
    {
        var ctx = _tenant.Current;
        var result = await _service.ListHolidaysAsync(
            countryCode, year, branchId, page, size, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Get a public holiday by id.</summary>
    [HttpGet("holidays/get")]
    [RequiresZelosHrPermission(ZelosHrPermissions.LeaveGet)]
    [ProducesResponseType(typeof(Respons<PublicHolidayListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<PublicHolidayListItemDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Respons<PublicHolidayListItemDto>>> GetHoliday(
        [FromQuery(Name = PlatformQueryParams.HolidayId)] Guid holidayId,
        CancellationToken ct)
    {
        if (QueryParamValidation.BadRequestIfEmptyGuid<PublicHolidayListItemDto>(
                holidayId, PlatformQueryParams.HolidayId) is { } missingId)
            return missingId;

        var ctx = _tenant.Current;
        var result = await _service.GetHolidayByIdAsync(holidayId, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Create a public holiday (admin).</summary>
    [HttpPost("holidays/add")]
    [RequiresZelosHrPermission(ZelosHrPermissions.LeaveAdmin)]
    [ProducesResponseType(typeof(Respons<PublicHolidayListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Respons<PublicHolidayListItemDto>>> CreateHoliday(
        [FromBody] CreatePublicHolidayDto body,
        CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.CreateHolidayAsync(body, ctx.TenantId, ctx.OrgId, ctx.UserId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Update a public holiday (admin).</summary>
    [HttpPut("holidays/update")]
    [RequiresZelosHrPermission(ZelosHrPermissions.LeaveAdmin)]
    [ProducesResponseType(typeof(Respons<PublicHolidayListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<PublicHolidayListItemDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Respons<PublicHolidayListItemDto>>> UpdateHoliday(
        [FromQuery(Name = PlatformQueryParams.HolidayId)] Guid holidayId,
        [FromBody] UpdatePublicHolidayDto body,
        CancellationToken ct)
    {
        if (QueryParamValidation.BadRequestIfEmptyGuid<PublicHolidayListItemDto>(
                holidayId, PlatformQueryParams.HolidayId) is { } missingId)
            return missingId;

        var ctx = _tenant.Current;
        var result = await _service.UpdateHolidayAsync(holidayId, body, ctx.TenantId, ctx.OrgId, ctx.UserId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Remove a public holiday (admin).</summary>
    [HttpDelete("holidays/delete")]
    [RequiresZelosHrPermission(ZelosHrPermissions.LeaveAdmin)]
    [ProducesResponseType(typeof(Respons<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Respons<object>>> DeleteHoliday(
        [FromQuery(Name = PlatformQueryParams.HolidayId)] Guid holidayId,
        CancellationToken ct)
    {
        if (QueryParamValidation.BadRequestIfEmptyGuid<object>(
                holidayId, PlatformQueryParams.HolidayId) is { } missingId)
            return missingId;

        var ctx = _tenant.Current;
        var result = await _service.DeleteHolidayAsync(holidayId, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }
}
