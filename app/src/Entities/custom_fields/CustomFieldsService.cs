using System.Text.RegularExpressions;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Persistence.Repositories;
using ZelosHR.Api.Shared.Pagination;
using ZelosHR.Api.Shared.Tenant;

namespace ZelosHR.Api.Entities.CustomFields;

public partial class CustomFieldsService
{
    private static readonly Regex FieldKeyPattern = FieldKeyRegex();

    private readonly ICustomFieldDefinitionsRepository _repository;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CustomFieldsService(
        ICustomFieldDefinitionsRepository repository,
        IHttpContextAccessor httpContextAccessor)
    {
        _repository = repository;
        _httpContextAccessor = httpContextAccessor;
    }

    private string? CurrentUserId =>
        _httpContextAccessor.HttpContext?.Items[TrovesuiteHttpContextKeys.UserId] as string;

    public async Task<Respons<CustomFieldsSummaryDto>> GetSummaryAsync(
        string tenantId, string orgId, CancellationToken ct)
    {
        var summary = await _repository.GetSummaryScopedAsync(tenantId, orgId, ct);
        return Respons<CustomFieldsSummaryDto>.Ok(summary);
    }

    public async Task<Respons<CustomFieldDefinitionListDto>> ListAsync(
        CustomFieldListQuery query, string tenantId, string orgId, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(query.EntityType)
            && !CustomFieldEntityTypes.All.Contains(query.EntityType.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            return Respons<CustomFieldDefinitionListDto>.ValidationError(new Dictionary<string, string>
            {
                ["entityType"] = $"Must be one of: {string.Join(" | ", CustomFieldEntityTypes.All)}.",
            });
        }

        var paging = PagedQuery.From(query.Page, query.Size);
        var listQuery = new CustomFieldListQuery
        {
            Search = query.Search,
            EntityType = query.EntityType,
            FieldKey = query.FieldKey,
            Label = query.Label,
            FieldType = query.FieldType,
            IsRequired = query.IsRequired,
            IsSensitive = query.IsSensitive,
            IsFilterable = query.IsFilterable,
            IsSearchable = query.IsSearchable,
            IsActive = query.IsActive,
            SectionName = query.SectionName,
            IncludeDeleted = query.IncludeDeleted,
            SortBy = query.SortBy,
            SortOrder = query.SortOrder,
            Page = paging.Page,
            Size = paging.Size,
        };

        var (items, total) = await _repository.ListScopedAsync(tenantId, orgId, listQuery, ct);
        var summary = await _repository.GetSummaryScopedAsync(tenantId, orgId, ct);

        return Respons<CustomFieldDefinitionListDto>.Ok(
            new CustomFieldDefinitionListDto { Summary = summary, Items = items },
            pagination: new PaginationMeta
            {
                Page = paging.Page,
                Size = paging.Size,
                Total = total,
                HasNext = paging.Offset + items.Count < total,
            });
    }

    public async Task<Respons<CustomFieldSchemaDto>> GetSchemaAsync(
        string entityType, string tenantId, string orgId, CancellationToken ct)
    {
        if (!CustomFieldEntityTypes.All.Contains(entityType.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            return Respons<CustomFieldSchemaDto>.ValidationError(new Dictionary<string, string>
            {
                ["entityType"] = $"Must be one of: {string.Join(" | ", CustomFieldEntityTypes.All)}.",
            });
        }

        var fields = await _repository.ListSchemaScopedAsync(tenantId, orgId, entityType.Trim(), ct);
        return Respons<CustomFieldSchemaDto>.Ok(new CustomFieldSchemaDto
        {
            EntityType = entityType.Trim(),
            Fields = fields,
        });
    }

    public Task<Respons<IReadOnlyList<string>>> GetEntityTypesAsync() =>
        Task.FromResult(Respons<IReadOnlyList<string>>.Ok(CustomFieldEntityTypes.All));

    public async Task<Respons<CustomFieldDefinitionDto>> GetByIdAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct)
    {
        var row = await _repository.GetByIdScopedAsync(id, tenantId, orgId, ct);
        return row is null
            ? Respons<CustomFieldDefinitionDto>.Fail("Custom field definition not found.", statusCode: 404)
            : Respons<CustomFieldDefinitionDto>.Ok(row);
    }

