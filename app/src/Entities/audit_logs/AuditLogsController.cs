using Microsoft.AspNetCore.Mvc;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Constants;
using ZelosHR.Api.Shared.Tenant;

namespace ZelosHR.Api.Entities.AuditLogs;

/// <summary>Read-only audit trail for HR actions (entries appended by the API on mutations).</summary>
[ApiController]
[ApiExplorerSettings(GroupName = SwaggerGroups.AuditLogs)]
[Route("api/v1/audit-logs")]
[Produces("application/json")]
public class AuditLogsController : ControllerBase
{
    private readonly AuditLogsService _service;
    private readonly ITenantContextAccessor _tenant;

    public AuditLogsController(AuditLogsService service, ITenantContextAccessor tenant)
    {
        _service = service;
        _tenant = tenant;
    }

    [HttpGet("statistics")]
    public async Task<ActionResult<Respons<AuditLogSummaryDto>>> Statistics(CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.GetSummaryAsync(ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("list")]
    public async Task<ActionResult<Respons<AuditLogListDto>>> List(
        [FromQuery] string? search,
        [FromQuery] string? action,
        [FromQuery] string? severity,
        [FromQuery] string? actor,
        [FromQuery] int page = 1,
        [FromQuery] int size = 20,
        CancellationToken ct = default)
    {
        var ctx = _tenant.Current;
        var result = await _service.ListAsync(
            search, action, severity, actor, page, size, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("get")]
    public async Task<ActionResult<Respons<AuditLogListItemDto>>> Get(
        [FromQuery(Name = PlatformQueryParams.AuditLogId)] Guid auditLogId,
        CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.GetByIdAsync(auditLogId, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }
}
