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

        // A user is granted access to a location either directly (cp_user_locations) or via a
        // group they belong to (cp_group_locations -> cp_user_groups). This mirrors core-platform's
        // effective-access model and the other Trove apps (mystoreguard, loandrift); checking only
        // the direct grant would 403 users who were granted access through a group.
        var hasDirectGrant = await db.CpUserLocations.AsNoTracking()
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

        if (hasDirectGrant)
            return true;

        return await db.CpGroupLocations.AsNoTracking()
            .Where(
                gl => gl.TenantId == tenantId
                      && gl.BusAppLocId == businessAppLocationId
                      && gl.DeleteStatus == CorePlatformConstants.DeleteStatus.NotDeleted
                      && gl.IsActive)
            .Join(
                db.CpUserGroups.AsNoTracking()
                    .Where(
                        ug => ug.TenantId == tenantId
                              && ug.UserId == userId
                              && ug.DeleteStatus == CorePlatformConstants.DeleteStatus.NotDeleted
                              && ug.IsActive),
                gl => gl.GroupId,
                ug => ug.GroupId,
                (gl, _) => gl.Id)
            .AnyAsync(ct);
    }
}