    public async Task<Respons<CustomFieldDefinitionDto>> CreateAsync(
        CreateCustomFieldDefinitionDto body, string tenantId, string orgId, CancellationToken ct)
    {
        var validation = ValidateCreate(body);
        if (validation is not null)
            return Respons<CustomFieldDefinitionDto>.ValidationError(validation);

        if (await _repository.FieldKeyExistsScopedAsync(
                tenantId, orgId, body.EntityType.Trim(), body.FieldKey.Trim(), null, ct))
        {
            return Respons<CustomFieldDefinitionDto>.Fail(
                "A custom field with this entity type and field key already exists.",
                statusCode: 409);
        }

        var id = await _repository.CreateScopedAsync(body, tenantId, orgId, CurrentUserId, ct);
        var created = await _repository.GetByIdScopedAsync(id, tenantId, orgId, ct);
        return Respons<CustomFieldDefinitionDto>.Ok(created!, "Custom field definition created.", statusCode: 201);
    }

    public async Task<Respons<CustomFieldDefinitionDto>> UpdateAsync(
        Guid id, UpdateCustomFieldDefinitionDto body, string tenantId, string orgId, CancellationToken ct)
    {
        var updated = await _repository.UpdateScopedAsync(id, body, tenantId, orgId, CurrentUserId, ct);
        if (updated is not null)
            return Respons<CustomFieldDefinitionDto>.Ok(updated);

        var exists = await _repository.GetByIdScopedAsync(id, tenantId, orgId, ct);
        return exists is null
            ? Respons<CustomFieldDefinitionDto>.Fail("Custom field definition not found.", statusCode: 404)
            : Respons<CustomFieldDefinitionDto>.Fail("No fields to update.", statusCode: 400);
    }

    public async Task<Respons<object>> DeleteAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct)
    {
        if (!await _repository.SoftDeleteScopedAsync(id, tenantId, orgId, CurrentUserId, ct))
            return Respons<object>.Fail("Custom field definition not found.", statusCode: 404);

        return Respons<object>.Ok(new { id = id.ToString() }, "Custom field definition deleted.");
    }

    public async Task<Respons<object>> ReorderAsync(
        ReorderCustomFieldDefinitionDto body, string tenantId, string orgId, CancellationToken ct)
    {
        if (body.Items is null || body.Items.Count == 0)
            return Respons<object>.Fail("At least one item is required.", statusCode: 400);

        var updated = await _repository.ReorderScopedAsync(
            tenantId, orgId, body.Items, CurrentUserId, ct);

        return Respons<object>.Ok(new { updated }, "Display order updated.");
    }

    public async Task<Respons<CustomFieldAuditLogListDto>> ListAuditLogsAsync(
        string? entityType,
        Guid? entityId,
        string? fieldKey,
        string? changeType,
        string? changedBy,
        DateTimeOffset? changedFrom,
        DateTimeOffset? changedTo,
        int page,
        int size,
        string tenantId,
        string orgId,
        CancellationToken ct)
    {
        var paging = PagedQuery.From(page, size);
        var (items, total) = await _repository.ListAuditLogsScopedAsync(
            tenantId,
            orgId,
            entityType,
            entityId,
            fieldKey,
            changeType,
            changedBy,
            changedFrom,
            changedTo,
            paging.Page,
            paging.Size,
            ct);

        return Respons<CustomFieldAuditLogListDto>.Ok(
            new CustomFieldAuditLogListDto { Items = items },
            pagination: new PaginationMeta
            {
                Page = paging.Page,
                Size = paging.Size,
                Total = total,
                HasNext = paging.Offset + items.Count < total,
            });
    }

    private static Dictionary<string, string>? ValidateCreate(CreateCustomFieldDefinitionDto body)
    {
        var errors = new Dictionary<string, string>();

        if (string.IsNullOrWhiteSpace(body.EntityType)
            || !CustomFieldEntityTypes.All.Contains(body.EntityType.Trim(), StringComparer.OrdinalIgnoreCase))
            errors["entityType"] = $"Required. Allowed: {string.Join(" | ", CustomFieldEntityTypes.All)}.";

        if (string.IsNullOrWhiteSpace(body.FieldKey) || !FieldKeyPattern.IsMatch(body.FieldKey.Trim()))
            errors["fieldKey"] = "Required. Use lowercase letters, numbers, and underscores (2–64 chars).";

        if (string.IsNullOrWhiteSpace(body.Label))
            errors["label"] = "Label is required.";

        if (string.IsNullOrWhiteSpace(body.FieldType)
            || !CustomFieldFieldTypes.All.Contains(body.FieldType.Trim(), StringComparer.OrdinalIgnoreCase))
            errors["fieldType"] = $"Required. Allowed: {string.Join(" | ", CustomFieldFieldTypes.All)}.";

        return errors.Count == 0 ? null : errors;
    }

    [GeneratedRegex("^[a-z][a-z0-9_]{1,63}$", RegexOptions.CultureInvariant)]
    private static partial Regex FieldKeyRegex();
}
