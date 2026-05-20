using Microsoft.AspNetCore.Mvc;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Tenant;

namespace ZelosHR.Api.Entities.LifecycleEvents;

/// <summary>Employee lifecycle events — list, create, update, delete.</summary>
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

    [HttpGet("summary")]
    public async Task<ActionResult<Respons<LifecycleEventSummaryDto>>> Summary(CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.GetSummaryAsync(ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet]
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

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Respons<LifecycleEventListItemDto>>> Get(Guid id, CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.GetByIdAsync(id, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost]
    public async Task<ActionResult<Respons<LifecycleEventListItemDto>>> Create(
        [FromBody] CreateLifecycleEventDto body, CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.CreateAsync(body, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<Respons<LifecycleEventListItemDto>>> Update(
        Guid id, [FromBody] UpdateLifecycleEventDto body, CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.UpdateAsync(id, body, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<Respons<object>>> Delete(Guid id, CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.DeleteAsync(id, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }
}
