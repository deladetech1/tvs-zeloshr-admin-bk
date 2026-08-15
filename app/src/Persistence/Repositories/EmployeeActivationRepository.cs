using Microsoft.EntityFrameworkCore;
using ZelosHR.Api.Persistence.Entities;
using ZelosHR.Api.Shared.Constants;

namespace ZelosHR.Api.Persistence.Repositories;

public sealed class EmployeeActivationRepository(ZelosHrDbContext db) : IEmployeeActivationRepository
{
    public async Task<string> CreateActivationTokenAsync(
        string tenantId,
        string userId,
        string email,
        string token,
        string? createdBy,
        string cdate,
        string ctime,
        DateTimeOffset cdatetime,
        CancellationToken ct = default)
    {
        await DeactivateUserActivationTokensAsync(tenantId, userId, ct);

        var id = Guid.NewGuid().ToString();
        db.CpOtps.Add(new CpOtpEntity
        {
            Id = id,
            TenantId = tenantId,
            OtpCode = token,
            IsActive = true,
            Description = EmployeeActivationConstants.OtpDescription,
            Email = email.Trim().ToLowerInvariant(),
            CreatedBy = userId,
            Cdate = cdate,
            Ctime = ctime,
            Cdatetime = cdatetime,
        });
        await db.SaveChangesAsync(ct);
        return id;
    }

    public async Task DeactivateUserActivationTokensAsync(
        string tenantId,
        string userId,
        CancellationToken ct = default)
    {
        var rows = await db.CpOtps
            .Where(o => o.TenantId == tenantId
                        && o.CreatedBy == userId
                        && o.IsActive
                        && o.Description == EmployeeActivationConstants.OtpDescription)
            .ToListAsync(ct);

        if (rows.Count == 0)
            return;

        foreach (var row in rows)
            row.IsActive = false;

        await db.SaveChangesAsync(ct);
    }

    public async Task<EmployeeActivationTokenRow?> FindActiveTokenAsync(string token, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        var normalized = token.Trim();
        var otp = await db.CpOtps.AsNoTracking()
            .Where(o => o.OtpCode == normalized
                        && o.IsActive
                        && o.Description == EmployeeActivationConstants.OtpDescription)
            .OrderByDescending(o => o.Cdatetime)
            .FirstOrDefaultAsync(ct);

        if (otp is null || string.IsNullOrWhiteSpace(otp.CreatedBy))
            return null;

        var user = await db.CpUsers.AsNoTracking()
            .Where(u => u.TenantId == otp.TenantId && u.Id == otp.CreatedBy)
            .FirstOrDefaultAsync(ct);

        if (user is null)
            return null;

        var hasPassword = !string.IsNullOrWhiteSpace(user.LoginPassword);
        return new EmployeeActivationTokenRow(otp, user, hasPassword);
    }

    public Task<CpPasswordPolicyEntity?> GetActivePasswordPolicyAsync(string tenantId, CancellationToken ct = default) =>
        db.CpPasswordPolicies.AsNoTracking()
            .Where(p => p.TenantId == tenantId && p.IsActive)
            .OrderByDescending(p => p.EnforcePasswordPolicy)
            .FirstOrDefaultAsync(ct);

    public async Task SetUserPasswordAsync(
        string tenantId,
        string userId,
        string hashedPassword,
        CancellationToken ct = default)
    {
        var user = await db.CpUsers
            .FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Id == userId, ct)
            ?? throw new InvalidOperationException("Platform user not found.");

        user.LoginPassword = hashedPassword;
        user.CanLogin = true;

        var loginSettings = await db.CpLoginSettings
            .Where(s => s.TenantId == tenantId && s.UserId == userId && s.IsActive)
            .OrderBy(s => s.UserId)
            .FirstOrDefaultAsync(ct);

        if (loginSettings is not null)
            loginSettings.IsLoginBefore = false;

        await db.SaveChangesAsync(ct);
    }

    public async Task ConsumeTokenAsync(string otpId, string tenantId, CancellationToken ct = default)
    {
        var otp = await db.CpOtps
            .FirstOrDefaultAsync(o => o.Id == otpId && o.TenantId == tenantId, ct);
        if (otp is null)
            return;

        otp.IsActive = false;
        await db.SaveChangesAsync(ct);
    }

    public Task<bool> UserHasPasswordAsync(string tenantId, string userId, CancellationToken ct = default) =>
        db.CpUsers.AsNoTracking()
            .Where(u => u.TenantId == tenantId && u.Id == userId)
            .Select(u => u.LoginPassword != null && u.LoginPassword != string.Empty)
            .FirstOrDefaultAsync(ct);
}
