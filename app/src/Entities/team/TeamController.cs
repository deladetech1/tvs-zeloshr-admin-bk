using Microsoft.AspNetCore.Mvc;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Clock;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Constants;
using ZelosHR.Api.Shared.Tenant;

namespace ZelosHR.Api.Entities.Team;

[ApiController]
[ApiExplorerSettings(GroupName = SwaggerGroups.Team)]
[Route("api/v1/team")]
[Produces("application/json")]
public sealed class TeamController : ControllerBase
{
    private readonly ClockService _service;
    private readonly ITenantContextAccessor _tenant;

    public TeamController(ClockService service, ITenantContextAccessor tenant)
    {
        _service = service;
        _tenant = tenant;
    }

    /// <summary>Team attendance for a date. Use scope=reports|department|all.</summary>
    [HttpGet("list")]
    public async Task<ActionResult<Respons<TeamDto>>> Get(
        [FromQuery(Name = PlatformQueryParams.EmployeeId)] Guid? employeeId,
        [FromQuery(Name = PlatformQueryParams.Date)] DateOnly? date,
        [FromQuery(Name = "scope")] string? scope,
        [FromQuery(Name = PlatformQueryParams.Department)] string? department,
        CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.TeamAsync(employeeId, date, scope, department, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }
}
