using Microsoft.AspNetCore.Mvc;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Authorization;
using ZelosHR.Api.Shared.Constants;
using ZelosHR.Api.Shared.Tenant;

namespace ZelosHR.Api.Entities.CustomFields;

/// <summary>
/// Tenant-scoped custom field **definitions** (schema). Values are stored on employees via
/// nested <c>custom_fields</c> on <c>POST /employees/add</c> and <c>PUT /employees/update</c>.
/// </summary>
[ApiController]
[ApiExplorerSettings(GroupName = SwaggerGroups.CustomFields)]
[Route("api/v1/custom-fields")]
[Produces("application/json")]
public class CustomFieldsController : ControllerBase
{
    private readonly CustomFieldsService _service;
    private readonly ITenantContextAccessor _tenant;

    public CustomFieldsController(CustomFieldsService service, ITenantContextAccessor tenant)
    {
        _service = service;
        _tenant = tenant;
    }

    [HttpGet("statistics")]
    [RequiresZelosHrPermission(ZelosHrPermissions.CustomFieldsGet)]
    public async Task<ActionResult<Respons<CustomFieldsSummaryDto>>> Statistics(CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.GetSummaryAsync(ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("entity-types")]
    [RequiresZelosHrPermission(ZelosHrPermissions.CustomFieldsGet)]
    public async Task<ActionResult<Respons<IReadOnlyList<string>>>> EntityTypes(CancellationToken ct)
    {
        var result = await _service.GetEntityTypesAsync();
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Active field definitions for an entity type — use to build employee forms.</summary>
    /// <remarks>
    /// Filter by `sectionName`: identity | employment | compensation | education | certification.
    /// Returned `field_key` values are the keys used in employee section `custom_fields` objects.
    /// </remarks>
    [HttpGet("schema")]
    [RequiresZelosHrPermission(ZelosHrPermissions.CustomFieldsGet)]
    public async Task<ActionResult<Respons<CustomFieldSchemaDto>>> Schema(
        [FromQuery]
        [SwaggerAllowedValues(typeof(CustomFieldEntityTypes), nameof(CustomFieldEntityTypes.All))]
        string entityType,
        CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.GetSchemaAsync(entityType, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("list")]
    [RequiresZelosHrPermission(ZelosHrPermissions.CustomFieldsGet)]
    public async Task<ActionResult<Respons<CustomFieldDefinitionListDto>>> List(
        [FromQuery] CustomFieldListQuery query,
        CancellationToken ct = default)
    {
        var ctx = _tenant.Current;
        var result = await _service.ListAsync(query, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("audit-logs")]
    [RequiresZelosHrPermission(ZelosHrPermissions.CustomFieldValuesGet)]
    public async Task<ActionResult<Respons<CustomFieldAuditLogListDto>>> AuditLogs(
        [FromQuery] CustomFieldAuditLogQuery query,
        CancellationToken ct = default)
    {
        var ctx = _tenant.Current;
        var result = await _service.ListAuditLogsAsync(
            query.EntityType, query.EntityId, query.FieldKey, query.ChangeType, query.ChangedBy,
            query.ChangedFrom, query.ChangedTo, query.Page, query.Size, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("reorder")]
    [RequiresZelosHrPermission(ZelosHrPermissions.CustomFieldsAdmin)]
    public async Task<ActionResult<Respons<object>>> Reorder(
        [FromBody] ReorderCustomFieldDefinitionDto body,
        CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.ReorderAsync(body, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("get")]
    [RequiresZelosHrPermission(ZelosHrPermissions.CustomFieldsGet)]
    public async Task<ActionResult<Respons<CustomFieldDefinitionDto>>> Get(
        [FromQuery(Name = PlatformQueryParams.CustomFieldId)] Guid customFieldId,
        CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.GetByIdAsync(customFieldId, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Create a custom field definition (schema only — not a value).</summary>
    /// <remarks>
    /// See **Examples** for compensation select and identity text field payloads.
    /// After creating definitions, send values on employee create/update under the matching section's `custom_fields`.
    /// </remarks>
    [HttpPost("add")]
    [RequiresZelosHrPermission(ZelosHrPermissions.CustomFieldsCreate)]
    public async Task<ActionResult<Respons<CustomFieldDefinitionDto>>> Create(
        [FromBody] CreateCustomFieldDefinitionDto body,
        CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.CreateAsync(body, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("update")]
    [RequiresZelosHrPermission(ZelosHrPermissions.CustomFieldsUpdate)]
    public async Task<ActionResult<Respons<CustomFieldDefinitionDto>>> Update(
        [FromQuery(Name = PlatformQueryParams.CustomFieldId)] Guid customFieldId,
        [FromBody] UpdateCustomFieldDefinitionDto body,
        CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.UpdateAsync(customFieldId, body, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpDelete("delete")]
    [RequiresZelosHrPermission(ZelosHrPermissions.CustomFieldsDelete)]
    public async Task<ActionResult<Respons<object>>> Delete(
        [FromQuery(Name = PlatformQueryParams.CustomFieldId)] Guid customFieldId,
        CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.DeleteAsync(customFieldId, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }
}
