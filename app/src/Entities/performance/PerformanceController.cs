using Microsoft.AspNetCore.Mvc;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Tenant;

namespace ZelosHR.Api.Entities.Performance;

/// <summary>Performance reviews — full CRUD.</summary>
[ApiController]
[ApiExplorerSettings(GroupName = SwaggerGroups.Performance)]
[Route("api/v1/performance")]
[Produces("application/json")]
public class PerformanceController : ControllerBase
{
    private readonly PerformanceService _service;
    private readonly ITenantContextAccessor _tenant;
    public PerformanceController(PerformanceService s, ITenantContextAccessor t) { _service = s; _tenant = t; }

    [HttpGet("summary")]
    public async Task<ActionResult<Respons<PerformanceSummaryDto>>> Summary(CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.GetSummaryAsync(ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet]
    public async Task<ActionResult<Respons<PerformanceListDto>>> List(
        [FromQuery] string? search, [FromQuery] string? status,
        [FromQuery] int page = 1, [FromQuery] int size = 20, CancellationToken ct = default)
    {
        var ctx = _tenant.Current;
        var result = await _service.ListAsync(search, status, page, size, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Respons<PerformanceReviewListItemDto>>> Get(Guid id, CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.GetByIdAsync(id, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost]
    public async Task<ActionResult<Respons<PerformanceReviewListItemDto>>> Create(
        [FromBody] CreatePerformanceReviewDto body, CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.CreateAsync(body, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<Respons<PerformanceReviewListItemDto>>> Update(
        Guid id, [FromBody] UpdatePerformanceReviewDto body, CancellationToken ct)
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
