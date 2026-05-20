using Microsoft.AspNetCore.Mvc;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Tenant;

namespace ZelosHR.Api.Entities.Disciplinary;

/// <summary>Disciplinary cases — full CRUD.</summary>
[ApiController]
[ApiExplorerSettings(GroupName = SwaggerGroups.Disciplinary)]
[Route("api/v1/disciplinary")]
[Produces("application/json")]
public class DisciplinaryController : ControllerBase
{
    private readonly DisciplinaryService _service;
    private readonly ITenantContextAccessor _tenant;
    public DisciplinaryController(DisciplinaryService s, ITenantContextAccessor t) { _service = s; _tenant = t; }

    [HttpGet("summary")]
    public async Task<ActionResult<Respons<DisciplinarySummaryDto>>> Summary(CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.GetSummaryAsync(ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet]
    public async Task<ActionResult<Respons<DisciplinaryListDto>>> List(
        [FromQuery] string? search, [FromQuery] string? status, [FromQuery] string? severity,
        [FromQuery] int page = 1, [FromQuery] int size = 20, CancellationToken ct = default)
    {
        var ctx = _tenant.Current;
        var result = await _service.ListAsync(search, status, severity, page, size, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Respons<DisciplinaryCaseListItemDto>>> Get(Guid id, CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.GetByIdAsync(id, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost]
    public async Task<ActionResult<Respons<DisciplinaryCaseListItemDto>>> Create(
        [FromBody] CreateDisciplinaryCaseDto body, CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.CreateAsync(body, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<Respons<DisciplinaryCaseListItemDto>>> Update(
        Guid id, [FromBody] UpdateDisciplinaryCaseDto body, CancellationToken ct)
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
