using Microsoft.EntityFrameworkCore;
using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Persistence.Entities;

namespace ZelosHR.Api.Persistence.Repositories;

public sealed class CpUserRepository(ZelosHrDbContext db) : ICpUserRepository
{
    private const string HrAppId = "app-hr";

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

    public Task<bool> IsLinkedToEmployeeAsync(string userId, string tenantId, CancellationToken ct = default) =>
        db.Employees.AsNoTracking()
            .AnyAsync(e => e.TenantId == tenantId && e.UserId == userId && !e.IsDeleted, ct);

    public async Task<CpUserDto> ProvisionEmployeeUserAsync(
        ProvisionCpUserRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var existing = await FindByEmailAsync(email, request.TenantId, ct);
        if (existing is not null)
            throw new InvalidOperationException($"Platform user already exists for email {email}.");

        var userId = Guid.NewGuid().ToString();
        var now = DateTimeOffset.UtcNow;
        var contact = string.IsNullOrWhiteSpace(request.Contact)
            ? "+233000000000"
            : request.Contact.Trim();

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        try
        {
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
                DeleteStatus = "NOT_DELETED",
                IsActive = true,
                CreatedBy = request.CreatedBy,
                Cdatetime = now,
            });

            db.CpLoginSettings.Add(new CpLoginSettingsEntity
            {
                Id = Guid.NewGuid().ToString(),
                TenantId = request.TenantId,
                UserId = userId,
                CanAlwaysLogin = true,
                DeleteStatus = "NOT_DELETED",
                IsActive = true,
            });

            db.CpUserLocations.Add(new CpUserLocationEntity
            {
                Id = Guid.NewGuid().ToString(),
                TenantId = request.TenantId,
                UserId = userId,
                OrgId = request.OrgId,
                AppId = HrAppId,
                DeleteStatus = "NOT_DELETED",
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
            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
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
            row.Contact = identity.Contact.Trim();

        row.Gender = identity.Gender ?? row.Gender;
        row.Dob = identity.Dob ?? row.Dob;
        row.Address = identity.Address ?? row.Address;
        if (!string.IsNullOrWhiteSpace(identity.ProfilePic))
            row.ProfilePic = identity.ProfilePic;

        await db.SaveChangesAsync(ct);
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
        string userId, string tenantId, string orgId, CancellationToken ct = default)
    {
        var exists = await db.CpUserLocations.AsNoTracking()
            .AnyAsync(l => l.TenantId == tenantId && l.UserId == userId && l.OrgId == orgId && l.AppId == HrAppId, ct);
        if (exists)
            return;

        db.CpUserLocations.Add(new CpUserLocationEntity
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = tenantId,
            UserId = userId,
            OrgId = orgId,
            AppId = HrAppId,
            DeleteStatus = "NOT_DELETED",
            IsActive = true,
        });
        await db.SaveChangesAsync(ct);
    }
}
