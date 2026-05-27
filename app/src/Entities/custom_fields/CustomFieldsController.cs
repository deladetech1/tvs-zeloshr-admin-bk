using Microsoft.AspNetCore.Mvc;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Authorization;
using ZelosHR.Api.Shared.Constants;
using ZelosHR.Api.Shared.Tenant;

namespace ZelosHR.Api.Entities.CustomFields;

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

    [HttpGet("schema")]
    [RequiresZelosHrPermission(ZelosHrPermissions.CustomFieldsGet)]
    public async Task<ActionResult<Respons<CustomFieldSchemaDto>>> Schema(
        [FromQuery] string entityType,
        CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.GetSchemaAsync(entityType, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("list")]
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
