using Microsoft.EntityFrameworkCore;
using ZelosHR.Api.Entities.EmploymentTypes;
using ZelosHR.Api.Persistence.Entities;

namespace ZelosHR.Api.Persistence.Repositories;

public sealed class EmploymentTypeRepository(ZelosHrDbContext db) : IEmploymentTypeRepository
{
    public async Task EnsureSystemDefaultsScopedAsync(
        string tenantId, string orgId, CancellationToken ct = default)
    {
        var existingNames = await db.EmploymentTypes
            .Where(t => t.TenantId == tenantId && t.OrgId == orgId && t.IsSystemDefault)
            .Select(t => t.Name)
            .ToListAsync(ct);

        var existingSet = new HashSet<string>(existingNames, StringComparer.OrdinalIgnoreCase);
        var now = DateTimeOffset.UtcNow;
        var added = false;

        foreach (var (name, description) in EmploymentTypeDefaults.SystemTypes)
        {
            if (existingSet.Contains(name))
                continue;

            db.EmploymentTypes.Add(new EmploymentTypeEntity
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

    public async Task BackfillEmployeeTypeIdsScopedAsync(
        string tenantId, string orgId, CancellationToken ct = default)
    {
        var types = await db.EmploymentTypes.AsNoTracking()
            .Where(t => t.TenantId == tenantId && t.OrgId == orgId)
            .Select(t => new { t.Id, t.Name })
            .ToListAsync(ct);

        if (types.Count == 0)
            return;

        var byName = types.ToDictionary(t => t.Name, t => t.Id, StringComparer.OrdinalIgnoreCase);
        var employees = await db.Employees
            .Where(e => e.TenantId == tenantId
                        && e.OrgId == orgId
                        && !e.IsDeleted
                        && e.EmploymentTypeId == null
                        && e.EmploymentType != null
                        && e.EmploymentType != "")
            .ToListAsync(ct);

        if (employees.Count == 0)
            return;

        foreach (var employee in employees)
        {
            if (byName.TryGetValue(employee.EmploymentType!.Trim(), out var typeId))
                employee.EmploymentTypeId = typeId;
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task<(IReadOnlyList<EmploymentTypeListItemDto> Items, int Total)> ListScopedAsync(
        string tenantId,
        string orgId,
        EmploymentTypeListQuery query,
        int page,
        int size,
        CancellationToken ct = default)
    {
        var baseQuery = db.EmploymentTypes.AsNoTracking()
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

        // Load counts separately — joining GroupBy(Guid?) to types triggers EF/Npgsql
        // "Nullable object must have a value" when legacy rows have employment_type_id null.
        var employeeCountByTypeId = await db.Employees.AsNoTracking()
            .Where(e => e.TenantId == tenantId
                        && e.OrgId == orgId
                        && !e.IsDeleted
                        && e.EmploymentTypeId != null)
            .GroupBy(e => e.EmploymentTypeId!.Value)
            .Select(g => new { TypeId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TypeId, x => x.Count, ct);

        var sortBy = query.SortBy.Trim().ToLowerInvariant();
        var desc = string.Equals(query.SortOrder, "desc", StringComparison.OrdinalIgnoreCase);

        IReadOnlyList<EmploymentTypeListItemDto> items;
        if (sortBy is "employees" or "employee_count")
        {
            var allTypes = await baseQuery.ToListAsync(ct);
            var ordered = desc
                ? allTypes.OrderByDescending(t => employeeCountByTypeId.GetValueOrDefault(t.Id, 0)).ThenBy(t => t.Name)
                : allTypes.OrderBy(t => employeeCountByTypeId.GetValueOrDefault(t.Id, 0)).ThenBy(t => t.Name);

            items = ordered
                .Skip((page - 1) * size)
                .Take(size)
                .Select(t => ToDto(t, employeeCountByTypeId.GetValueOrDefault(t.Id, 0)))
                .ToList();
        }
        else
        {
            var pageTypes = await ApplyTypeSort(baseQuery, sortBy, desc)
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync(ct);

            items = pageTypes
                .Select(t => ToDto(t, employeeCountByTypeId.GetValueOrDefault(t.Id, 0)))
                .ToList();
        }

        return (items, total);
    }

    private static IQueryable<EmploymentTypeEntity> ApplyTypeSort(
        IQueryable<EmploymentTypeEntity> query,
        string sortBy,
        bool desc) =>
        sortBy switch
        {
            "type" => desc
                ? query.OrderByDescending(t => t.IsSystemDefault).ThenBy(t => t.Name)
                : query.OrderBy(t => t.IsSystemDefault).ThenBy(t => t.Name),
            "status" or "is_active" => desc
                ? query.OrderByDescending(t => t.IsActive).ThenBy(t => t.Name)
                : query.OrderBy(t => t.IsActive).ThenBy(t => t.Name),
            "created_at" => desc
                ? query.OrderByDescending(t => t.CreatedAt)
                : query.OrderBy(t => t.CreatedAt),
            _ => desc
                ? query.OrderByDescending(t => t.Name)
                : query.OrderBy(t => t.Name),
        };

    public async Task<EmploymentTypeListItemDto?> GetByIdScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default)
    {
        var entity = await GetEntityByIdScopedAsync(id, tenantId, orgId, ct);
        if (entity is null)
            return null;

        var count = await CountEmployeesUsingTypeScopedAsync(id, tenantId, orgId, ct);
        return ToDto(entity, count);
    }

    public Task<EmploymentTypeEntity?> GetEntityByIdScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default) =>
        db.EmploymentTypes.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id && t.TenantId == tenantId && t.OrgId == orgId, ct);

    public async Task<bool> NameExistsScopedAsync(
        string tenantId, string orgId, string name, Guid? excludeId, CancellationToken ct = default)
    {
        var query = db.EmploymentTypes.AsNoTracking()
            .Where(t => t.TenantId == tenantId && t.OrgId == orgId && t.Name == name);
        if (excludeId.HasValue)
            query = query.Where(t => t.Id != excludeId.Value);
        return await query.AnyAsync(ct);
    }

    public async Task<Guid> CreateScopedAsync(
        string tenantId,
        string orgId,
        CreateEmploymentTypeDto data,
        string? actorUserId,
        CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var entity = new EmploymentTypeEntity
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
        db.EmploymentTypes.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity.Id;
    }

    public async Task<EmploymentTypeListItemDto?> UpdateScopedAsync(
        Guid id,
        string tenantId,
        string orgId,
        UpdateEmploymentTypeDto data,
        string? actorUserId,
        CancellationToken ct = default)
    {
        var entity = await db.EmploymentTypes
            .FirstOrDefaultAsync(t => t.Id == id && t.TenantId == tenantId && t.OrgId == orgId, ct);
        if (entity is null)
            return null;

        if (!string.IsNullOrWhiteSpace(data.Name))
        {
            var trimmed = data.Name.Trim();
            if (entity.IsSystemDefault && !string.Equals(trimmed, entity.Name, StringComparison.Ordinal))
                throw new InvalidOperationException("System default employment types cannot be renamed.");

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

        var count = await CountEmployeesUsingTypeScopedAsync(id, tenantId, orgId, ct);
        return ToDto(entity, count);
    }

    public async Task<(bool Found, bool InUse)> DeleteScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default)
    {
        var entity = await db.EmploymentTypes
            .FirstOrDefaultAsync(t => t.Id == id && t.TenantId == tenantId && t.OrgId == orgId, ct);
        if (entity is null)
            return (false, false);

        if (entity.IsSystemDefault)
            throw new InvalidOperationException("System default employment types cannot be deleted.");

        var inUse = await CountEmployeesUsingTypeScopedAsync(id, tenantId, orgId, ct) > 0;
        if (inUse)
            return (true, true);

        db.EmploymentTypes.Remove(entity);
        await db.SaveChangesAsync(ct);
        return (true, false);
    }

    public Task<int> CountEmployeesUsingTypeScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default) =>
        db.Employees.AsNoTracking()
            .CountAsync(
                e => e.TenantId == tenantId
                     && e.OrgId == orgId
                     && !e.IsDeleted
                     && e.EmploymentTypeId == id,
                ct);

    private static EmploymentTypeListItemDto ToDto(EmploymentTypeEntity entity, int employeeCount) =>
        new()
        {
            EmploymentTypeId = entity.Id.ToString(),
            Name = entity.Name,
            Description = entity.Description,
            Type = entity.IsSystemDefault ? EmploymentTypeKind.Default : EmploymentTypeKind.Custom,
            IsSystemDefault = entity.IsSystemDefault,
            EmployeeCount = employeeCount,
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            CreatedById = entity.CreatedBy,
            UpdatedById = entity.UpdatedBy,
        };
}
