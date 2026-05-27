using ZelosHR.Api.Entities.CustomFields;

namespace ZelosHR.Api.Persistence.Repositories;

public interface ICustomFieldDefinitionsRepository
{
    Task<CustomFieldsSummaryDto> GetSummaryScopedAsync(string tenantId, string orgId, CancellationToken ct = default);

    Task<(IReadOnlyList<CustomFieldDefinitionDto> Items, int Total)> ListScopedAsync(
        string tenantId,
        string orgId,
        CustomFieldListQuery query,
        CancellationToken ct = default);

    Task<IReadOnlyList<CustomFieldDefinitionDto>> ListSchemaScopedAsync(
        string tenantId,
        string orgId,
        string entityType,
        CancellationToken ct = default);

    Task<CustomFieldDefinitionDto?> GetByIdScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default);

    Task<bool> FieldKeyExistsScopedAsync(
        string tenantId,
        string orgId,
        string entityType,
        string fieldKey,
        Guid? excludeId,
        CancellationToken ct = default);

    Task<Guid> CreateScopedAsync(
        CreateCustomFieldDefinitionDto data,
        string tenantId,
        string orgId,
        string? createdBy,
        CancellationToken ct = default);

    Task<CustomFieldDefinitionDto?> UpdateScopedAsync(
        Guid id,
        UpdateCustomFieldDefinitionDto data,
        string tenantId,
        string orgId,
        string? updatedBy,
        CancellationToken ct = default);

    Task<bool> SoftDeleteScopedAsync(
        Guid id, string tenantId, string orgId, string? updatedBy, CancellationToken ct = default);

    Task<int> ReorderScopedAsync(
        string tenantId,
        string orgId,
        IReadOnlyList<ReorderCustomFieldItemDto> items,
        string? updatedBy,
        CancellationToken ct = default);

    Task<(IReadOnlyList<CustomFieldAuditLogDto> Items, int Total)> ListAuditLogsScopedAsync(
        string tenantId,
        string orgId,
        string? entityType,
        Guid? entityId,
        string? fieldKey,
        string? changeType,
        string? changedBy,
        DateTimeOffset? changedFrom,
        DateTimeOffset? changedTo,
        int page,
        int pageSize,
        CancellationToken ct = default);
}
