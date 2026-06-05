using Microsoft.EntityFrameworkCore;
using ZelosHR.Api.Entities.CustomFields;
using ZelosHR.Api.Persistence.Entities;

namespace ZelosHR.Api.Persistence.Repositories;

public sealed class CustomFieldDefinitionsRepository(ZelosHrDbContext db) : ICustomFieldDefinitionsRepository
{
    private IQueryable<CustomFieldDefinitionEntity> Scoped(string tenantId, string orgId) =>
        db.CustomFieldDefinitions.AsNoTracking()
            .Where(d => d.TenantId == tenantId && d.OrgId == orgId);

    public async Task<CustomFieldsSummaryDto> GetSummaryScopedAsync(
        string tenantId, string orgId, CancellationToken ct = default)
    {
        var query = db.CustomFieldDefinitions.Where(d => d.TenantId == tenantId && d.OrgId == orgId);
        return new CustomFieldsSummaryDto
        {
            TotalDefinitions = await query.CountAsync(ct),
            ActiveDefinitions = await query.CountAsync(d => d.IsActive && !d.IsDeleted, ct),
            DeletedDefinitions = await query.CountAsync(d => d.IsDeleted, ct),
        };
    }

    public async Task<(IReadOnlyList<CustomFieldDefinitionDto> Items, int Total)> ListScopedAsync(
        string tenantId,
        string orgId,
        CustomFieldListQuery query,
        CancellationToken ct = default)
    {
        var filtered = ApplyFilters(Scoped(tenantId, orgId), query);
        var total = await filtered.CountAsync(ct);
        var ordered = ApplySort(filtered, query.SortBy, query.SortOrder);
        var page = query.Page < 1 ? 1 : query.Page;
        var size = query.Size < 1 ? 20 : Math.Min(query.Size, 100);

        var rows = await ordered
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(ct);

        return (rows.Select(ToDto).ToList(), total);
    }

    public async Task<IReadOnlyList<CustomFieldDefinitionDto>> ListSchemaScopedAsync(
        string tenantId,
        string orgId,
        string entityType,
        CancellationToken ct = default)
    {
        var rows = await Scoped(tenantId, orgId)
            .Where(d => d.EntityType == entityType && d.IsActive && !d.IsDeleted)
            .OrderBy(d => d.SectionOrder)
            .ThenBy(d => d.DisplayOrder)
            .ThenBy(d => d.Label)
            .ToListAsync(ct);

        return rows.Select(ToDto).ToList();
    }

