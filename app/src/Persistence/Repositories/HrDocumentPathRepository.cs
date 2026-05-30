using Microsoft.EntityFrameworkCore;
using ZelosHR.Api.Persistence.Entities;

namespace ZelosHR.Api.Persistence.Repositories;

public interface IHrDocumentPathRepository
{
    Task<HrDocumentPathEntity?> GetByIdAsync(string id, string tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<HrDocumentPathEntity>> GetByIdsAsync(
        IEnumerable<string> ids, string tenantId, CancellationToken ct = default);
    Task AddAsync(HrDocumentPathEntity entity, CancellationToken ct = default);
    Task UpdateAsync(HrDocumentPathEntity entity, CancellationToken ct = default);
}

public sealed class HrDocumentPathRepository(ZelosHrDbContext db) : IHrDocumentPathRepository
{
    public Task<HrDocumentPathEntity?> GetByIdAsync(string id, string tenantId, CancellationToken ct = default) =>
        db.HrDocumentPaths.AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == id && x.TenantId == tenantId && x.DeleteStatus == "NOT_DELETED",
                ct);

    public async Task<IReadOnlyList<HrDocumentPathEntity>> GetByIdsAsync(
        IEnumerable<string> ids, string tenantId, CancellationToken ct = default)
    {
        var idList = ids.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList();
        if (idList.Count == 0)
            return [];

        return await db.HrDocumentPaths.AsNoTracking()
            .Where(x => idList.Contains(x.Id) && x.TenantId == tenantId && x.DeleteStatus == "NOT_DELETED")
            .ToListAsync(ct);
    }

    public async Task AddAsync(HrDocumentPathEntity entity, CancellationToken ct = default)
    {
        db.HrDocumentPaths.Add(entity);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(HrDocumentPathEntity entity, CancellationToken ct = default)
    {
        db.HrDocumentPaths.Update(entity);
        await db.SaveChangesAsync(ct);
    }
}
