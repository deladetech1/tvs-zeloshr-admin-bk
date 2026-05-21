using Microsoft.EntityFrameworkCore;
using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Persistence.Entities;

namespace ZelosHR.Api.Persistence.Repositories;

public sealed class EmployeeCertificationRepository(ZelosHrDbContext db) : IEmployeeCertificationRepository
{
    public Task<bool> EmployeeExistsAsync(Guid employeeId, string tenantId, string orgId, CancellationToken ct = default) =>
        db.Employees.AsNoTracking()
            .AnyAsync(e => e.Id == employeeId && e.TenantId == tenantId && e.OrgId == orgId && !e.IsDeleted, ct);

    public async Task<IReadOnlyList<EmployeeCertificationEntity>> ListByEmployeeAsync(
        Guid employeeId, string tenantId, string orgId, CancellationToken ct = default) =>
        await db.EmployeeCertifications.AsNoTracking()
            .Where(x => x.EmployeeId == employeeId)
            .OrderBy(x => x.Name)
            .ToListAsync(ct);

    public Task<EmployeeCertificationEntity?> GetByIdAsync(
        Guid id, Guid employeeId, string tenantId, string orgId, CancellationToken ct = default) =>
        db.EmployeeCertifications
            .FirstOrDefaultAsync(x => x.Id == id && x.EmployeeId == employeeId, ct);

    public async Task<EmployeeCertificationEntity> AddAsync(EmployeeCertificationEntity entity, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        entity.CreatedAt = now;
        entity.UpdatedAt = now;
        db.EmployeeCertifications.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task UpdateAsync(EmployeeCertificationEntity entity, CancellationToken ct = default)
    {
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        db.EmployeeCertifications.Update(entity);
        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> DeleteAsync(
        Guid id, Guid employeeId, string tenantId, string orgId, CancellationToken ct = default)
    {
        if (!await EmployeeExistsAsync(employeeId, tenantId, orgId, ct))
            return false;

        var rows = await db.EmployeeCertifications
            .Where(x => x.Id == id && x.EmployeeId == employeeId)
            .ExecuteDeleteAsync(ct);
        return rows > 0;
    }
}
