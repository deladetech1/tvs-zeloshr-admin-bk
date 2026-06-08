using Microsoft.EntityFrameworkCore;
using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Entities.Users;
using ZelosHR.Api.Persistence.Entities;
using ZelosHR.Api.Shared.Constants;
using ZelosHR.Api.Shared.Infrastructure;
using ZelosHR.Api.Shared.Tenant;
using ZelosHR.Api.Shared.Validation;

namespace ZelosHR.Api.Persistence.Repositories;

public sealed class CpUserRepository(ZelosHrDbContext db) : ICpUserRepository
{
    private static CpUserDto ToDto(CpUserEntity u) =>
        new(u.Id, u.Fullname, u.Email, u.Contact, u.IsActive, u.Gender, u.Dob, u.Address, u.ProfilePic);

    public async Task<CpUserDto?> FindByEmailAsync(string email, string tenantId, CancellationToken ct = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        var row = await db.CpUsers.AsNoTracking()
            .Where(u => u.TenantId == tenantId && u.Email.ToLower() == normalized)
            .FirstOrDefaultAsync(ct);
        return row is null ? null : ToDto(row);
    }

    public async Task<CpUserEmailOwner?> FindEmailOwnerAsync(string email, CancellationToken ct = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        var row = await db.CpUsers.AsNoTracking()
            .Where(u => u.Email.ToLower() == normalized)
            .Select(u => new { u.TenantId, u.Id })
            .FirstOrDefaultAsync(ct);
        return row is null ? null : new CpUserEmailOwner(row.TenantId, row.Id);
    }

    public async Task<CpUserContactOwner?> FindContactOwnerAsync(
        string contact, string? excludeUserId = null, CancellationToken ct = default)
    {
        var normalized = contact.Trim();
        var query = db.CpUsers.AsNoTracking().Where(u => u.Contact == normalized);
        if (!string.IsNullOrWhiteSpace(excludeUserId))
            query = query.Where(u => u.Id != excludeUserId);

        var row = await query.Select(u => new { u.TenantId, u.Id }).FirstOrDefaultAsync(ct);
        return row is null ? null : new CpUserContactOwner(row.TenantId, row.Id);
    }

    public async Task<CpUserDto?> GetByIdAsync(string userId, string tenantId, CancellationToken ct = default)
    {
        var row = await db.CpUsers.AsNoTracking()
            .Where(u => u.TenantId == tenantId && u.Id == userId)
            .FirstOrDefaultAsync(ct);
        return row is null ? null : ToDto(row);
    }

    public async Task<IReadOnlyDictionary<string, CpUserDto>> GetByIdsAsync(
        IEnumerable<string> userIds, string tenantId, CancellationToken ct = default)
    {
        var ids = userIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<string, CpUserDto>();

        var rows = await db.CpUsers.AsNoTracking()
            .Where(u => u.TenantId == tenantId && ids.Contains(u.Id))
            .ToListAsync(ct);

        return rows.ToDictionary(u => u.Id, ToDto, StringComparer.Ordinal);
    }

    public async Task<IReadOnlyList<CpUserDto>> SearchAsync(
        string query, string tenantId, int limit = 20, CancellationToken ct = default)
    {
        var q = $"%{query.Trim()}%";
        var rows = await db.CpUsers.AsNoTracking()
            .Where(u => u.TenantId == tenantId
                        && (EF.Functions.ILike(u.Fullname, q) || EF.Functions.ILike(u.Email, q)))
            .OrderBy(u => u.Fullname)
            .Take(limit)
            .ToListAsync(ct);
        return rows.Select(ToDto).ToList();
    }

