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
        new(b.Id, b.Name, b.City, b.Region, b.CountryCode, employeeCount, b.IsArchived);

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
                || (b.City != null && EF.Functions.ILike(b.City, pattern))
                || (b.Region != null && EF.Functions.ILike(b.Region, pattern))
                || (b.CountryCode != null && EF.Functions.ILike(b.CountryCode, pattern)));
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
            City = NullIfWhiteSpace(model.City),
            Region = NullIfWhiteSpace(model.Region),
            CountryCode = OrgStructureValidation.NormalizeOptionalCountryCode(model.CountryCode),
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
        string? city,
        string? region,
        string? countryCode,
        bool updateName,
        bool updateCity,
        bool updateRegion,
        bool updateCountryCode,
        CancellationToken ct = default)
    {
        var entity = await db.Branches.FirstOrDefaultAsync(
            b => b.Id == id && b.TenantId == tenantId && b.OrgId == orgId && !b.IsArchived, ct);
        if (entity is null)
            return null;

        if (updateName)
            entity.Name = name!.Trim();
        if (updateCity)
            entity.City = NullIfWhiteSpace(city);
        if (updateRegion)
            entity.Region = NullIfWhiteSpace(region);
        if (updateCountryCode)
            entity.CountryCode = OrgStructureValidation.NormalizeOptionalCountryCode(countryCode);

        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        return ToRow(entity, EmployeeCount(entity.Id, tenantId, orgId));
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

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
