using Microsoft.EntityFrameworkCore;
using ZelosHR.Api.Shared.Constants;

namespace ZelosHR.Api.Persistence.Repositories;

public sealed class PlatformContextRepository(ZelosHrDbContext db) : IPlatformContextRepository
{
    public async Task<bool> ValidateSessionContextAsync(
        string tenantId,
        string? userId,
        string orgId,
        string busId,
        string locId,
        string appId,
        CancellationToken ct = default)
    {
        var businessAppLocationId = await db.BusinessAppLocations.AsNoTracking()
            .Where(
                b => b.TenantId == tenantId
                     && b.OrgId == orgId
                     && b.BusId == busId
                     && b.LocId == locId
                     && b.AppId == appId
                     && b.DeleteStatus == CorePlatformConstants.DeleteStatus.NotDeleted
                     && b.IsActive)
            .Select(b => b.Id)
            .FirstOrDefaultAsync(ct);

        if (businessAppLocationId is null)
            return false;

        if (string.IsNullOrWhiteSpace(userId))
            return true;

        return await db.CpUserLocations.AsNoTracking()
            .AnyAsync(
                ul => ul.TenantId == tenantId
                      && ul.UserId == userId
                      && ul.OrgId == orgId
                      && ul.BusId == busId
                      && ul.AppId == appId
                      && ul.BusAppLocId == businessAppLocationId
                      && ul.DeleteStatus == CorePlatformConstants.DeleteStatus.NotDeleted
                      && ul.IsActive,
                ct);
    }
}