    public async Task<(IReadOnlyList<PlatformUserListRow> Items, int Total)> ListPlatformMembersScopedAsync(
        GetUsersQuery query,
        string tenantId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var users = from u in db.CpUsers.AsNoTracking()
                    join m in db.CpMembers.AsNoTracking()
                        on new { u.Id, u.TenantId } equals new { Id = m.UserId, m.TenantId }
                    where u.TenantId == tenantId
                          && m.DeleteStatus == CorePlatformConstants.DeleteStatus.NotDeleted
                    select u;

        users = ApplyPlatformUserFilters(users, query);

        var total = await users.CountAsync(ct);
        var rows = await users
            .OrderByDescending(u => u.Cdatetime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (rows.Select(ToPlatformRow).ToList(), total);
    }

    public Task<bool> IsLinkedToEmployeeAsync(string userId, string tenantId, CancellationToken ct = default) =>
        db.Employees.AsNoTracking()
            .AnyAsync(e => e.TenantId == tenantId && e.UserId == userId, ct);

    public async Task<CpUserDto> ProvisionEmployeeUserAsync(
        ProvisionCpUserRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var existing = await FindByEmailAsync(email, request.TenantId, ct);
        if (existing is not null)
        {
            throw new PlatformUserConflictException(
                "identity.work_email", EmployeeErrorMessages.WorkEmailAlreadyRegistered);
        }

        if (await FindEmailOwnerAsync(email, ct) is not null)
        {
            throw new PlatformUserConflictException(
                "identity.work_email", EmployeeErrorMessages.WorkEmailUsedByAnotherOrganisation);
        }

        var userId = Guid.NewGuid().ToString();
        var now = DateTimeOffset.UtcNow;
        var contact = ResolveContact(request.Contact, userId);
        await EnsureContactAvailableAsync(contact, excludeUserId: null, ct);

        async Task PersistAsync()
        {
            try
            {
                await PersistProvisionAsync(request, userId, email, contact, now, ct);
            }
            catch (DbUpdateException ex)
            {
                ThrowIfCpUserUniqueViolation(ex);
            }
        }

        if (db.Database.CurrentTransaction is null)
        {
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            try
            {
                await PersistAsync();
                await tx.CommitAsync(ct);
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        }
        else
        {
            await PersistAsync();
        }

        return new CpUserDto(
            userId,
            request.FullName.Trim(),
            email,
            contact,
            true,
            request.Gender,
            request.Dob,
            request.Address,
            request.ProfilePic);
    }

    private async Task PersistProvisionAsync(
        ProvisionCpUserRequest request,
        string userId,
        string email,
        string contact,
        DateTimeOffset now,
        CancellationToken ct)
    {
        // Platform FK order: cp_users before cp_login_settings / cp_user_locations / hr_employees.
        db.CpUsers.Add(new CpUserEntity
        {
            Id = userId,
            TenantId = request.TenantId,
            Fullname = request.FullName.Trim(),
            Email = email,
            Contact = contact,
            Gender = request.Gender,
            Dob = request.Dob,
            Address = request.Address,
            ProfilePic = request.ProfilePic,
            CanLogin = true,
            IsOwner = false,
            DeleteStatus = CorePlatformConstants.DeleteStatus.NotDeleted,
            IsActive = true,
            CreatedBy = request.CreatedBy,
            Cdatetime = now,
        });
        await db.SaveChangesAsync(ct);

        db.CpLoginSettings.Add(new CpLoginSettingsEntity
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = request.TenantId,
            UserId = userId,
            CanAlwaysLogin = true,
            DeleteStatus = CorePlatformConstants.DeleteStatus.NotDeleted,
            IsActive = true,
        });

        var busAppLocId = await ResolveBusAppLocationIdAsync(
                request.TenantId, request.OrgId, request.BusId, request.LocId, TroveStandardHeaders.HrAppId, ct)
            ?? throw new InvalidOperationException(
                "Platform context (org, business, location, app) is not configured for this tenant.");

        db.CpUserLocations.Add(new CpUserLocationEntity
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = request.TenantId,
            UserId = userId,
            OrgId = request.OrgId,
            BusId = request.BusId,
            AppId = TroveStandardHeaders.HrAppId,
            BusAppLocId = busAppLocId,
            DeleteStatus = CorePlatformConstants.DeleteStatus.NotDeleted,
            IsActive = true,
        });

        db.HrEmployees.Add(new HrEmployeeEntity
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = request.TenantId,
            UserId = userId,
            CreatedBy = request.CreatedBy,
            Cdatetime = now,
        });

