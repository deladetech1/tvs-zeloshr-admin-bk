using Microsoft.EntityFrameworkCore;
using Npgsql;
using ZelosHR.Api.Entities.CompanyInfo;
using ZelosHR.Api.Persistence.Entities;

namespace ZelosHR.Api.Persistence.Repositories;

public sealed class CompanyProfileRepository(ZelosHrDbContext db) : ICompanyProfileRepository
{
    public Task<CompanyProfileEntity?> GetEntityAsync(
        string tenantId, string orgId, CancellationToken ct = default) =>
        db.CompanyProfiles.AsNoTracking()
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.OrgId == orgId, ct);

    public async Task<CompanyProfileEntity> EnsureStubAsync(
        string tenantId,
        string orgId,
        string? actorUserId,
        string? defaultLegalName,
        CancellationToken ct = default)
    {
        var legalName = NormalizeLegalName(defaultLegalName);
        var existing = await GetEntityAsync(tenantId, orgId, ct);
        if (existing is not null)
            return await BackfillDefaultLegalNameIfEmptyAsync(existing, legalName, actorUserId, ct);

        var now = DateTimeOffset.UtcNow;
        var entity = new CompanyProfileEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OrgId = orgId,
            LegalName = legalName,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = actorUserId,
            UpdatedBy = actorUserId,
        };

        db.CompanyProfiles.Add(entity);
        try
        {
            await db.SaveChangesAsync(ct);
            return entity;
        }
        catch (DbUpdateException ex) when (IsCompanyProfileTenantOrgUnique(ex))
        {
            db.Entry(entity).State = EntityState.Detached;
            var raced = await GetEntityAsync(tenantId, orgId, ct);
            return raced is null
                ? throw new InvalidOperationException("Company profile stub insert raced but row is missing.")
                : await BackfillDefaultLegalNameIfEmptyAsync(raced, legalName, actorUserId, ct);
        }
    }

    private async Task<CompanyProfileEntity> BackfillDefaultLegalNameIfEmptyAsync(
        CompanyProfileEntity existing,
        string? defaultLegalName,
        string? actorUserId,
        CancellationToken ct)
    {
        if (CompanyProfileState.IsConfigured(existing) || string.IsNullOrWhiteSpace(defaultLegalName))
            return existing;

        var tracked = await db.CompanyProfiles
            .FirstOrDefaultAsync(p => p.TenantId == existing.TenantId && p.OrgId == existing.OrgId, ct);
        if (tracked is null || CompanyProfileState.IsConfigured(tracked))
            return existing;

        tracked.LegalName = defaultLegalName;
        tracked.UpdatedAt = DateTimeOffset.UtcNow;
        tracked.UpdatedBy = actorUserId;
        await db.SaveChangesAsync(ct);
        return tracked;
    }

    private static string? NormalizeLegalName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        return trimmed.Length <= 200 ? trimmed : trimmed[..200];
    }

    public async Task<CompanyProfileEntity> CreateAsync(
        string tenantId,
        string orgId,
        CreateCompanyInfoDto data,
        string? actorUserId,
        CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var entity = new CompanyProfileEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OrgId = orgId,
            LegalName = data.LegalName!.Trim(),
            TradingName = Norm(data.TradingName),
            Industry = Norm(data.Industry),
            CompanySize = Norm(data.CompanySize),
            BusinessRegistrationNumber = Norm(data.BusinessRegistrationNumber),
            Tin = Norm(data.Tin),
            PrimaryWorkCountry = Norm(data.PrimaryWorkCountry),
            CompanyEmail = Norm(data.CompanyEmail),
            Website = Norm(data.Website),
            LogoDocumentId = Norm(data.LogoUrl),
            BannerDocumentId = Norm(data.BannerUrl),
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = actorUserId,
            UpdatedBy = actorUserId,
        };
        db.CompanyProfiles.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task<CompanyProfileEntity?> UpdateAsync(
        string tenantId,
        string orgId,
        UpdateCompanyInfoDto data,
        string? actorUserId,
        CancellationToken ct = default)
    {
        var entity = await db.CompanyProfiles
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.OrgId == orgId, ct);
        if (entity is null)
            return null;

        // Full replacement (same shape as create) — omitted optional fields are cleared, not left untouched.
        entity.LegalName = string.IsNullOrWhiteSpace(data.LegalName) ? null : data.LegalName.Trim();
        entity.TradingName = Norm(data.TradingName);
        entity.Industry = Norm(data.Industry);
        entity.CompanySize = Norm(data.CompanySize);
        entity.BusinessRegistrationNumber = Norm(data.BusinessRegistrationNumber);
        entity.Tin = Norm(data.Tin);
        entity.PrimaryWorkCountry = Norm(data.PrimaryWorkCountry);
        entity.CompanyEmail = Norm(data.CompanyEmail);
        entity.Website = Norm(data.Website);
        entity.LogoDocumentId = Norm(data.LogoUrl);
        entity.BannerDocumentId = Norm(data.BannerUrl);

        entity.UpdatedAt = DateTimeOffset.UtcNow;
        entity.UpdatedBy = actorUserId;
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task<bool> DeleteAsync(string tenantId, string orgId, CancellationToken ct = default)
    {
        var entity = await db.CompanyProfiles
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.OrgId == orgId, ct);
        if (entity is null)
            return false;

        db.CompanyProfiles.Remove(entity);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static string? Norm(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool IsCompanyProfileTenantOrgUnique(DbUpdateException ex) =>
        ex.InnerException is PostgresException pg
        && pg.SqlState == PostgresErrorCodes.UniqueViolation
        && pg.ConstraintName?.Contains("zhr_company_profile", StringComparison.OrdinalIgnoreCase) == true;
}
