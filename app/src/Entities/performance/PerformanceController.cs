using Microsoft.AspNetCore.Mvc;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Constants;
using ZelosHR.Api.Shared.Tenant;

namespace ZelosHR.Api.Entities.Performance;

[ApiController]
[ApiExplorerSettings(GroupName = SwaggerGroups.Performance, IgnoreApi = true)]
[Route("api/v1/performance")]
[Produces("application/json")]
public class PerformanceController : ControllerBase
{
    private readonly PerformanceService _service;
    private readonly ITenantContextAccessor _tenant;

    public PerformanceController(PerformanceService service, ITenantContextAccessor tenant)
    {
        _service = service;
        _tenant = tenant;
    }

    [HttpGet("statistics")]
    public async Task<ActionResult<Respons<PerformanceSummaryDto>>> Statistics(CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.GetSummaryAsync(ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("list")]
    public async Task<ActionResult<Respons<PerformanceListDto>>> List(
        [FromQuery(Name = PlatformQueryParams.Search)] string? search,
        [FromQuery(Name = PlatformQueryParams.Status)] string? status,
        [FromQuery(Name = PlatformQueryParams.Page)] int page = 1,
        [FromQuery(Name = PlatformQueryParams.Size)] int size = 20,
        CancellationToken ct = default)
    {
        var ctx = _tenant.Current;
        var result = await _service.ListAsync(search, status, page, size, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("get")]
    public async Task<ActionResult<Respons<PerformanceReviewListItemDto>>> Get(
        [FromQuery(Name = PlatformQueryParams.PerformanceId)] Guid performanceId,
        CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.GetByIdAsync(performanceId, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("add")]
    public async Task<ActionResult<Respons<PerformanceReviewListItemDto>>> Create(
        [FromBody] CreatePerformanceReviewDto body,
        CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.CreateAsync(body, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("update")]
    public async Task<ActionResult<Respons<PerformanceReviewListItemDto>>> Update(
        [FromQuery(Name = PlatformQueryParams.PerformanceId)] Guid performanceId,
        [FromBody] UpdatePerformanceReviewDto body,
        CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.UpdateAsync(performanceId, body, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpDelete("delete")]
    public async Task<ActionResult<Respons<object>>> Delete(
        [FromQuery(Name = PlatformQueryParams.PerformanceId)] Guid performanceId,
        CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.DeleteAsync(performanceId, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }
}
