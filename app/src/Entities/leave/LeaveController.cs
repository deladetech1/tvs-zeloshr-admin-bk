using Microsoft.AspNetCore.Mvc;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Authorization;
using ZelosHR.Api.Shared.Constants;
using ZelosHR.Api.Shared.Tenant;
using ZelosHR.Api.Shared.Validation;

namespace ZelosHR.Api.Entities.Leave;

/// <summary>Leave management — requests, balances, types, and public holidays.</summary>
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

    /// <summary>Leave Calendar — employee rows with approved and pending leave bars for the visible date window.</summary>
    [HttpGet("calendar")]
    [RequiresZelosHrPermission(ZelosHrPermissions.LeaveGet)]
    [ProducesResponseType(typeof(Respons<LeaveCalendarDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Respons<LeaveCalendarDto>>> Calendar(
        [FromQuery] LeaveCalendarQuery query,
        CancellationToken ct = default)
    {
        var ctx = _tenant.Current;
        var result = await _service.GetCalendarAsync(query, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Leave Approvals table — flat employee rows with leave type, dates, waiting, approved_by.</summary>
    [HttpGet("approvals/list")]
    [RequiresZelosHrPermission(ZelosHrPermissions.LeaveGet)]
    [ProducesResponseType(typeof(Respons<LeaveApprovalListDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Respons<LeaveApprovalListDto>>> ListApprovals(
        [FromQuery] LeaveApprovalListQuery query,
        CancellationToken ct = default)
    {
        var ctx = _tenant.Current;
        var result = await _service.ListApprovalsAsync(query, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>List leave requests — admin Leave Management table.</summary>
    [HttpGet("requests/list")]
    [RequiresZelosHrPermission(ZelosHrPermissions.LeaveGet)]
    [ProducesResponseType(typeof(Respons<LeaveListDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Respons<LeaveListDto>>> ListRequests(
        [FromQuery] LeaveRequestListQuery query,
        CancellationToken ct = default)
    {
        var ctx = _tenant.Current;
        var result = await _service.ListAsync(query, ctx.TenantId, ctx.OrgId, ct);
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

    /// <summary>Personal leave summary for an employee — remaining days, counts, and balances.</summary>
    [HttpGet("my/summary")]
    [RequiresZelosHrPermission(ZelosHrPermissions.LeaveGet)]
    [ProducesResponseType(typeof(Respons<LeavePersonalSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<LeavePersonalSummaryDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Respons<LeavePersonalSummaryDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Respons<LeavePersonalSummaryDto>>> MySummary(
        [FromQuery(Name = PlatformQueryParams.EmployeeId)] Guid employeeId,
        CancellationToken ct)
    {
        if (QueryParamValidation.BadRequestIfEmptyGuid<LeavePersonalSummaryDto>(
                employeeId, PlatformQueryParams.EmployeeId) is { } badRequest)
            return badRequest;

        var ctx = _tenant.Current;
        var result = await _service.GetMySummaryAsync(employeeId, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>List leave requests for an employee (My Leave).</summary>
    [HttpGet("my/requests/list")]
    [RequiresZelosHrPermission(ZelosHrPermissions.LeaveGet)]
    [ProducesResponseType(typeof(Respons<LeaveMyRequestListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<LeaveMyRequestListDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Respons<LeaveMyRequestListDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Respons<LeaveMyRequestListDto>>> MyRequests(
        [FromQuery(Name = PlatformQueryParams.EmployeeId)] Guid employeeId,
        [FromQuery(Name = PlatformQueryParams.Status)]
        [SwaggerAllowedValues(typeof(LeaveFieldOptions), nameof(LeaveFieldOptions.RequestStatuses))]
        string? status,
        [FromQuery(Name = PlatformQueryParams.Page)] int page = 1,
        [FromQuery(Name = PlatformQueryParams.Size)] int size = 20,
        CancellationToken ct = default)
    {
        if (QueryParamValidation.BadRequestIfEmptyGuid<LeaveMyRequestListDto>(
                employeeId, PlatformQueryParams.EmployeeId) is { } badRequest)
            return badRequest;

        var ctx = _tenant.Current;
        var result = await _service.ListMyRequestsAsync(employeeId, status, page, size, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>List leave balances for an employee (nested employee and leave_type on each row).</summary>
    [HttpGet("my/balances/list")]
    [RequiresZelosHrPermission(ZelosHrPermissions.LeaveGet)]
    [ProducesResponseType(typeof(Respons<LeaveBalanceListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<LeaveBalanceListDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Respons<LeaveBalanceListDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Respons<LeaveBalanceListDto>>> MyBalances(
        [FromQuery(Name = PlatformQueryParams.EmployeeId)] Guid employeeId,
        CancellationToken ct)
    {
        if (QueryParamValidation.BadRequestIfEmptyGuid<LeaveBalanceListDto>(
                employeeId, PlatformQueryParams.EmployeeId) is { } badRequest)
            return badRequest;

        var ctx = _tenant.Current;
        var result = await _service.ListMyBalancesAsync(employeeId, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Submit a leave request for an employee (My Leave).</summary>
    [HttpPost("my/requests/add")]
    [RequiresZelosHrPermission(ZelosHrPermissions.LeaveCreate)]
    [ProducesResponseType(typeof(Respons<LeaveRequestDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<LeaveRequestDetailDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Respons<LeaveRequestDetailDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Respons<LeaveRequestDetailDto>>> CreateMyRequest(
        [FromQuery(Name = PlatformQueryParams.EmployeeId)] Guid employeeId,
        [FromBody] CreateMyLeaveRequestDto body,
        CancellationToken ct)
    {
        if (QueryParamValidation.BadRequestIfEmptyGuid<LeaveRequestDetailDto>(
                employeeId, PlatformQueryParams.EmployeeId) is { } badRequest)
            return badRequest;

        var ctx = _tenant.Current;
        var result = await _service.CreateMyRequestAsync(body, employeeId, ctx.UserId, ctx.TenantId, ctx.OrgId, ct);
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
    [RequiresZelosHrPermission(ZelosHrPermissions.LeaveCreate)]
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
    [RequiresZelosHrPermission(ZelosHrPermissions.LeaveUpdate)]
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

    /// <summary>List configured leave types (Settings table) — paginated; each row includes audit fields.</summary>
    [HttpGet("types/list")]
    [RequiresZelosHrPermission(ZelosHrPermissions.LeaveGet)]
    [ProducesResponseType(typeof(Respons<LeaveTypeListDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Respons<LeaveTypeListDto>>> ListTypes(
        [FromQuery] LeaveTypeListQuery query,
        CancellationToken ct = default)
    {
        var ctx = _tenant.Current;
        var result = await _service.ListTypesAsync(query, ctx.TenantId, ctx.OrgId, ct);
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
    [RequiresZelosHrPermission(ZelosHrPermissions.LeaveCreate)]
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

    /// <summary>Update a leave type (admin) — same body as POST /types/add; pass leave_type_id query param.</summary>
    [HttpPut("types/update")]
    [RequiresZelosHrPermission(ZelosHrPermissions.LeaveUpdate)]
    [ProducesResponseType(typeof(Respons<LeaveTypeListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<LeaveTypeListItemDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Respons<LeaveTypeListItemDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Respons<LeaveTypeListItemDto>>> UpdateType(
        [FromQuery(Name = PlatformQueryParams.LeaveTypeId)] Guid leaveTypeId,
        [FromBody] CreateLeaveTypeDto body,
        CancellationToken ct)
    {
        if (QueryParamValidation.BadRequestIfEmptyGuid<LeaveTypeListItemDto>(
                leaveTypeId, PlatformQueryParams.LeaveTypeId) is { } missingId)
            return missingId;

        var ctx = _tenant.Current;
        var result = await _service.UpdateTypeAsync(leaveTypeId, body, ctx.TenantId, ctx.OrgId, ctx.UserId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Archive a leave type (sets is_active=false). Use when the type must stay for history.</summary>
    [HttpPost("types/archive")]
    [RequiresZelosHrPermission(ZelosHrPermissions.LeaveDelete)]
    [ProducesResponseType(typeof(Respons<LeaveTypeListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<LeaveTypeListItemDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Respons<LeaveTypeListItemDto>>> ArchiveType(
        [FromQuery(Name = PlatformQueryParams.LeaveTypeId)] Guid leaveTypeId,
        CancellationToken ct)
    {
        if (QueryParamValidation.BadRequestIfEmptyGuid<LeaveTypeListItemDto>(
                leaveTypeId, PlatformQueryParams.LeaveTypeId) is { } missingId)
            return missingId;

        var ctx = _tenant.Current;
        var result = await _service.ArchiveTypeAsync(leaveTypeId, ctx.TenantId, ctx.OrgId, ctx.UserId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Delete a leave type permanently (only when not referenced). Use archive when in use.</summary>
    [HttpDelete("types/delete")]
    [RequiresZelosHrPermission(ZelosHrPermissions.LeaveDelete)]
    [ProducesResponseType(typeof(Respons<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Respons<object>), StatusCodes.Status409Conflict)]
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

    /// <summary>List public holidays.</summary>
    /// <remarks>
    /// Matches frontend <c>PublicHolidayParams</c>: <c>search</c> · <c>year</c> (calendar year number, e.g. <c>2026</c> — sets <c>occurrence_date</c> on recurring rows) · <c>country</c> (name from <c>GET /countries/list</c>, e.g. Ghana) · <c>page</c> · <c>size</c>.
    /// </remarks>
    [HttpGet("holidays/list")]
    [RequiresZelosHrPermission(ZelosHrPermissions.LeaveGet)]
    [ProducesResponseType(typeof(Respons<PublicHolidayListDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Respons<PublicHolidayListDto>>> ListHolidays(
        [FromQuery] PublicHolidayListQuery query,
        CancellationToken ct = default)
    {
        var ctx = _tenant.Current;
        var result = await _service.ListHolidaysAsync(query, ctx.TenantId, ctx.OrgId, ct);
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
    /// <remarks>Body matches frontend <c>AddPublicHolidayRequest</c>: holiday_name · date · is_recurring_annually · country.</remarks>
    [HttpPost("holidays/add")]
    [RequiresZelosHrPermission(ZelosHrPermissions.LeaveCreate)]
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
    /// <remarks>Same body as create; <c>holiday_id</c> on query string.</remarks>
    [HttpPut("holidays/update")]
    [RequiresZelosHrPermission(ZelosHrPermissions.LeaveUpdate)]
    [ProducesResponseType(typeof(Respons<PublicHolidayListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<PublicHolidayListItemDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Respons<PublicHolidayListItemDto>>> UpdateHoliday(
        [FromQuery(Name = PlatformQueryParams.HolidayId)] Guid holidayId,
        [FromBody] CreatePublicHolidayDto body,
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
    [RequiresZelosHrPermission(ZelosHrPermissions.LeaveDelete)]
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
