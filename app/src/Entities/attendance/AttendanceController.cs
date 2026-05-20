using Microsoft.AspNetCore.Mvc;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Tenant;

namespace ZelosHR.Api.Entities.Attendance;

/// <summary>Daily attendance records — clock-in/out, status, hours.</summary>
[ApiController]
[ApiExplorerSettings(GroupName = SwaggerGroups.Attendance)]
[Route("api/v1/attendance")]
[Produces("application/json")]
public class AttendanceController : ControllerBase
{
    private readonly AttendanceService _service;
    private readonly ITenantContextAccessor _tenant;

    public AttendanceController(AttendanceService service, ITenantContextAccessor tenant)
    {
        _service = service;
        _tenant = tenant;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<Respons<AttendanceSummaryDto>>> Summary(
        [FromQuery] DateOnly? date, CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.GetSummaryAsync(ctx.TenantId, ctx.OrgId, date, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet]
    public async Task<ActionResult<Respons<AttendanceListDto>>> List(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] string? branch,
        [FromQuery] DateOnly? date,
        [FromQuery] int page = 1,
        [FromQuery] int size = 20,
        CancellationToken ct = default)
    {
        var ctx = _tenant.Current;
        var result = await _service.ListAsync(
            search, status, branch, date, page, size, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Respons<AttendanceListItemDto>>> Get(Guid id, CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.GetByIdAsync(id, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost]
    public async Task<ActionResult<Respons<AttendanceListItemDto>>> Create(
        [FromBody] CreateAttendanceDto body, CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.CreateAsync(body, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<Respons<AttendanceListItemDto>>> Update(
        Guid id, [FromBody] UpdateAttendanceDto body, CancellationToken ct)
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
