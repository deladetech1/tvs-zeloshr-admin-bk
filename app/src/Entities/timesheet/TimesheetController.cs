using Microsoft.AspNetCore.Mvc;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Clock;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Constants;
using ZelosHR.Api.Shared.Tenant;

namespace ZelosHR.Api.Entities.Timesheet;

[ApiController]
[ApiExplorerSettings(GroupName = SwaggerGroups.Timesheet)]
[Route("api/v1/timesheet")]
[Produces("application/json")]
public sealed class TimesheetController : ControllerBase
{
    private readonly ClockService _service;
    private readonly ITenantContextAccessor _tenant;

    public TimesheetController(ClockService service, ITenantContextAccessor tenant)
    {
        _service = service;
        _tenant = tenant;
    }

    /// <summary>Personal timesheet for a date range (defaults to the last 7 days).</summary>
    [HttpGet("get")]
    public async Task<ActionResult<Respons<TimesheetDto>>> Get(
        [FromQuery(Name = PlatformQueryParams.EmployeeId)] Guid employeeId,
        [FromQuery(Name = PlatformQueryParams.FromDate)] DateOnly? fromDate,
        [FromQuery(Name = PlatformQueryParams.ToDate)] DateOnly? toDate,
        CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.TimesheetAsync(employeeId, fromDate, toDate, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }
}
