using Microsoft.EntityFrameworkCore;
using ZelosHR.Api.Entities.Branches;
using ZelosHR.Api.Persistence.Entities;

namespace ZelosHR.Api.Persistence.Repositories;

public sealed class BranchRepository(ZelosHrDbContext db) : IBranchRepository
{
    private IQueryable<BranchEntity> Scoped(string tenantId, string orgId) =>
        db.Branches.AsNoTracking()
            .Where(b => b.TenantId == tenantId && b.OrgId == orgId);

    public async Task<IReadOnlyList<BranchListRow>> ListScopedAsync(
        string tenantId, string orgId, bool includeArchived, CancellationToken ct = default)
    {
        var query = Scoped(tenantId, orgId);
        if (!includeArchived)
            query = query.Where(b => !b.IsArchived);

        return await query
            .OrderBy(b => b.Name)
            .Select(b => new BranchListRow(
                b.Id,
                b.Name,
                db.Employees.Count(e =>
                    e.BranchId == b.Id
                    && e.TenantId == tenantId
                    && e.OrgId == orgId
                    && !e.IsDeleted),
                b.IsArchived))
            .ToListAsync(ct);
    }

    public async Task<Guid> CreateScopedAsync(
        string tenantId, string orgId, string name, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var entity = new BranchEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OrgId = orgId,
            Name = name.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.Branches.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity.Id;
    }

    public async Task<string?> UpdateNameScopedAsync(
        Guid id, string tenantId, string orgId, string name, CancellationToken ct = default)
    {
        var entity = await db.Branches.FirstOrDefaultAsync(
            b => b.Id == id && b.TenantId == tenantId && b.OrgId == orgId && !b.IsArchived, ct);
        if (entity is null)
            return null;

        entity.Name = name.Trim();
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return entity.Name;
    }

    public async Task<bool> ExistsActiveScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default) =>
        await db.Branches.AsNoTracking()
            .AnyAsync(b => b.Id == id && b.TenantId == tenantId && b.OrgId == orgId && !b.IsArchived, ct);

    public async Task<bool> ArchiveScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default)
    {
        var entity = await db.Branches.FirstOrDefaultAsync(
            b => b.Id == id && b.TenantId == tenantId && b.OrgId == orgId && !b.IsArchived, ct);
        if (entity is null)
            return false;

        entity.IsArchived = true;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return true;
    }
}
