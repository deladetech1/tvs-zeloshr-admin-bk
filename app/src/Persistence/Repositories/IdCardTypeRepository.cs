using Microsoft.EntityFrameworkCore;
using ZelosHR.Api.Entities.IdCardTypes;
using ZelosHR.Api.Persistence.Entities;

namespace ZelosHR.Api.Persistence.Repositories;

public sealed class IdCardTypeRepository(ZelosHrDbContext db) : IIdCardTypeRepository
{
    public async Task EnsureSystemDefaultsScopedAsync(
        string tenantId, string orgId, CancellationToken ct = default)
    {
        var existingNames = await db.IdCardTypes
            .Where(t => t.TenantId == tenantId && t.OrgId == orgId && t.IsSystemDefault)
            .Select(t => t.Name)
            .ToListAsync(ct);

        var existingSet = new HashSet<string>(existingNames, StringComparer.OrdinalIgnoreCase);
        var now = DateTimeOffset.UtcNow;
        var added = false;

        foreach (var (name, description) in IdCardTypeDefaults.SystemTypes)
        {
            if (existingSet.Contains(name))
                continue;

            db.IdCardTypes.Add(new IdCardTypeEntity
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                OrgId = orgId,
                Name = name,
                Description = description,
                IsSystemDefault = true,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now,
            });
            added = true;
        }

        if (added)
            await db.SaveChangesAsync(ct);
    }

    public async Task<(IReadOnlyList<IdCardTypeListItemDto> Items, int Total)> ListScopedAsync(
        string tenantId,
        string orgId,
        IdCardTypeListQuery query,
        int page,
        int size,
        CancellationToken ct = default)
    {
        var baseQuery = db.IdCardTypes.AsNoTracking()
            .Where(t => t.TenantId == tenantId && t.OrgId == orgId);

        if (query.IsActive is true)
            baseQuery = baseQuery.Where(t => t.IsActive);
        else if (query.IsActive is false)
            baseQuery = baseQuery.Where(t => !t.IsActive);

        if (!string.IsNullOrWhiteSpace(query.Search) && query.Search.Trim().Length >= 2)
        {
            var term = $"%{query.Search.Trim()}%";
            baseQuery = baseQuery.Where(t =>
                EF.Functions.ILike(t.Name, term)
                || (t.Description != null && EF.Functions.ILike(t.Description, term)));
        }

        var total = await baseQuery.CountAsync(ct);

        var sortBy = query.SortBy.Trim().ToLowerInvariant();
        var desc = string.Equals(query.SortOrder, "desc", StringComparison.OrdinalIgnoreCase);

        baseQuery = sortBy switch
        {
            "type" => desc
                ? baseQuery.OrderByDescending(x => x.IsSystemDefault).ThenBy(x => x.Name)
                : baseQuery.OrderBy(x => x.IsSystemDefault).ThenBy(x => x.Name),
            "status" or "is_active" => desc
                ? baseQuery.OrderByDescending(x => x.IsActive).ThenBy(x => x.Name)
                : baseQuery.OrderBy(x => x.IsActive).ThenBy(x => x.Name),
            "created_at" => desc
                ? baseQuery.OrderByDescending(x => x.CreatedAt)
                : baseQuery.OrderBy(x => x.CreatedAt),
            _ => desc
                ? baseQuery.OrderByDescending(x => x.Name)
                : baseQuery.OrderBy(x => x.Name),
        };

        var rows = await baseQuery
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(ct);

        var items = rows.Select(ToDto).ToList();
        return (items, total);
    }

    public async Task<IdCardTypeListItemDto?> GetByIdScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default)
    {
        var entity = await GetEntityByIdScopedAsync(id, tenantId, orgId, ct);
        return entity is null ? null : ToDto(entity);
    }

    public Task<IdCardTypeEntity?> GetEntityByIdScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default) =>
        db.IdCardTypes.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id && t.TenantId == tenantId && t.OrgId == orgId, ct);

    public async Task<bool> NameExistsScopedAsync(
        string tenantId, string orgId, string name, Guid? excludeId, CancellationToken ct = default)
    {
        var query = db.IdCardTypes.AsNoTracking()
            .Where(t => t.TenantId == tenantId && t.OrgId == orgId && t.Name == name);
        if (excludeId.HasValue)
            query = query.Where(t => t.Id != excludeId.Value);
        return await query.AnyAsync(ct);
    }

    public async Task<Guid> CreateScopedAsync(
        string tenantId,
        string orgId,
        CreateIdCardTypeDto data,
        string? actorUserId,
        CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var entity = new IdCardTypeEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OrgId = orgId,
            Name = data.Name!.Trim(),
            Description = string.IsNullOrWhiteSpace(data.Description) ? null : data.Description.Trim(),
            IsSystemDefault = false,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = actorUserId,
            UpdatedBy = actorUserId,
        };
        db.IdCardTypes.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity.Id;
    }

    public async Task<IdCardTypeListItemDto?> UpdateScopedAsync(
        Guid id,
        string tenantId,
        string orgId,
        UpdateIdCardTypeDto data,
        string? actorUserId,
        CancellationToken ct = default)
    {
        var entity = await db.IdCardTypes
            .FirstOrDefaultAsync(t => t.Id == id && t.TenantId == tenantId && t.OrgId == orgId, ct);
        if (entity is null)
            return null;

        if (!string.IsNullOrWhiteSpace(data.Name))
        {
            var trimmed = data.Name.Trim();
            if (entity.IsSystemDefault && !string.Equals(trimmed, entity.Name, StringComparison.Ordinal))
                throw new InvalidOperationException("System default ID card types cannot be renamed.");

            entity.Name = trimmed;
        }

        if (data.Description is not null)
        {
            entity.Description = string.IsNullOrWhiteSpace(data.Description)
                ? null
                : data.Description.Trim();
        }

        if (data.IsActive.HasValue)
            entity.IsActive = data.IsActive.Value;

        entity.UpdatedAt = DateTimeOffset.UtcNow;
        entity.UpdatedBy = actorUserId;
        await db.SaveChangesAsync(ct);

        return ToDto(entity);
    }

    public async Task<(bool Found, bool InUse)> DeleteScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default)
    {
        var entity = await db.IdCardTypes
            .FirstOrDefaultAsync(t => t.Id == id && t.TenantId == tenantId && t.OrgId == orgId, ct);
        if (entity is null)
            return (false, false);

        if (entity.IsSystemDefault)
            throw new InvalidOperationException("System default ID card types cannot be deleted.");

        db.IdCardTypes.Remove(entity);
        await db.SaveChangesAsync(ct);
        return (true, false);
    }

    private static IdCardTypeListItemDto ToDto(IdCardTypeEntity entity) =>
        new()
        {
            IdCardTypeId = entity.Id.ToString(),
            Name = entity.Name,
            Description = entity.Description,
            Type = entity.IsSystemDefault ? IdCardTypeKind.Default : IdCardTypeKind.Custom,
            IsSystemDefault = entity.IsSystemDefault,
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            CreatedById = entity.CreatedBy,
            UpdatedById = entity.UpdatedBy,
        };
}
