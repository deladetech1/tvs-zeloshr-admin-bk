using Microsoft.AspNetCore.Mvc;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Constants;
using ZelosHR.Api.Shared.Tenant;

namespace ZelosHR.Api.Entities.Clock;

[ApiController]
[ApiExplorerSettings(GroupName = SwaggerGroups.Clock)]
[Route("api/v1/clock")]
[Produces("application/json")]
public sealed class ClockController : ControllerBase
{
    private readonly ClockService _service;
    private readonly ITenantContextAccessor _tenant;

    public ClockController(ClockService service, ITenantContextAccessor tenant)
    {
        _service = service;
        _tenant = tenant;
    }

    /// <summary>Today's punch state for an employee.</summary>
    [HttpGet("today")]
    public async Task<ActionResult<Respons<ClockTodayDto>>> Today(
        [FromQuery(Name = PlatformQueryParams.EmployeeId)] Guid employeeId,
        CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.TodayAsync(employeeId, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Clock in (or return from break).</summary>
    [HttpPost("in")]
    public async Task<ActionResult<Respons<ClockTodayDto>>> ClockIn(
        [FromBody] ClockActionRequest body,
        CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.ClockInAsync(body, ctx.TenantId, ctx.OrgId, ctx.UserId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Clock out (or start a break).</summary>
    [HttpPost("out")]
    public async Task<ActionResult<Respons<ClockTodayDto>>> ClockOut(
        [FromBody] ClockActionRequest body,
        CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.ClockOutAsync(body, ctx.TenantId, ctx.OrgId, ctx.UserId, ct);
        return StatusCode(result.StatusCode, result);
    }
}
