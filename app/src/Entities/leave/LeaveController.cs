using Microsoft.AspNetCore.Mvc;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Tenant;

namespace ZelosHR.Api.Entities.Leave;

/// <summary>Leave requests and balances — CRUD under <c>/requests</c>.</summary>
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

    [HttpGet("summary")]
    public async Task<ActionResult<Respons<LeaveSummaryDto>>> Summary(CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.GetSummaryAsync(ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet]
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

    [HttpGet("requests/{id:guid}")]
    public async Task<ActionResult<Respons<LeaveRequestListItemDto>>> GetRequest(Guid id, CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.GetRequestByIdAsync(id, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("requests")]
    public async Task<ActionResult<Respons<LeaveRequestListItemDto>>> CreateRequest(
        [FromBody] CreateLeaveRequestDto body, CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.CreateRequestAsync(body, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("requests/{id:guid}")]
    public async Task<ActionResult<Respons<LeaveRequestListItemDto>>> UpdateRequest(
        Guid id, [FromBody] UpdateLeaveRequestDto body, CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.UpdateRequestAsync(id, body, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpDelete("requests/{id:guid}")]
    public async Task<ActionResult<Respons<object>>> DeleteRequest(Guid id, CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.DeleteRequestAsync(id, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }
}
