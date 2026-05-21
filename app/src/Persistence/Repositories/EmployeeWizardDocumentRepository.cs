using Microsoft.EntityFrameworkCore;
using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Persistence.Entities;

namespace ZelosHR.Api.Persistence.Repositories;

public sealed class EmployeeWizardDocumentRepository(ZelosHrDbContext db) : IEmployeeWizardDocumentRepository
{
    private IQueryable<EmployeeDocumentEntity> Scoped(string tenantId, string orgId) =>
        db.EmployeeDocuments.Where(d => d.TenantId == tenantId && d.OrgId == orgId && !d.IsDeleted);

    public Task<bool> EmployeeExistsAsync(Guid employeeId, string tenantId, string orgId, CancellationToken ct = default) =>
        db.Employees.AsNoTracking()
            .AnyAsync(e => e.Id == employeeId && e.TenantId == tenantId && e.OrgId == orgId && !e.IsDeleted, ct);

    public async Task<IReadOnlyList<EmployeeDocumentEntity>> ListByEmployeeAsync(
        Guid employeeId, string tenantId, string orgId, string? category, CancellationToken ct = default)
    {
        var q = Scoped(tenantId, orgId).Where(d => d.EmployeeId == employeeId);
        if (!string.IsNullOrWhiteSpace(category) && !string.Equals(category, "All", StringComparison.OrdinalIgnoreCase))
            q = q.Where(d => d.Category == category.Trim());
        return await q.OrderByDescending(d => d.UploadedAt).ToListAsync(ct);
    }

    public Task<EmployeeDocumentEntity?> GetByIdAsync(
        Guid id, Guid employeeId, string tenantId, string orgId, CancellationToken ct = default) =>
        Scoped(tenantId, orgId).AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id && d.EmployeeId == employeeId, ct);

    public async Task<EmployeeDocumentEntity> AddAsync(EmployeeDocumentEntity entity, CancellationToken ct = default)
    {
        entity.UploadedAt = DateTimeOffset.UtcNow;
        db.EmployeeDocuments.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task<bool> SoftDeleteAsync(
        Guid id, Guid employeeId, string tenantId, string orgId, CancellationToken ct = default)
    {
        var rows = await Scoped(tenantId, orgId)
            .Where(d => d.Id == id && d.EmployeeId == employeeId)
            .ExecuteUpdateAsync(s => s.SetProperty(d => d.IsDeleted, true), ct);
        return rows > 0;
    }
}
