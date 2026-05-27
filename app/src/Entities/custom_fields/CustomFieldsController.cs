using Microsoft.AspNetCore.Mvc;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Authorization;
using ZelosHR.Api.Shared.Tenant;

namespace ZelosHR.Api.Entities.CustomFields;

/// <summary>Custom field definitions — admin CRUD, filterable list, form schema, audit log.</summary>
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

    /// <summary>Counts of definitions (total, active, soft-deleted).</summary>
    [HttpGet("summary")]
    [RequiresZelosHrPermission(ZelosHrPermissions.CustomFieldsGet)]
    public async Task<ActionResult<Respons<CustomFieldsSummaryDto>>> Summary(CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.GetSummaryAsync(ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Supported <c>entity_type</c> values for definitions.</summary>
    [HttpGet("entity-types")]
    [RequiresZelosHrPermission(ZelosHrPermissions.CustomFieldsGet)]
    public async Task<ActionResult<Respons<IReadOnlyList<string>>>> EntityTypes(CancellationToken ct)
    {
        var result = await _service.GetEntityTypesAsync();
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Active field definitions for forms (ordered by section and display order).</summary>
    [HttpGet("schema")]
    [RequiresZelosHrPermission(ZelosHrPermissions.CustomFieldsGet)]
    public async Task<ActionResult<Respons<CustomFieldSchemaDto>>> Schema(
        [FromQuery] string entityType, CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.GetSchemaAsync(entityType, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Paginated list with filters on all definition properties:
    /// search, entityType, fieldKey, label, fieldType, flags, sectionName, includeDeleted, sortBy, sortOrder.
    /// </summary>
    [HttpGet]
    [RequiresZelosHrPermission(ZelosHrPermissions.CustomFieldsGet)]
    public async Task<ActionResult<Respons<CustomFieldDefinitionListDto>>> List(
        [FromQuery] string? search,
        [FromQuery] string? entityType,
        [FromQuery] string? fieldKey,
        [FromQuery] string? label,
        [FromQuery] string? fieldType,
        [FromQuery] bool? isRequired,
        [FromQuery] bool? isSensitive,
        [FromQuery] bool? isFilterable,
        [FromQuery] bool? isSearchable,
        [FromQuery] bool? isActive,
        [FromQuery] string? sectionName,
        [FromQuery] bool includeDeleted = false,
        [FromQuery] string? sortBy = "label",
        [FromQuery] string? sortOrder = "asc",
        [FromQuery] int page = 1,
        [FromQuery] int size = 20,
        CancellationToken ct = default)
    {
        var ctx = _tenant.Current;
        var query = new CustomFieldListQuery
        {
            Search = search,
            EntityType = entityType,
            FieldKey = fieldKey,
            Label = label,
            FieldType = fieldType,
            IsRequired = isRequired,
            IsSensitive = isSensitive,
            IsFilterable = isFilterable,
            IsSearchable = isSearchable,
            IsActive = isActive,
            SectionName = sectionName,
            IncludeDeleted = includeDeleted,
            SortBy = sortBy,
            SortOrder = sortOrder,
            Page = page,
            Size = size,
        };
        var result = await _service.ListAsync(query, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Value change history for custom fields on host entities.</summary>
    [HttpGet("audit-logs")]
    [RequiresZelosHrPermission(ZelosHrPermissions.CustomFieldValuesGet)]
    public async Task<ActionResult<Respons<CustomFieldAuditLogListDto>>> AuditLogs(
        [FromQuery] string? entityType,
        [FromQuery] Guid? entityId,
        [FromQuery] string? fieldKey,
        [FromQuery] string? changeType,
        [FromQuery] string? changedBy,
        [FromQuery] DateTimeOffset? changedFrom,
        [FromQuery] DateTimeOffset? changedTo,
        [FromQuery] int page = 1,
        [FromQuery] int size = 20,
        CancellationToken ct = default)
    {
        var ctx = _tenant.Current;
        var result = await _service.ListAuditLogsAsync(
            entityType, entityId, fieldKey, changeType, changedBy, changedFrom, changedTo,
            page, size, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("reorder")]
    [RequiresZelosHrPermission(ZelosHrPermissions.CustomFieldsAdmin)]
    public async Task<ActionResult<Respons<object>>> Reorder(
        [FromBody] ReorderCustomFieldDefinitionDto body, CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.ReorderAsync(body, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("{id:guid}")]
    [RequiresZelosHrPermission(ZelosHrPermissions.CustomFieldsGet)]
    public async Task<ActionResult<Respons<CustomFieldDefinitionDto>>> Get(Guid id, CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.GetByIdAsync(id, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost]
    [RequiresZelosHrPermission(ZelosHrPermissions.CustomFieldsCreate)]
    public async Task<ActionResult<Respons<CustomFieldDefinitionDto>>> Create(
        [FromBody] CreateCustomFieldDefinitionDto body, CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.CreateAsync(body, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("{id:guid}")]
    [RequiresZelosHrPermission(ZelosHrPermissions.CustomFieldsUpdate)]
    public async Task<ActionResult<Respons<CustomFieldDefinitionDto>>> Update(
        Guid id, [FromBody] UpdateCustomFieldDefinitionDto body, CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.UpdateAsync(id, body, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpDelete("{id:guid}")]
    [RequiresZelosHrPermission(ZelosHrPermissions.CustomFieldsDelete)]
    public async Task<ActionResult<Respons<object>>> Delete(Guid id, CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.DeleteAsync(id, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }
}
