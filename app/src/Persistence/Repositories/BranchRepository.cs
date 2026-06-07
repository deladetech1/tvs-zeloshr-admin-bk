using Microsoft.EntityFrameworkCore;
using ZelosHR.Api.Entities.Branches;
using ZelosHR.Api.Entities.OrgStructure;
using ZelosHR.Api.Persistence.Entities;

namespace ZelosHR.Api.Persistence.Repositories;

public sealed class BranchRepository(ZelosHrDbContext db) : IBranchRepository
{
    private IQueryable<BranchEntity> Scoped(string tenantId, string orgId) =>
        db.Branches.AsNoTracking()
            .Where(b => b.TenantId == tenantId && b.OrgId == orgId);

    private static BranchListRow ToRow(BranchEntity b, int employeeCount) =>
        new(b.Id, b.Name, b.Address, b.Country, b.Description, employeeCount, b.IsArchived);

    private int EmployeeCount(Guid branchId, string tenantId, string orgId) =>
        db.Employees.Count(e =>
            e.BranchId == branchId
            && e.TenantId == tenantId
            && e.OrgId == orgId
            && !e.IsDeleted);

    public async Task<IReadOnlyList<BranchListRow>> ListScopedAsync(
        string tenantId, string orgId, bool includeArchived, CancellationToken ct = default)
    {
        var query = Scoped(tenantId, orgId);
        if (!includeArchived)
            query = query.Where(b => !b.IsArchived);

        var branches = await query.OrderBy(b => b.Name).ToListAsync(ct);
        return branches
            .Select(b => ToRow(b, EmployeeCount(b.Id, tenantId, orgId)))
            .ToList();
    }

    public async Task<(IReadOnlyList<BranchListRow> Items, int Total)> ListPagedScopedAsync(
        string tenantId,
        string orgId,
        string? search,
        bool includeArchived,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = Scoped(tenantId, orgId);
        if (!includeArchived)
            query = query.Where(b => !b.IsArchived);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(b =>
                EF.Functions.ILike(b.Name, pattern)
                || (b.Address != null && EF.Functions.ILike(b.Address, pattern))
                || (b.Country != null && EF.Functions.ILike(b.Country, pattern))
                || (b.Description != null && EF.Functions.ILike(b.Description, pattern)));
        }

        var total = await query.CountAsync(ct);
        var branches = await query
            .OrderBy(b => b.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var items = branches
            .Select(b => ToRow(b, EmployeeCount(b.Id, tenantId, orgId)))
            .ToList();

        return (items, total);
    }

    public async Task<Guid> CreateScopedAsync(
        BranchWriteModel model,
        string tenantId,
        string orgId,
        CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var entity = new BranchEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OrgId = orgId,
            Name = model.Name.Trim(),
            Address = NullIfWhiteSpace(model.Address),
            Country = NullIfWhiteSpace(model.Country),
            Description = NullIfWhiteSpace(model.Description),
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.Branches.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity.Id;
    }

    public async Task<BranchListRow?> GetActiveScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default)
    {
        var entity = await db.Branches.AsNoTracking()
            .FirstOrDefaultAsync(
                b => b.Id == id && b.TenantId == tenantId && b.OrgId == orgId && !b.IsArchived, ct);
        return entity is null ? null : ToRow(entity, EmployeeCount(entity.Id, tenantId, orgId));
    }

    public async Task<BranchListRow?> UpdateScopedAsync(
        Guid id,
        string tenantId,
        string orgId,
        string? name,
        string? address,
        string? country,
        string? description,
        bool updateName,
        bool updateAddress,
        bool updateCountry,
        bool updateDescription,
        CancellationToken ct = default)
    {
        var entity = await db.Branches.FirstOrDefaultAsync(
            b => b.Id == id && b.TenantId == tenantId && b.OrgId == orgId && !b.IsArchived, ct);
        if (entity is null)
            return null;

        if (updateName)
            entity.Name = name!.Trim();
        if (updateAddress)
            entity.Address = NullIfWhiteSpace(address);
        if (updateCountry)
            entity.Country = NullIfWhiteSpace(country);
        if (updateDescription)
            entity.Description = NullIfWhiteSpace(description);

        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        return ToRow(entity, EmployeeCount(entity.Id, tenantId, orgId));
    }

    public async Task<bool> ExistsActiveScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default) =>
        await db.Branches.AsNoTracking()
            .AnyAsync(b => b.Id == id && b.TenantId == tenantId && b.OrgId == orgId && !b.IsArchived, ct);

    public async Task<OrgStructureDeleteResult> DeleteScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default)
    {
        var entity = await db.Branches.FirstOrDefaultAsync(
            b => b.Id == id && b.TenantId == tenantId && b.OrgId == orgId, ct);
        if (entity is null)
            return OrgStructureDeleteResult.NotFound;

        var hasEmployees = await db.Employees.AnyAsync(
            e => e.BranchId == id
                 && e.TenantId == tenantId
                 && e.OrgId == orgId
                 && !e.IsDeleted,
            ct);
        if (hasEmployees)
            return OrgStructureDeleteResult.InUseByEmployees;

        db.Branches.Remove(entity);
        await db.SaveChangesAsync(ct);
        return OrgStructureDeleteResult.Deleted;
    }

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
