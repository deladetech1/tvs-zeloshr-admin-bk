using Microsoft.AspNetCore.Mvc;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Constants;
using ZelosHR.Api.Shared.Tenant;

namespace ZelosHR.Api.Entities.LifecycleEvents;

[ApiController]
[ApiExplorerSettings(GroupName = SwaggerGroups.LifecycleEvents)]
[Route("api/v1/lifecycle-events")]
[Produces("application/json")]
public class LifecycleEventsController : ControllerBase
{
    private readonly LifecycleEventsService _service;
    private readonly ITenantContextAccessor _tenant;

    public LifecycleEventsController(LifecycleEventsService service, ITenantContextAccessor tenant)
    {
        _service = service;
        _tenant = tenant;
    }

    [HttpGet("statistics")]
    public async Task<ActionResult<Respons<LifecycleEventSummaryDto>>> Statistics(CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.GetSummaryAsync(ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("list")]
    public async Task<ActionResult<Respons<LifecycleEventListDto>>> List(
        [FromQuery] string? search,
        [FromQuery] string? eventType,
        [FromQuery] string? urgency,
        [FromQuery] string? department,
        [FromQuery] int page = 1,
        [FromQuery] int size = 10,
        CancellationToken ct = default)
    {
        var ctx = _tenant.Current;
        var result = await _service.ListAsync(
            search, eventType, urgency, department, page, size, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("get")]
    public async Task<ActionResult<Respons<LifecycleEventListItemDto>>> Get(
        [FromQuery(Name = PlatformQueryParams.LifecycleEventId)] Guid lifecycleEventId,
        CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.GetByIdAsync(lifecycleEventId, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("add")]
    public async Task<ActionResult<Respons<LifecycleEventListItemDto>>> Create(
        [FromBody] CreateLifecycleEventDto body,
        CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.CreateAsync(body, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("update")]
    public async Task<ActionResult<Respons<LifecycleEventListItemDto>>> Update(
        [FromQuery(Name = PlatformQueryParams.LifecycleEventId)] Guid lifecycleEventId,
        [FromBody] UpdateLifecycleEventDto body,
        CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.UpdateAsync(lifecycleEventId, body, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpDelete("delete")]
    public async Task<ActionResult<Respons<object>>> Delete(
        [FromQuery(Name = PlatformQueryParams.LifecycleEventId)] Guid lifecycleEventId,
        CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.DeleteAsync(lifecycleEventId, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }
}
