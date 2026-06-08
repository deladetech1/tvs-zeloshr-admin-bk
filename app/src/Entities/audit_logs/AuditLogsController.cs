using Microsoft.AspNetCore.Mvc;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Authorization;
using ZelosHR.Api.Shared.Constants;
using ZelosHR.Api.Shared.Tenant;
using ZelosHR.Api.Shared.Validation;

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

    /// <summary>Audit log KPI cards.</summary>
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeGet)]
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(Respons<AuditLogSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Respons<AuditLogSummaryDto>>> Statistics(CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.GetSummaryAsync(ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Paginated audit log table.</summary>
    /// <remarks>
    /// Optional <c>start_date</c> / <c>end_date</c> filter by <c>occurred_at</c> (UTC day boundaries).
    /// Same filters as <c>GET /audit-logs/export</c>.
    /// </remarks>
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeGet)]
    [HttpGet("list")]
    [ProducesResponseType(typeof(Respons<AuditLogListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<AuditLogListDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Respons<AuditLogListDto>>> List(
        [FromQuery] AuditLogListQuery query,
        CancellationToken ct = default)
    {
        var ctx = _tenant.Current;
        var result = await _service.ListAsync(query, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Export audit log entries as CSV.</summary>
    /// <remarks>
    /// Accepts the same filters as <c>GET /audit-logs/list</c> (search, action, severity, actor, start_date, end_date).
    /// </remarks>
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeGet)]
    [HttpGet("export")]
    [Produces("text/csv")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Export(
        [FromQuery] AuditLogExportQuery query,
        CancellationToken ct = default)
    {
        var ctx = _tenant.Current;
        var result = await _service.ExportCsvAsync(query, ctx.TenantId, ctx.OrgId, ct);
        if (!result.Success || result.Data is null)
            return StatusCode(result.StatusCode, result);

        var fileName = $"audit_logs_export_{DateTime.UtcNow:yyyyMMdd}.csv";
        return File(result.Data, "text/csv", fileName);
    }

    /// <summary>Delete audit log entries older than three months for the current org.</summary>
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeUpdate)]
    [HttpDelete("purge")]
    [ProducesResponseType(typeof(Respons<AuditLogPurgeResultDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Respons<AuditLogPurgeResultDto>>> Purge(CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.PurgeOldAsync(ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Single audit log entry.</summary>
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeGet)]
    [HttpGet("get")]
    [ProducesResponseType(typeof(Respons<AuditLogListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<AuditLogListItemDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Respons<AuditLogListItemDto>>> Get(
        [FromQuery(Name = PlatformQueryParams.AuditLogId)] Guid auditLogId,
        CancellationToken ct)
    {
        if (QueryParamValidation.BadRequestIfEmptyGuid<AuditLogListItemDto>(
                auditLogId, PlatformQueryParams.AuditLogId) is { } missingAuditLogId)
            return missingAuditLogId;

        var ctx = _tenant.Current;
        var result = await _service.GetByIdAsync(auditLogId, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }
}
