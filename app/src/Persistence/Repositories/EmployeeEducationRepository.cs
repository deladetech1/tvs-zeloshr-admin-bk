using Microsoft.EntityFrameworkCore;
using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Persistence.Entities;

namespace ZelosHR.Api.Persistence.Repositories;

public sealed class EmployeeEducationRepository(ZelosHrDbContext db) : IEmployeeEducationRepository
{
    public Task<bool> EmployeeExistsAsync(Guid employeeId, string tenantId, string orgId, CancellationToken ct = default) =>
        db.Employees.AsNoTracking()
            .AnyAsync(e => e.Id == employeeId && e.TenantId == tenantId && e.OrgId == orgId && !e.IsDeleted, ct);

    public async Task<IReadOnlyList<EmployeeEducationEntity>> ListByEmployeeAsync(
        Guid employeeId, string tenantId, string orgId, CancellationToken ct = default) =>
        await db.EmployeeEducations.AsNoTracking()
            .Where(x => x.EmployeeId == employeeId)
            .OrderByDescending(x => x.StartYear)
            .ToListAsync(ct);

    public Task<EmployeeEducationEntity?> GetByIdAsync(
        Guid id, Guid employeeId, string tenantId, string orgId, CancellationToken ct = default) =>
        db.EmployeeEducations
            .FirstOrDefaultAsync(x => x.Id == id && x.EmployeeId == employeeId, ct);

    public async Task<EmployeeEducationEntity> AddAsync(EmployeeEducationEntity entity, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        entity.CreatedAt = now;
        entity.UpdatedAt = now;
        db.EmployeeEducations.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task UpdateAsync(EmployeeEducationEntity entity, CancellationToken ct = default)
    {
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        db.EmployeeEducations.Update(entity);
        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> DeleteAsync(
        Guid id, Guid employeeId, string tenantId, string orgId, CancellationToken ct = default)
    {
        if (!await EmployeeExistsAsync(employeeId, tenantId, orgId, ct))
            return false;

        var rows = await db.EmployeeEducations
            .Where(x => x.Id == id && x.EmployeeId == employeeId)
            .ExecuteDeleteAsync(ct);
        return rows > 0;
    }
}
