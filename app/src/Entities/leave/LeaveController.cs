using Microsoft.AspNetCore.Mvc;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Authorization;
using ZelosHR.Api.Shared.Constants;
using ZelosHR.Api.Shared.Tenant;

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
    public async Task<ActionResult<Respons<LeaveSummaryDto>>> Statistics(CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.GetSummaryAsync(ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>List leave requests and balances (admin Leave Management tab).</summary>
    [HttpGet("requests/list")]
    [RequiresZelosHrPermission(ZelosHrPermissions.LeaveGet)]
    public async Task<ActionResult<Respons<LeaveListDto>>> ListRequests(
        [FromQuery] string? search,
        [FromQuery]
        [SwaggerAllowedValues(typeof(LeaveFieldOptions), nameof(LeaveFieldOptions.RequestStatuses))]
        string? status,
        [FromQuery] string? leaveType,
        [FromQuery(Name = PlatformQueryParams.EmployeeId)] Guid? employeeId,
        [FromQuery] int page = 1,
        [FromQuery] int size = 20,
        CancellationToken ct = default)
    {
        var ctx = _tenant.Current;
        var result = await _service.ListAsync(
            search, status, leaveType, employeeId, page, size, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("requests/get")]
    [RequiresZelosHrPermission(ZelosHrPermissions.LeaveGet)]
    public async Task<ActionResult<Respons<LeaveRequestListItemDto>>> GetRequest(
        [FromQuery(Name = PlatformQueryParams.LeaveRequestId)] Guid leaveRequestId,
        CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.GetRequestByIdAsync(leaveRequestId, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("requests/add")]
    [RequiresZelosHrPermission(ZelosHrPermissions.LeaveCreate)]
    public async Task<ActionResult<Respons<LeaveRequestListItemDto>>> CreateRequest(
        [FromBody] CreateLeaveRequestDto body,
        CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.CreateRequestAsync(body, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("requests/update")]
    [RequiresZelosHrPermission(ZelosHrPermissions.LeaveUpdate)]
    public async Task<ActionResult<Respons<LeaveRequestListItemDto>>> UpdateRequest(
        [FromQuery(Name = PlatformQueryParams.LeaveRequestId)] Guid leaveRequestId,
        [FromBody] UpdateLeaveRequestDto body,
        CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.UpdateRequestAsync(leaveRequestId, body, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("requests/approve")]
    [RequiresZelosHrPermission(ZelosHrPermissions.LeaveUpdate)]
    public async Task<ActionResult<Respons<LeaveRequestListItemDto>>> ApproveRequest(
        [FromQuery(Name = PlatformQueryParams.LeaveRequestId)] Guid leaveRequestId,
        [FromBody] ApproveLeaveRequestDto body,
        CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.ApproveRequestAsync(
            leaveRequestId, body.ApproverName!, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("requests/reject")]
    [RequiresZelosHrPermission(ZelosHrPermissions.LeaveUpdate)]
    public async Task<ActionResult<Respons<LeaveRequestListItemDto>>> RejectRequest(
        [FromQuery(Name = PlatformQueryParams.LeaveRequestId)] Guid leaveRequestId,
        [FromBody] RejectLeaveRequestDto body,
        CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.RejectRequestAsync(
            leaveRequestId, body.ApproverName!, body.Notes, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpDelete("requests/delete")]
    [RequiresZelosHrPermission(ZelosHrPermissions.LeaveDelete)]
    public async Task<ActionResult<Respons<object>>> DeleteRequest(
        [FromQuery(Name = PlatformQueryParams.LeaveRequestId)] Guid leaveRequestId,
        CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.DeleteRequestAsync(leaveRequestId, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>My Leave summary — remaining days and personal counts.</summary>
    [HttpGet("my/summary")]
    [RequiresZelosHrPermission(ZelosHrPermissions.LeaveGet)]
    public async Task<ActionResult<Respons<LeaveMySummaryDto>>> MySummary(CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.GetMySummaryAsync(ctx.UserId, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("my/requests/list")]
    [RequiresZelosHrPermission(ZelosHrPermissions.LeaveGet)]
    public async Task<ActionResult<Respons<LeaveListDto>>> MyRequests(
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

    [HttpGet("my/balances/list")]
    [RequiresZelosHrPermission(ZelosHrPermissions.LeaveGet)]
    public async Task<ActionResult<Respons<LeaveBalanceListDto>>> MyBalances(CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.ListMyBalancesAsync(ctx.UserId, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("my/requests/add")]
    [RequiresZelosHrPermission(ZelosHrPermissions.LeaveCreate)]
    public async Task<ActionResult<Respons<LeaveRequestListItemDto>>> CreateMyRequest(
        [FromBody] CreateLeaveRequestDto body,
        CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.CreateMyRequestAsync(body, ctx.UserId, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("balances/list")]
    [RequiresZelosHrPermission(ZelosHrPermissions.LeaveGet)]
    public async Task<ActionResult<Respons<LeaveBalanceListDto>>> ListBalances(
        [FromQuery(Name = PlatformQueryParams.EmployeeId)] Guid? employeeId,
        [FromQuery] string? leaveType,
        CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.ListBalancesAsync(employeeId, leaveType, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("balances/get")]
    [RequiresZelosHrPermission(ZelosHrPermissions.LeaveGet)]
    public async Task<ActionResult<Respons<LeaveBalanceListItemDto>>> GetBalance(
        [FromQuery(Name = PlatformQueryParams.LeaveBalanceId)] Guid leaveBalanceId,
        CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.GetBalanceByIdAsync(leaveBalanceId, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("balances/add")]
    [RequiresZelosHrPermission(ZelosHrPermissions.LeaveAdmin)]
    public async Task<ActionResult<Respons<LeaveBalanceListItemDto>>> CreateBalance(
        [FromBody] CreateLeaveBalanceDto body,
        CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.CreateBalanceAsync(body, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("balances/update")]
    [RequiresZelosHrPermission(ZelosHrPermissions.LeaveAdmin)]
    public async Task<ActionResult<Respons<LeaveBalanceListItemDto>>> UpdateBalance(
        [FromQuery(Name = PlatformQueryParams.LeaveBalanceId)] Guid leaveBalanceId,
        [FromBody] UpdateLeaveBalanceDto body,
        CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.UpdateBalanceAsync(leaveBalanceId, body, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("types/list")]
    [RequiresZelosHrPermission(ZelosHrPermissions.LeaveGet)]
    public async Task<ActionResult<Respons<LeaveTypeListDto>>> ListTypes(
        [FromQuery] string? countryCode,
        [FromQuery] bool activeOnly = true,
        CancellationToken ct = default)
    {
        var ctx = _tenant.Current;
        var result = await _service.ListTypesAsync(countryCode, activeOnly, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("types/get")]
    [RequiresZelosHrPermission(ZelosHrPermissions.LeaveGet)]
    public async Task<ActionResult<Respons<LeaveTypeListItemDto>>> GetType(
        [FromQuery(Name = PlatformQueryParams.LeaveTypeId)] Guid leaveTypeId,
        CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.GetTypeByIdAsync(leaveTypeId, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("types/add")]
    [RequiresZelosHrPermission(ZelosHrPermissions.LeaveAdmin)]
    public async Task<ActionResult<Respons<LeaveTypeListItemDto>>> CreateType(
        [FromBody] CreateLeaveTypeDto body,
        CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.CreateTypeAsync(body, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("types/update")]
    [RequiresZelosHrPermission(ZelosHrPermissions.LeaveAdmin)]
    public async Task<ActionResult<Respons<LeaveTypeListItemDto>>> UpdateType(
        [FromQuery(Name = PlatformQueryParams.LeaveTypeId)] Guid leaveTypeId,
        [FromBody] UpdateLeaveTypeDto body,
        CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.UpdateTypeAsync(leaveTypeId, body, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpDelete("types/delete")]
    [RequiresZelosHrPermission(ZelosHrPermissions.LeaveAdmin)]
    public async Task<ActionResult<Respons<object>>> DeleteType(
        [FromQuery(Name = PlatformQueryParams.LeaveTypeId)] Guid leaveTypeId,
        CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.DeleteTypeAsync(leaveTypeId, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("holidays/list")]
    [RequiresZelosHrPermission(ZelosHrPermissions.LeaveGet)]
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

    [HttpGet("holidays/get")]
    [RequiresZelosHrPermission(ZelosHrPermissions.LeaveGet)]
    public async Task<ActionResult<Respons<PublicHolidayListItemDto>>> GetHoliday(
        [FromQuery(Name = PlatformQueryParams.HolidayId)] Guid holidayId,
        CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.GetHolidayByIdAsync(holidayId, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("holidays/add")]
    [RequiresZelosHrPermission(ZelosHrPermissions.LeaveAdmin)]
    public async Task<ActionResult<Respons<PublicHolidayListItemDto>>> CreateHoliday(
        [FromBody] CreatePublicHolidayDto body,
        CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.CreateHolidayAsync(body, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("holidays/update")]
    [RequiresZelosHrPermission(ZelosHrPermissions.LeaveAdmin)]
    public async Task<ActionResult<Respons<PublicHolidayListItemDto>>> UpdateHoliday(
        [FromQuery(Name = PlatformQueryParams.HolidayId)] Guid holidayId,
        [FromBody] UpdatePublicHolidayDto body,
        CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.UpdateHolidayAsync(holidayId, body, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpDelete("holidays/delete")]
    [RequiresZelosHrPermission(ZelosHrPermissions.LeaveAdmin)]
    public async Task<ActionResult<Respons<object>>> DeleteHoliday(
        [FromQuery(Name = PlatformQueryParams.HolidayId)] Guid holidayId,
        CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.DeleteHolidayAsync(holidayId, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }
}
