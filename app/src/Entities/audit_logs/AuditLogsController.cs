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
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeGet)]
    [HttpGet("list")]
    [ProducesResponseType(typeof(Respons<AuditLogListDto>), StatusCodes.Status200OK)]
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
