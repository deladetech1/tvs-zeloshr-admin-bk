using Microsoft.AspNetCore.Mvc;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Constants;
using ZelosHR.Api.Shared.Tenant;

namespace ZelosHR.Api.Entities.Disciplinary;

[ApiController]
[ApiExplorerSettings(GroupName = SwaggerGroups.Disciplinary, IgnoreApi = true)]
[Route("api/v1/disciplinary")]
[Produces("application/json")]
public class DisciplinaryController : ControllerBase
{
    private readonly DisciplinaryService _service;
    private readonly ITenantContextAccessor _tenant;

    public DisciplinaryController(DisciplinaryService service, ITenantContextAccessor tenant)
    {
        _service = service;
        _tenant = tenant;
    }

    [HttpGet("statistics")]
    public async Task<ActionResult<Respons<DisciplinarySummaryDto>>> Statistics(CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.GetSummaryAsync(ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("list")]
    public async Task<ActionResult<Respons<DisciplinaryListDto>>> List(
        [FromQuery(Name = PlatformQueryParams.Search)] string? search,
        [FromQuery(Name = PlatformQueryParams.Status)] string? status,
        [FromQuery(Name = PlatformQueryParams.Severity)] string? severity,
        [FromQuery(Name = PlatformQueryParams.Page)] int page = 1,
        [FromQuery(Name = PlatformQueryParams.Size)] int size = 20,
        CancellationToken ct = default)
    {
        var ctx = _tenant.Current;
        var result = await _service.ListAsync(search, status, severity, page, size, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("get")]
    public async Task<ActionResult<Respons<DisciplinaryCaseListItemDto>>> Get(
        [FromQuery(Name = PlatformQueryParams.DisciplinaryId)] Guid disciplinaryId,
        CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.GetByIdAsync(disciplinaryId, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("add")]
    public async Task<ActionResult<Respons<DisciplinaryCaseListItemDto>>> Create(
        [FromBody] CreateDisciplinaryCaseDto body,
        CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.CreateAsync(body, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("update")]
    public async Task<ActionResult<Respons<DisciplinaryCaseListItemDto>>> Update(
        [FromQuery(Name = PlatformQueryParams.DisciplinaryId)] Guid disciplinaryId,
        [FromBody] UpdateDisciplinaryCaseDto body,
        CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.UpdateAsync(disciplinaryId, body, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpDelete("delete")]
    public async Task<ActionResult<Respons<object>>> Delete(
        [FromQuery(Name = PlatformQueryParams.DisciplinaryId)] Guid disciplinaryId,
        CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.DeleteAsync(disciplinaryId, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }
}