    public async Task<CustomFieldDefinitionDto?> GetByIdScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default)
    {
        var entity = await Scoped(tenantId, orgId).FirstOrDefaultAsync(d => d.Id == id, ct);
        return entity is null ? null : ToDto(entity);
    }

    public Task<bool> FieldKeyExistsScopedAsync(
        string tenantId,
        string orgId,
        string entityType,
        string fieldKey,
        Guid? excludeId,
        CancellationToken ct = default)
    {
        var query = db.CustomFieldDefinitions.Where(d =>
            d.TenantId == tenantId
            && d.OrgId == orgId
            && d.EntityType == entityType
            && d.FieldKey == fieldKey
            && !d.IsDeleted);

        if (excludeId.HasValue)
            query = query.Where(d => d.Id != excludeId.Value);

        return query.AnyAsync(ct);
    }

    public async Task<Guid> CreateScopedAsync(
        CreateCustomFieldDefinitionDto data,
        string tenantId,
        string orgId,
        string? createdBy,
        CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var entity = new CustomFieldDefinitionEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OrgId = orgId,
            EntityType = data.EntityType.Trim(),
            FieldKey = data.FieldKey.Trim(),
            Label = data.Label.Trim(),
            Description = data.Description?.Trim(),
            FieldType = data.FieldType.Trim(),
            IsRequired = data.IsRequired,
            IsSensitive = data.IsSensitive,
            IsFilterable = data.IsFilterable,
            IsSearchable = data.IsSearchable,
            DisplayOrder = data.DisplayOrder,
            SectionName = data.SectionName?.Trim(),
            SectionOrder = data.SectionOrder,
            Options = data.Options,
            ValidationRules = data.ValidationRules,
            DefaultValue = data.DefaultValue,
            Placeholder = data.Placeholder?.Trim(),
            IsActive = data.IsActive,
            IsDeleted = false,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = createdBy,
            UpdatedBy = createdBy,
        };

        db.CustomFieldDefinitions.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity.Id;
    }

    public async Task<CustomFieldDefinitionDto?> UpdateScopedAsync(
        Guid id,
        UpdateCustomFieldDefinitionDto data,
        string tenantId,
        string orgId,
        string? updatedBy,
        CancellationToken ct = default)
    {
        var entity = await db.CustomFieldDefinitions.FirstOrDefaultAsync(
            d => d.Id == id && d.TenantId == tenantId && d.OrgId == orgId && !d.IsDeleted, ct);
        if (entity is null)
            return null;

        var changed = false;
        if (!string.IsNullOrWhiteSpace(data.Label))
        {
            entity.Label = data.Label.Trim();
            changed = true;
        }
        if (data.Description is not null)
        {
            entity.Description = string.IsNullOrWhiteSpace(data.Description) ? null : data.Description.Trim();
            changed = true;
        }
        if (!string.IsNullOrWhiteSpace(data.FieldType))
        {
            entity.FieldType = data.FieldType.Trim();
            changed = true;
        }
        if (data.IsRequired.HasValue)
        {
            entity.IsRequired = data.IsRequired.Value;
            changed = true;
        }
        if (data.IsSensitive.HasValue)
        {
            entity.IsSensitive = data.IsSensitive.Value;
            changed = true;
        }
        if (data.IsFilterable.HasValue)
        {
            entity.IsFilterable = data.IsFilterable.Value;
            changed = true;
        }
        if (data.IsSearchable.HasValue)
        {
            entity.IsSearchable = data.IsSearchable.Value;
            changed = true;
        }
        if (data.DisplayOrder.HasValue)
        {
            entity.DisplayOrder = data.DisplayOrder.Value;
            changed = true;
        }
        if (data.SectionName is not null)
        {
            entity.SectionName = string.IsNullOrWhiteSpace(data.SectionName) ? null : data.SectionName.Trim();
            changed = true;
        }
        if (data.SectionOrder.HasValue)
        {
            entity.SectionOrder = data.SectionOrder.Value;
            changed = true;
        }
        if (data.Options is not null)
        {
            entity.Options = data.Options;
            changed = true;
        }
        if (data.ValidationRules is not null)
        {
            entity.ValidationRules = data.ValidationRules;
            changed = true;
        }
        if (data.DefaultValue is not null)
        {
            entity.DefaultValue = data.DefaultValue;
            changed = true;
        }
        if (data.Placeholder is not null)
        {
            entity.Placeholder = string.IsNullOrWhiteSpace(data.Placeholder) ? null : data.Placeholder.Trim();
            changed = true;
        }
        if (data.IsActive.HasValue)
        {
            entity.IsActive = data.IsActive.Value;
            changed = true;
        }

        if (!changed)
            return null;

        entity.UpdatedAt = DateTimeOffset.UtcNow;
        entity.UpdatedBy = updatedBy;
        await db.SaveChangesAsync(ct);
        return ToDto(entity);
    }

    public async Task<bool> SoftDeleteScopedAsync(
        Guid id, string tenantId, string orgId, string? updatedBy, CancellationToken ct = default)
    {
        var entity = await db.CustomFieldDefinitions.FirstOrDefaultAsync(
            d => d.Id == id && d.TenantId == tenantId && d.OrgId == orgId && !d.IsDeleted, ct);
        if (entity is null)
            return false;

        entity.IsDeleted = true;
        entity.IsActive = false;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        entity.UpdatedBy = updatedBy;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<int> ReorderScopedAsync(
        string tenantId,
        string orgId,
        IReadOnlyList<ReorderCustomFieldItemDto> items,
        string? updatedBy,
        CancellationToken ct = default)
    {
        if (items.Count == 0)
            return 0;

        var ids = items.Select(i => i.Id).ToList();
        var entities = await db.CustomFieldDefinitions
            .Where(d => d.TenantId == tenantId && d.OrgId == orgId && ids.Contains(d.Id) && !d.IsDeleted)
            .ToListAsync(ct);

        var updated = 0;
        foreach (var item in items)
        {
            var entity = entities.FirstOrDefault(e => e.Id == item.Id);
            if (entity is null)
                continue;

            entity.DisplayOrder = item.DisplayOrder;
            if (item.SectionOrder.HasValue)
                entity.SectionOrder = item.SectionOrder.Value;
            entity.UpdatedAt = DateTimeOffset.UtcNow;
            entity.UpdatedBy = updatedBy;
            updated++;
        }

        if (updated > 0)
            await db.SaveChangesAsync(ct);

        return updated;
    }

    public async Task<(IReadOnlyList<CustomFieldAuditLogDto> Items, int Total)> ListAuditLogsScopedAsync(
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
        CancellationToken ct = default)
    {
        var query = db.CustomFieldAuditLogs.AsNoTracking()
            .Where(a => a.TenantId == tenantId && a.OrgId == orgId);

        if (!string.IsNullOrWhiteSpace(entityType))
            query = query.Where(a => a.EntityType == entityType.Trim());

        if (entityId.HasValue)
            query = query.Where(a => a.EntityId == entityId.Value);

        if (!string.IsNullOrWhiteSpace(fieldKey))
            query = query.Where(a => a.FieldKey == fieldKey.Trim());

        if (!string.IsNullOrWhiteSpace(changeType))
            query = query.Where(a => a.ChangeType == changeType.Trim());

        if (!string.IsNullOrWhiteSpace(changedBy))
            query = query.Where(a => a.ChangedBy == changedBy.Trim());

        if (changedFrom.HasValue)
            query = query.Where(a => a.ChangedAt >= changedFrom.Value);

        if (changedTo.HasValue)
            query = query.Where(a => a.ChangedAt <= changedTo.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(a => a.ChangedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new CustomFieldAuditLogDto
            {
                Id = a.Id.ToString(),
                EntityType = a.EntityType,
                EntityId = a.EntityId.ToString(),
                FieldKey = a.FieldKey,
                OldValue = a.OldValue,
                NewValue = a.NewValue,
                ChangedById = a.ChangedBy,
                ChangedBy = a.ChangedBy,
                ChangedAt = a.ChangedAt,
                ChangeType = a.ChangeType,
            })
            .ToListAsync(ct);

        return (items, total);
    }

    private static IQueryable<CustomFieldDefinitionEntity> ApplyFilters(
        IQueryable<CustomFieldDefinitionEntity> query,
        CustomFieldListQuery filters)
    {
        if (!filters.IncludeDeleted)
            query = query.Where(d => !d.IsDeleted);

        if (!string.IsNullOrWhiteSpace(filters.EntityType))
            query = query.Where(d => d.EntityType == filters.EntityType.Trim());

        if (!string.IsNullOrWhiteSpace(filters.FieldKey))
        {
            var key = filters.FieldKey.Trim();
            query = query.Where(d => EF.Functions.ILike(d.FieldKey, $"%{key}%"));
        }

        if (!string.IsNullOrWhiteSpace(filters.Label))
        {
            var label = filters.Label.Trim();
            query = query.Where(d => EF.Functions.ILike(d.Label, $"%{label}%"));
        }

        if (!string.IsNullOrWhiteSpace(filters.FieldType))
            query = query.Where(d => d.FieldType == filters.FieldType.Trim());

        if (filters.IsRequired.HasValue)
            query = query.Where(d => d.IsRequired == filters.IsRequired.Value);

        if (filters.IsSensitive.HasValue)
            query = query.Where(d => d.IsSensitive == filters.IsSensitive.Value);

        if (filters.IsFilterable.HasValue)
            query = query.Where(d => d.IsFilterable == filters.IsFilterable.Value);

        if (filters.IsSearchable.HasValue)
            query = query.Where(d => d.IsSearchable == filters.IsSearchable.Value);

        if (filters.IsActive.HasValue)
            query = query.Where(d => d.IsActive == filters.IsActive.Value);

        if (!string.IsNullOrWhiteSpace(filters.SectionName))
        {
            var section = filters.SectionName.Trim();
            query = query.Where(d => d.SectionName != null && EF.Functions.ILike(d.SectionName, $"%{section}%"));
        }

        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            var term = filters.Search.Trim();
            if (term.Length >= 2)
            {
                var pattern = $"%{term}%";
                query = query.Where(d =>
                    EF.Functions.ILike(d.Label, pattern)
                    || EF.Functions.ILike(d.FieldKey, pattern)
                    || (d.Description != null && EF.Functions.ILike(d.Description, pattern)));
            }
        }

        return query;
    }

    private static IOrderedQueryable<CustomFieldDefinitionEntity> ApplySort(
        IQueryable<CustomFieldDefinitionEntity> query,
        string? sortBy,
        string? sortOrder)
    {
        var desc = string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase);

        return (sortBy?.Trim().ToLowerInvariant()) switch
        {
            "fieldkey" => desc ? query.OrderByDescending(d => d.FieldKey) : query.OrderBy(d => d.FieldKey),
            "entitytype" => desc ? query.OrderByDescending(d => d.EntityType) : query.OrderBy(d => d.EntityType),
            "fieldtype" => desc ? query.OrderByDescending(d => d.FieldType) : query.OrderBy(d => d.FieldType),
            "displayorder" => desc ? query.OrderByDescending(d => d.DisplayOrder) : query.OrderBy(d => d.DisplayOrder),
            "sectionorder" => desc
                ? query.OrderByDescending(d => d.SectionOrder).ThenByDescending(d => d.DisplayOrder)
                : query.OrderBy(d => d.SectionOrder).ThenBy(d => d.DisplayOrder),
            "updatedat" => desc ? query.OrderByDescending(d => d.UpdatedAt) : query.OrderBy(d => d.UpdatedAt),
            "createdat" => desc ? query.OrderByDescending(d => d.CreatedAt) : query.OrderBy(d => d.CreatedAt),
            _ => desc ? query.OrderByDescending(d => d.Label) : query.OrderBy(d => d.Label),
        };
    }

    private static CustomFieldDefinitionDto ToDto(CustomFieldDefinitionEntity d) => new()
    {
        Id = d.Id.ToString(),
        EntityType = d.EntityType,
        FieldKey = d.FieldKey,
        Label = d.Label,
        Description = d.Description,
        FieldType = d.FieldType,
        IsRequired = d.IsRequired,
        IsSensitive = d.IsSensitive,
        IsFilterable = d.IsFilterable,
        IsSearchable = d.IsSearchable,
        DisplayOrder = d.DisplayOrder,
        SectionName = d.SectionName,
        SectionOrder = d.SectionOrder,
        Options = d.Options,
        ValidationRules = d.ValidationRules,
        DefaultValue = d.DefaultValue,
        Placeholder = d.Placeholder,
        IsActive = d.IsActive,
        IsDeleted = d.IsDeleted,
        CreatedAt = d.CreatedAt,
        UpdatedAt = d.UpdatedAt,
        CreatedById = d.CreatedBy,
        UpdatedById = d.UpdatedBy,
    };
}
