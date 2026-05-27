using Microsoft.AspNetCore.Mvc;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Constants;
using ZelosHR.Api.Shared.Tenant;

namespace ZelosHR.Api.Entities.Leave;

[ApiController]
[ApiExplorerSettings(GroupName = SwaggerGroups.Leave, IgnoreApi = true)]
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

    [HttpGet("statistics")]
    public async Task<ActionResult<Respons<LeaveSummaryDto>>> Statistics(CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.GetSummaryAsync(ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("requests/list")]
    public async Task<ActionResult<Respons<LeaveListDto>>> List(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] string? leaveType,
        [FromQuery] int page = 1,
        [FromQuery] int size = 20,
        CancellationToken ct = default)
    {
        var ctx = _tenant.Current;
        var result = await _service.ListAsync(
            search, status, leaveType, page, size, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("requests/get")]
    public async Task<ActionResult<Respons<LeaveRequestListItemDto>>> GetRequest(
        [FromQuery(Name = PlatformQueryParams.LeaveRequestId)] Guid leaveRequestId,
        CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.GetRequestByIdAsync(leaveRequestId, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("requests/add")]
    public async Task<ActionResult<Respons<LeaveRequestListItemDto>>> CreateRequest(
        [FromBody] CreateLeaveRequestDto body,
        CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.CreateRequestAsync(body, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("requests/update")]
    public async Task<ActionResult<Respons<LeaveRequestListItemDto>>> UpdateRequest(
        [FromQuery(Name = PlatformQueryParams.LeaveRequestId)] Guid leaveRequestId,
        [FromBody] UpdateLeaveRequestDto body,
        CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.UpdateRequestAsync(leaveRequestId, body, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpDelete("requests/delete")]
    public async Task<ActionResult<Respons<object>>> DeleteRequest(
        [FromQuery(Name = PlatformQueryParams.LeaveRequestId)] Guid leaveRequestId,
        CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.DeleteRequestAsync(leaveRequestId, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }
}