        await db.SaveChangesAsync(ct);
    }

    public async Task<CpUserDto> UpdateIdentityAsync(
        string userId, string tenantId, CpUserIdentityData identity, CancellationToken ct = default)
    {
        var row = await db.CpUsers
            .FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Id == userId, ct)
            ?? throw new InvalidOperationException("Platform user not found.");

        if (!string.IsNullOrWhiteSpace(identity.FullName))
            row.Fullname = identity.FullName.Trim();

        if (!string.IsNullOrWhiteSpace(identity.Email))
            row.Email = identity.Email.Trim().ToLowerInvariant();

        if (!string.IsNullOrWhiteSpace(identity.Contact))
        {
            var contact = identity.Contact.Trim();
            await EnsureContactAvailableAsync(contact, userId, ct);
            row.Contact = contact;
        }

        row.Gender = identity.Gender ?? row.Gender;
        row.Dob = identity.Dob ?? row.Dob;
        row.Address = identity.Address ?? row.Address;
        if (!string.IsNullOrWhiteSpace(identity.ProfilePic))
            row.ProfilePic = identity.ProfilePic;

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            ThrowIfCpUserUniqueViolation(ex);
        }

        return ToDto(row);
    }

    public async Task UpdateProfilePicAsync(
        string userId, string tenantId, string profilePicUrl, CancellationToken ct = default)
    {
        var row = await db.CpUsers
            .FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Id == userId, ct)
            ?? throw new InvalidOperationException("Platform user not found.");

        row.ProfilePic = profilePicUrl;
        await db.SaveChangesAsync(ct);
    }

    public async Task EnsureHrMembershipAsync(
        string userId, string tenantId, string? createdBy, CancellationToken ct = default)
    {
        var exists = await db.HrEmployees.AsNoTracking()
            .AnyAsync(h => h.TenantId == tenantId && h.UserId == userId, ct);
        if (exists)
            return;

        db.HrEmployees.Add(new HrEmployeeEntity
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = tenantId,
            UserId = userId,
            CreatedBy = createdBy,
            Cdatetime = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync(ct);
    }

    public async Task EnsureUserLocationAsync(
        string userId,
        string tenantId,
        string orgId,
        string busId,
        string locId,
        CancellationToken ct = default)
    {
        var exists = await db.CpUserLocations.AsNoTracking()
            .AnyAsync(
                l => l.TenantId == tenantId
                     && l.UserId == userId
                     && l.OrgId == orgId
                     && l.BusId == busId
                     && l.AppId == TroveStandardHeaders.HrAppId,
                ct);
        if (exists)
            return;

        var busAppLocId = await ResolveBusAppLocationIdAsync(tenantId, orgId, busId, locId, TroveStandardHeaders.HrAppId, ct);
        if (busAppLocId is null)
            throw new InvalidOperationException(
                "Platform context (org, business, location, app) is not configured for this tenant.");

        db.CpUserLocations.Add(new CpUserLocationEntity
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = tenantId,
            UserId = userId,
            OrgId = orgId,
            BusId = busId,
            AppId = TroveStandardHeaders.HrAppId,
            BusAppLocId = busAppLocId,
            DeleteStatus = CorePlatformConstants.DeleteStatus.NotDeleted,
            IsActive = true,
        });
        await db.SaveChangesAsync(ct);
    }

    private Task<string?> ResolveBusAppLocationIdAsync(
        string tenantId,
        string orgId,
        string busId,
        string locId,
        string appId,
        CancellationToken ct) =>
        db.BusinessAppLocations.AsNoTracking()
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

    private static string ResolveContact(string? contact, string userId)
    {
        if (!string.IsNullOrWhiteSpace(contact))
            return contact.Trim();

        // cp_users.contact is globally unique; synthesize a stable placeholder per user.
        var suffix = new string(userId.Where(char.IsLetterOrDigit).Take(9).ToArray());
        return $"+233000{suffix.PadRight(9, '0')}";
    }

    private async Task EnsureContactAvailableAsync(string contact, string? excludeUserId, CancellationToken ct)
    {
        if (await FindContactOwnerAsync(contact, excludeUserId, ct) is not null)
        {
            throw new PlatformUserConflictException(
                "identity.phone", EmployeeErrorMessages.PhoneAlreadyRegistered);
        }
    }

    private static void ThrowIfCpUserUniqueViolation(DbUpdateException ex)
    {
        if (PostgresUniqueViolation.IsCpUserEmail(ex))
        {
            throw new PlatformUserConflictException(
                "identity.work_email", EmployeeErrorMessages.WorkEmailAlreadyRegistered);
        }

        if (PostgresUniqueViolation.IsCpUserContact(ex))
        {
            throw new PlatformUserConflictException(
                "identity.phone", EmployeeErrorMessages.PhoneAlreadyRegistered);
        }

        throw ex;
    }

    private static IQueryable<CpUserEntity> ApplyPlatformUserFilters(
        IQueryable<CpUserEntity> query,
        GetUsersQuery filters)
    {
        if (filters.UseOr)
        {
            var isActive = filters.IsActive;
            var deleteStatus = string.IsNullOrWhiteSpace(filters.DeleteStatus)
                ? null
                : filters.DeleteStatus.Trim();
            var canLogin = filters.CanLogin;
            var email = string.IsNullOrWhiteSpace(filters.Email) ? null : filters.Email.Trim();
            var fullname = string.IsNullOrWhiteSpace(filters.Fullname) ? null : filters.Fullname.Trim();
            var gender = string.IsNullOrWhiteSpace(filters.Gender) ? null : filters.Gender.Trim().ToUpperInvariant();

            var hasOptional = isActive is not null
                || deleteStatus is not null
                || canLogin is not null
                || email is not null
                || fullname is not null
                || gender is not null;

            if (hasOptional)
            {
                query = query.Where(u =>
                    (isActive != null && u.IsActive == isActive)
                    || (deleteStatus != null && u.DeleteStatus == deleteStatus)
                    || (canLogin != null && u.CanLogin == canLogin)
                    || (email != null && EF.Functions.ILike(u.Email, $"%{email}%"))
                    || (fullname != null && EF.Functions.ILike(u.Fullname, $"%{fullname}%"))
                    || (gender != null && u.Gender == gender));
            }

            return query;
        }

        if (filters.IsActive is not null)
            query = query.Where(u => u.IsActive == filters.IsActive);

        if (!string.IsNullOrWhiteSpace(filters.DeleteStatus))
            query = query.Where(u => u.DeleteStatus == filters.DeleteStatus.Trim());

        if (filters.CanLogin is not null)
            query = query.Where(u => u.CanLogin == filters.CanLogin);

        if (!string.IsNullOrWhiteSpace(filters.Email))
        {
            var pattern = $"%{filters.Email.Trim()}%";
            query = query.Where(u => EF.Functions.ILike(u.Email, pattern));
        }

        if (!string.IsNullOrWhiteSpace(filters.Fullname))
        {
            var pattern = $"%{filters.Fullname.Trim()}%";
            query = query.Where(u => EF.Functions.ILike(u.Fullname, pattern));
        }

        if (!string.IsNullOrWhiteSpace(filters.Gender))
            query = query.Where(u => u.Gender == filters.Gender.Trim().ToUpperInvariant());

        return query;
    }

    private static PlatformUserListRow ToPlatformRow(CpUserEntity u) => new(
        u.Id,
        u.TenantId,
        u.Fullname,
        u.Email,
        u.Contact,
        u.Address,
        u.Gender,
        u.Dob,
        u.ProfilePic,
        u.CanLogin,
        u.DeleteStatus,
        u.IsActive,
        u.IsOwner,
        u.Description,
        u.Cdate,
        u.Ctime,
        u.Cdatetime);
}
