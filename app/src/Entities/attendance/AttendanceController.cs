using Microsoft.AspNetCore.Mvc;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Constants;
using ZelosHR.Api.Shared.Tenant;

namespace ZelosHR.Api.Entities.Attendance;

[ApiController]
[ApiExplorerSettings(GroupName = SwaggerGroups.Attendance, IgnoreApi = true)]
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

    [HttpGet("statistics")]
    public async Task<ActionResult<Respons<AttendanceSummaryDto>>> Statistics(
        [FromQuery(Name = PlatformQueryParams.Date)] DateOnly? date,
        [FromQuery(Name = PlatformQueryParams.FromDate)] DateOnly? fromDate,
        [FromQuery(Name = PlatformQueryParams.ToDate)] DateOnly? toDate,
        CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.GetSummaryAsync(ctx.TenantId, ctx.OrgId, date ?? fromDate, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("list")]
    public async Task<ActionResult<Respons<AttendanceListDto>>> List(
        [FromQuery(Name = PlatformQueryParams.Search)] string? search,
        [FromQuery(Name = PlatformQueryParams.Status)] string? status,
        [FromQuery(Name = PlatformQueryParams.Branch)] string? branch,
        [FromQuery(Name = PlatformQueryParams.Date)] DateOnly? date,
        [FromQuery(Name = PlatformQueryParams.Page)] int page = 1,
        [FromQuery(Name = PlatformQueryParams.Size)] int size = 20,
        CancellationToken ct = default)
    {
        var ctx = _tenant.Current;
        var result = await _service.ListAsync(
            search, status, branch, date, page, size, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("get")]
    public async Task<ActionResult<Respons<AttendanceListItemDto>>> Get(
        [FromQuery(Name = PlatformQueryParams.AttendanceId)] Guid attendanceId,
        CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.GetByIdAsync(attendanceId, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("add")]
    public async Task<ActionResult<Respons<AttendanceListItemDto>>> Create(
        [FromBody] CreateAttendanceDto body,
        CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.CreateAsync(body, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("update")]
    public async Task<ActionResult<Respons<AttendanceListItemDto>>> Update(
        [FromQuery(Name = PlatformQueryParams.AttendanceId)] Guid attendanceId,
        [FromBody] UpdateAttendanceDto body,
        CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.UpdateAsync(attendanceId, body, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpDelete("delete")]
    public async Task<ActionResult<Respons<object>>> Delete(
        [FromQuery(Name = PlatformQueryParams.AttendanceId)] Guid attendanceId,
        CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.DeleteAsync(attendanceId, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }
}
