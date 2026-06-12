using Microsoft.EntityFrameworkCore;
using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Persistence.Entities;

namespace ZelosHR.Api.Persistence.Repositories;

public sealed class EmployeeRepository(ZelosHrDbContext db) : IEmployeeRepository
{
    private IQueryable<EmployeeEntity> Scoped(string tenantId, string orgId) =>
        db.Employees.AsNoTracking()
            .Where(e => e.TenantId == tenantId && e.OrgId == orgId && !e.IsDeleted);

    public async Task<EmployeeEntity?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<EmployeeEntity?> GetByIdScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default) =>
        await db.Employees.AsNoTracking()
            .Include(e => e.Department)
            .Include(e => e.Branch)
            .Include(e => e.Manager)
            .FirstOrDefaultAsync(
                e => e.Id == id && e.TenantId == tenantId && e.OrgId == orgId && !e.IsDeleted,
                ct);

    public async Task<EmployeeEntity?> GetByPlatformUserIdScopedAsync(
        string platformUserId, string tenantId, string orgId, CancellationToken ct = default) =>
        await db.Employees.AsNoTracking()
            .FirstOrDefaultAsync(
                e => e.UserId == platformUserId
                     && e.TenantId == tenantId
                     && e.OrgId == orgId
                     && !e.IsDeleted,
                ct);

    public async Task<EmployeeEntity?> GetByIdScopedForUpdateAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default) =>
        await db.Employees
            .FirstOrDefaultAsync(
                e => e.Id == id && e.TenantId == tenantId && e.OrgId == orgId && !e.IsDeleted,
                ct);

    public async Task<EmployeeEntity?> GetByGhanaCardAsync(
        string ghanaCard, string tenantId, CancellationToken ct = default) =>
        await db.Employees.AsNoTracking()
            .FirstOrDefaultAsync(e => e.TenantId == tenantId && e.GhanaCardNumber == ghanaCard, ct);

    public async Task<bool> ExistsByGhanaCardAsync(
        string ghanaCard, string tenantId, Guid? excludeId = null, CancellationToken ct = default)
    {
        var query = db.Employees.AsNoTracking()
            .Where(e => e.TenantId == tenantId && e.GhanaCardNumber == ghanaCard && !e.IsDeleted);
        if (excludeId.HasValue)
            query = query.Where(e => e.Id != excludeId.Value);
        return await query.AnyAsync(ct);
    }

    public async Task<IReadOnlyList<EmployeeEntity>> GetAllAsync(CancellationToken ct = default) =>
        await db.Employees.AsNoTracking().Where(e => !e.IsDeleted).ToListAsync(ct);

    public async Task<(IReadOnlyList<EmployeeEntity> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.Employees.AsNoTracking().Where(e => !e.IsDeleted);
        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
        return (items, total);
    }

    public async Task<(IReadOnlyList<EmployeeEntity> Items, int TotalCount)> GetPagedScopedAsync(
        string tenantId, string orgId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = Scoped(tenantId, orgId);
        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
        return (items, total);
    }

    public async Task<(IReadOnlyList<EmployeeEntity> Items, int TotalCount)> SearchScopedAsync(
        string? nameQuery,
        Guid? departmentId,
        string? status,
        string tenantId,
        string orgId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = Scoped(tenantId, orgId);

        if (!string.IsNullOrWhiteSpace(nameQuery))
        {
            var pattern = $"%{nameQuery.Trim()}%";
            query = query.Where(e =>
                EF.Functions.ILike(e.FirstName, pattern)
                || EF.Functions.ILike(e.LastName, pattern)
                || EF.Functions.ILike(e.EmployeeCode, pattern));
        }

        if (departmentId.HasValue)
            query = query.Where(e => e.DepartmentId == departmentId);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(e => e.EmploymentStatus == status);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(e => e.LastName)
            .ThenBy(e => e.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<(IReadOnlyList<EmployeeEntity> Items, int TotalCount)> ListScopedAsync(
        EmployeeListQuery listQuery,
        string tenantId,
        string orgId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var directoryQuery = new EmployeeDirectoryQuery
        {
            Search = listQuery.Search,
            DepartmentId = listQuery.DepartmentId,
            BranchId = listQuery.BranchId,
            EmploymentType = listQuery.EmploymentType,
            WorkLocation = listQuery.WorkLocation,
            Status = listQuery.EmploymentStatus,
            StatusFilter = listQuery.Status,
            IncludeInactive = listQuery.IncludeInactive,
            StartDate = listQuery.StartDate,
            EndDate = listQuery.EndDate,
            SortBy = listQuery.SortBy,
            SortOrder = listQuery.SortOrder,
        };

        var query = db.Employees.AsNoTracking()
            .Include(e => e.Department)
            .Include(e => e.Branch)
            .Where(e => e.TenantId == tenantId && e.OrgId == orgId && !e.IsDeleted);

        query = EmployeeDirectoryQueryBuilder.ApplyFilters(
            query, directoryQuery, db.CpUsers, tenantId);

        var total = await query.CountAsync(ct);
        var ordered = EmployeeDirectoryQueryBuilder.ApplySort(query, listQuery.SortBy, listQuery.SortOrder);
        var items = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<IReadOnlyList<EmployeeEntity>> ExportListScopedAsync(
        EmployeeExportQuery exportQuery,
        string tenantId,
        string orgId,
        CancellationToken ct = default)
    {
        var directoryQuery = new EmployeeDirectoryQuery
        {
            Search = exportQuery.Search,
            DepartmentId = exportQuery.DepartmentId,
            BranchId = exportQuery.BranchId,
            EmploymentType = exportQuery.EmploymentType,
            WorkLocation = exportQuery.WorkLocation,
            Status = exportQuery.EmploymentStatus,
            StatusFilter = exportQuery.Status,
            IncludeInactive = exportQuery.IncludeInactive,
            StartDate = exportQuery.StartDate,
            EndDate = exportQuery.EndDate,
        };

        var query = db.Employees.AsNoTracking()
            .Include(e => e.Department)
            .Include(e => e.Branch)
            .Where(e => e.TenantId == tenantId && e.OrgId == orgId && !e.IsDeleted);

        query = EmployeeDirectoryQueryBuilder.ApplyFilters(
            query, directoryQuery, db.CpUsers, tenantId);

        return await query
            .OrderBy(e => e.LastName)
            .ThenBy(e => e.FirstName)
            .ToListAsync(ct);
    }

    public async Task<EmployeeEntity> AddAsync(EmployeeEntity entity, CancellationToken ct = default)
    {
        EmployeeEntityInsertDefaults.EnsureRequiredColumns(entity);
        entity.CreatedAt = DateTimeOffset.UtcNow;
        entity.UpdatedAt = entity.CreatedAt;
        db.Employees.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task UpdateAsync(EmployeeEntity entity, CancellationToken ct = default)
    {
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        db.Employees.Update(entity);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await db.Employees.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (entity is null)
            return;
        db.Employees.Remove(entity);
        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> ExistsActiveScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default) =>
        await db.Employees.AsNoTracking()
            .AnyAsync(
                e => e.Id == id && e.TenantId == tenantId && e.OrgId == orgId && !e.IsDeleted,
                ct);

    public async Task<bool> SoftDeleteScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default)
    {
        var affected = await db.Employees
            .Where(e => e.Id == id && e.TenantId == tenantId && e.OrgId == orgId && !e.IsDeleted)
            .ExecuteUpdateAsync(
                s => s
                    .SetProperty(e => e.IsDeleted, true)
                    .SetProperty(e => e.EmploymentStatus, EmploymentStatusValues.Inactive)
                    .SetProperty(e => e.UpdatedAt, DateTimeOffset.UtcNow),
                ct);
        return affected > 0;
    }

    public async Task<long> GetNextEmployeeSequenceAsync(
        string tenantId, string orgId, CancellationToken ct = default)
    {
        var codes = await db.Employees.AsNoTracking()
            .Where(e => e.TenantId == tenantId)
            .Select(e => e.EmployeeCode)
            .ToListAsync(ct);

        long max = 0;
        foreach (var code in codes)
        {
            var digits = new string(code.Where(char.IsDigit).ToArray());
            if (long.TryParse(digits, out var n) && n > max)
                max = n;
        }

        return max + 1;
    }
}
