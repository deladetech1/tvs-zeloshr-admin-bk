using Microsoft.EntityFrameworkCore;
using ZelosHR.Api.Entities.CompanyInfo;
using ZelosHR.Api.Persistence.Entities;

namespace ZelosHR.Api.Persistence.Repositories;

public sealed class CompanyOfficeRepository(ZelosHrDbContext db) : ICompanyOfficeRepository
{
    public async Task<IReadOnlyList<CompanyOfficeEntity>> ListAsync(
        string tenantId, string orgId, CancellationToken ct = default) =>
        await db.CompanyOffices.AsNoTracking()
            .Where(o => o.TenantId == tenantId && o.OrgId == orgId)
            .OrderBy(o => o.Name)
            .ToListAsync(ct);

    public async Task<(IReadOnlyList<CompanyOfficeEntity> Items, IReadOnlyList<Guid> UnknownIds)> ReplaceAllAsync(
        string tenantId,
        string orgId,
        IReadOnlyList<CompanyOfficeWriteDto> items,
        string? actorUserId,
        CancellationToken ct = default)
    {
        var existing = await db.CompanyOffices
            .Where(o => o.TenantId == tenantId && o.OrgId == orgId)
            .ToListAsync(ct);
        var existingById = existing.ToDictionary(o => o.Id);

        var unknownIds = items
            .Where(i => i.OfficeId.HasValue && !existingById.ContainsKey(i.OfficeId.Value))
            .Select(i => i.OfficeId!.Value)
            .Distinct()
            .ToList();
        if (unknownIds.Count > 0)
            return (existing, unknownIds);

        var keepIds = items.Where(i => i.OfficeId.HasValue).Select(i => i.OfficeId!.Value).ToHashSet();
        foreach (var stale in existing.Where(o => !keepIds.Contains(o.Id)))
            db.CompanyOffices.Remove(stale);

        var now = DateTimeOffset.UtcNow;
        var result = new List<CompanyOfficeEntity>();
        foreach (var item in items)
        {
            if (item.OfficeId.HasValue && existingById.TryGetValue(item.OfficeId.Value, out var entity))
            {
                entity.Name = item.Name!.Trim();
                entity.Country = Norm(item.Country);
                entity.City = Norm(item.City);
                entity.Phone = Norm(item.Phone);
                entity.IsHeadOffice = item.IsHeadOffice ?? false;
                entity.UpdatedAt = now;
                entity.UpdatedBy = actorUserId;
                result.Add(entity);
            }
            else
            {
                var created = new CompanyOfficeEntity
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    OrgId = orgId,
                    Name = item.Name!.Trim(),
                    Country = Norm(item.Country),
                    City = Norm(item.City),
                    Phone = Norm(item.Phone),
                    IsHeadOffice = item.IsHeadOffice ?? false,
                    CreatedAt = now,
                    UpdatedAt = now,
                    CreatedBy = actorUserId,
                    UpdatedBy = actorUserId,
                };
                db.CompanyOffices.Add(created);
                result.Add(created);
            }
        }

        await db.SaveChangesAsync(ct);
        return (result, []);
    }

    public async Task DeleteAllAsync(string tenantId, string orgId, CancellationToken ct = default)
    {
        var existing = await db.CompanyOffices
            .Where(o => o.TenantId == tenantId && o.OrgId == orgId)
            .ToListAsync(ct);
        if (existing.Count == 0)
            return;

        db.CompanyOffices.RemoveRange(existing);
        await db.SaveChangesAsync(ct);
    }

    private static string? Norm(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
