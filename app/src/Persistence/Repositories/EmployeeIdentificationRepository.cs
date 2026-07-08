using Microsoft.EntityFrameworkCore;
using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Persistence.Entities;

namespace ZelosHR.Api.Persistence.Repositories;

public sealed class EmployeeIdentificationRepository(ZelosHrDbContext db) : IEmployeeIdentificationRepository
{
    public Task<bool> EmployeeExistsAsync(Guid employeeId, string tenantId, string orgId, CancellationToken ct = default) =>
        db.Employees.AsNoTracking()
            .AnyAsync(e => e.Id == employeeId && e.TenantId == tenantId && e.OrgId == orgId && !e.IsDeleted, ct);

    public async Task<IReadOnlyList<EmployeeIdentificationEntity>> ListByEmployeeAsync(
        Guid employeeId, string tenantId, string orgId, CancellationToken ct = default)
    {
        _ = tenantId;
        _ = orgId;
        return await db.EmployeeIdentifications.AsNoTracking()
            .Include(x => x.IdCardType)
            .Where(x => x.EmployeeId == employeeId)
            .OrderBy(x => x.IdCardType!.Name)
            .ToListAsync(ct);
    }

    public Task<EmployeeIdentificationEntity?> GetByIdAsync(
        Guid id, Guid employeeId, string tenantId, string orgId, CancellationToken ct = default)
    {
        _ = tenantId;
        _ = orgId;
        return db.EmployeeIdentifications
            .Include(x => x.IdCardType)
            .FirstOrDefaultAsync(x => x.Id == id && x.EmployeeId == employeeId, ct);
    }

    public Task<bool> ExistsForEmployeeAndTypeAsync(
        Guid employeeId, Guid idCardTypeId, Guid? excludeId, CancellationToken ct = default) =>
        db.EmployeeIdentifications.AsNoTracking()
            .AnyAsync(x =>
                x.EmployeeId == employeeId
                && x.IdCardTypeId == idCardTypeId
                && (excludeId == null || x.Id != excludeId.Value),
                ct);

    public async Task<EmployeeIdentificationEntity> AddAsync(EmployeeIdentificationEntity entity, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        entity.CreatedAt = now;
        entity.UpdatedAt = now;
        db.EmployeeIdentifications.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task UpdateAsync(EmployeeIdentificationEntity entity, CancellationToken ct = default)
    {
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        db.EmployeeIdentifications.Update(entity);
        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> DeleteAsync(
        Guid id, Guid employeeId, string tenantId, string orgId, CancellationToken ct = default)
    {
        if (!await EmployeeExistsAsync(employeeId, tenantId, orgId, ct))
            return false;

        var rows = await db.EmployeeIdentifications
            .Where(x => x.Id == id && x.EmployeeId == employeeId)
            .ExecuteDeleteAsync(ct);
        return rows > 0;
    }
}
