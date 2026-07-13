using Microsoft.EntityFrameworkCore;
using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Persistence;
using ZelosHR.Api.Persistence.Entities;

namespace ZelosHR.Api.Persistence.Repositories;

public sealed class EmployeeChangeRequestRepository(ZelosHrDbContext db) : IEmployeeChangeRequestRepository
{
    public Task<EmployeeChangeRequestEntity?> GetByIdScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default) =>
        db.EmployeeChangeRequests.AsNoTracking()
            .FirstOrDefaultAsync(
                r => r.Id == id && r.TenantId == tenantId && r.OrgId == orgId,
                ct);

    public Task<EmployeeChangeRequestEntity?> GetTrackedByIdScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default) =>
        db.EmployeeChangeRequests
            .FirstOrDefaultAsync(
                r => r.Id == id && r.TenantId == tenantId && r.OrgId == orgId,
                ct);

    public async Task<(IReadOnlyList<EmployeeChangeRequestEntity> Items, int Total)> ListScopedAsync(
        string tenantId,
        string orgId,
        Guid? employeeId,
        string? status,
        int page,
        int size,
        CancellationToken ct = default)
    {
        var query = db.EmployeeChangeRequests.AsNoTracking()
            .Where(r => r.TenantId == tenantId && r.OrgId == orgId);

        if (employeeId is not null)
            query = query.Where(r => r.EmployeeId == employeeId);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(r => r.Status == status);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<IReadOnlyList<EmployeeChangeRequestEntity>> ListPendingByFieldPathsAsync(
        Guid employeeId,
        IEnumerable<string> fieldPaths,
        CancellationToken ct = default)
    {
        var paths = fieldPaths.ToList();
        if (paths.Count == 0)
            return [];

        return await db.EmployeeChangeRequests
            .Where(r => r.EmployeeId == employeeId
                && r.Status == ChangeRequestStatuses.Pending
                && paths.Contains(r.FieldPath))
            .ToListAsync(ct);
    }

    public Task AddAsync(EmployeeChangeRequestEntity entity, CancellationToken ct = default)
    {
        db.EmployeeChangeRequests.Add(entity);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);
}
