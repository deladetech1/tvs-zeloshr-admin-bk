using Microsoft.AspNetCore.Mvc;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Tenant;

namespace ZelosHR.Api.Entities.Dashboard;

/// <summary>Executive dashboard KPIs and recent activity (read-only).</summary>
[ApiController]
[ApiExplorerSettings(GroupName = SwaggerGroups.Dashboard, IgnoreApi = true)]
[Route("api/v1/dashboard")]
[Produces("application/json")]
public class DashboardController : ControllerBase
{
    private readonly DashboardService _service;
    private readonly ITenantContextAccessor _tenant;

    public DashboardController(DashboardService service, ITenantContextAccessor tenant)
    {
        _service = service;
        _tenant = tenant;
    }

    [HttpGet]
    public async Task<ActionResult<Respons<DashboardDto>>> Get(CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.GetDashboardAsync(ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("statistics")]
    public async Task<ActionResult<Respons<DashboardSummaryDto>>> Statistics(CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.GetDashboardAsync(ctx.TenantId, ctx.OrgId, ct);
        if (!result.Success || result.Data is null)
            return StatusCode(result.StatusCode, result);
        return Ok(Respons<DashboardSummaryDto>.Ok(result.Data.Summary));
    }
}
