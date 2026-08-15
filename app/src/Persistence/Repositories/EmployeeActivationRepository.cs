using Microsoft.EntityFrameworkCore;
using ZelosHR.Api.Persistence.Entities;
using ZelosHR.Api.Shared.Constants;

namespace ZelosHR.Api.Persistence.Repositories;

public sealed class EmployeeActivationRepository(ZelosHrDbContext db) : IEmployeeActivationRepository
{
    public Task<string> CreateActivationTokenAsync(
        string tenantId,
        string userId,
        string email,
        string token,
        string? createdBy,
        string cdate,
        string ctime,
        DateTimeOffset cdatetime,
        CancellationToken ct = default) =>
        CreateTokenAsync(
            tenantId,
            userId,
            email,
            token,
            EmployeeActivationConstants.OtpDescription,
            createdBy,
            cdate,
            ctime,
            cdatetime,
            ct);

    public Task DeactivateUserActivationTokensAsync(
        string tenantId,
        string userId,
        CancellationToken ct = default) =>
        DeactivateUserTokensAsync(
            tenantId,
            userId,
            EmployeeActivationConstants.OtpDescription,
            ct);

    public Task<EmployeeActivationTokenRow?> FindActiveTokenAsync(string token, CancellationToken ct = default) =>
        FindActiveTokenByDescriptionAsync(token, EmployeeActivationConstants.OtpDescription, ct);

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

    public Task<string> CreatePasswordResetTokenAsync(
        string tenantId,
        string userId,
        string email,
        string token,
        string? createdBy,
        string cdate,
        string ctime,
        DateTimeOffset cdatetime,
        CancellationToken ct = default) =>
        CreateTokenAsync(
            tenantId,
            userId,
            email,
            token,
            EmployeePasswordResetConstants.OtpDescription,
            createdBy,
            cdate,
            ctime,
            cdatetime,
            ct);

    public async Task DeactivateUserPasswordResetTokensAsync(
        string tenantId,
        string userId,
        CancellationToken ct = default) =>
        await DeactivateUserTokensAsync(
            tenantId,
            userId,
            EmployeePasswordResetConstants.OtpDescription,
            ct);

    public Task<EmployeeActivationTokenRow?> FindActivePasswordResetTokenAsync(string token, CancellationToken ct = default) =>
        FindActiveTokenByDescriptionAsync(token, EmployeePasswordResetConstants.OtpDescription, ct);

    public Task<int> CountRecentPasswordResetTokensAsync(
        string tenantId,
        string userId,
        DateTimeOffset since,
        CancellationToken ct = default) =>
        db.CpOtps.AsNoTracking()
            .Where(o => o.TenantId == tenantId
                        && o.CreatedBy == userId
                        && o.Description == EmployeePasswordResetConstants.OtpDescription
                        && o.Cdatetime >= since)
            .CountAsync(ct);

    private async Task<string> CreateTokenAsync(
        string tenantId,
        string userId,
        string email,
        string token,
        string description,
        string? createdBy,
        string cdate,
        string ctime,
        DateTimeOffset cdatetime,
        CancellationToken ct)
    {
        await DeactivateUserTokensAsync(tenantId, userId, description, ct);

        var id = Guid.NewGuid().ToString();
        db.CpOtps.Add(new CpOtpEntity
        {
            Id = id,
            TenantId = tenantId,
            OtpCode = token,
            IsActive = true,
            Description = description,
            Email = email.Trim().ToLowerInvariant(),
            CreatedBy = userId,
            Cdate = cdate,
            Ctime = ctime,
            Cdatetime = cdatetime,
        });
        await db.SaveChangesAsync(ct);
        return id;
    }

    private async Task DeactivateUserTokensAsync(
        string tenantId,
        string userId,
        string description,
        CancellationToken ct)
    {
        var rows = await db.CpOtps
            .Where(o => o.TenantId == tenantId
                        && o.CreatedBy == userId
                        && o.IsActive
                        && o.Description == description)
            .ToListAsync(ct);

        if (rows.Count == 0)
            return;

        foreach (var row in rows)
            row.IsActive = false;

        await db.SaveChangesAsync(ct);
    }

    private async Task<EmployeeActivationTokenRow?> FindActiveTokenByDescriptionAsync(
        string token,
        string description,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        var normalized = token.Trim();
        var otp = await db.CpOtps.AsNoTracking()
            .Where(o => o.OtpCode == normalized
                        && o.IsActive
                        && o.Description == description)
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
}
