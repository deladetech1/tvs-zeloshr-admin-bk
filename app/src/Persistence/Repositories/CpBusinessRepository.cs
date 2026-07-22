using Microsoft.EntityFrameworkCore;
using ZelosHR.Api.Shared.Constants;

namespace ZelosHR.Api.Persistence.Repositories;

public sealed class CpBusinessRepository(ZelosHrDbContext db) : ICpBusinessRepository
{
    public Task<string?> GetBusNameAsync(string tenantId, string busId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(busId))
            return Task.FromResult<string?>(null);

        return db.CpBusinesses.AsNoTracking()
            .Where(
                b => b.TenantId == tenantId
                     && b.Id == busId
                     && b.DeleteStatus == CorePlatformConstants.DeleteStatus.NotDeleted
                     && b.IsActive)
            .Select(b => b.BusName)
            .FirstOrDefaultAsync(ct);
    }
}
