using Microsoft.EntityFrameworkCore;
using ZelosHR.Api.Entities.EmployeePortal;
using ZelosHR.Api.Persistence.Entities;

namespace ZelosHR.Api.Persistence.Repositories;

public sealed class EmployeePortalSubdomainRepository(ZelosHrDbContext db) : IEmployeePortalSubdomainRepository
{
    public Task<EmployeePortalSubdomainEntity?> GetEntityAsync(
        string tenantId, string orgId, CancellationToken ct = default) =>
        db.EmployeePortalSubdomains.AsNoTracking()
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.OrgId == orgId, ct);

    public Task<EmployeePortalSubdomainEntity?> GetBySubdomainAsync(
        string subdomain, CancellationToken ct = default) =>
        db.EmployeePortalSubdomains.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Subdomain == subdomain, ct);

    public Task<bool> SubdomainTakenAsync(
        string subdomain, Guid? excludeId = null, CancellationToken ct = default)
    {
        var query = db.EmployeePortalSubdomains.AsNoTracking()
            .Where(p => p.Subdomain == subdomain);
        if (excludeId is not null)
            query = query.Where(p => p.Id != excludeId.Value);
        return query.AnyAsync(ct);
    }

    public async Task<EmployeePortalSubdomainEntity> CreateAsync(
        string tenantId,
        string orgId,
        string busId,
        string locId,
        CreateEmployeePortalSubdomainDto data,
        string? actorUserId,
        CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var entity = new EmployeePortalSubdomainEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OrgId = orgId,
            BusId = busId,
            LocId = locId,
            Subdomain = EmployeePortalSubdomainRules.Normalize(data.Subdomain!),
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = actorUserId,
            UpdatedBy = actorUserId,
        };
        db.EmployeePortalSubdomains.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task<EmployeePortalSubdomainEntity?> UpdateAsync(
        string tenantId,
        string orgId,
        string busId,
        string locId,
        UpdateEmployeePortalSubdomainDto data,
        string? actorUserId,
        CancellationToken ct = default)
    {
        var entity = await db.EmployeePortalSubdomains
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.OrgId == orgId, ct);
        if (entity is null)
            return null;

        entity.Subdomain = EmployeePortalSubdomainRules.Normalize(data.Subdomain!);
        entity.BusId = busId;
        entity.LocId = locId;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        entity.UpdatedBy = actorUserId;
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task<bool> DeleteAsync(string tenantId, string orgId, CancellationToken ct = default)
    {
        var entity = await db.EmployeePortalSubdomains
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.OrgId == orgId, ct);
        if (entity is null)
            return false;

        db.EmployeePortalSubdomains.Remove(entity);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
