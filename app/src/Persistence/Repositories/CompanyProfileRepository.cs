using Microsoft.EntityFrameworkCore;
using ZelosHR.Api.Entities.CompanyInfo;
using ZelosHR.Api.Persistence.Entities;

namespace ZelosHR.Api.Persistence.Repositories;

public sealed class CompanyProfileRepository(ZelosHrDbContext db) : ICompanyProfileRepository
{
    public Task<CompanyProfileEntity?> GetEntityAsync(
        string tenantId, string orgId, CancellationToken ct = default) =>
        db.CompanyProfiles.AsNoTracking()
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.OrgId == orgId, ct);

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

        if (data.LegalName is not null) entity.LegalName = data.LegalName.Trim();
        if (data.TradingName is not null) entity.TradingName = Norm(data.TradingName);
        if (data.Industry is not null) entity.Industry = Norm(data.Industry);
        if (data.CompanySize is not null) entity.CompanySize = Norm(data.CompanySize);
        if (data.BusinessRegistrationNumber is not null)
            entity.BusinessRegistrationNumber = Norm(data.BusinessRegistrationNumber);
        if (data.Tin is not null) entity.Tin = Norm(data.Tin);
        if (data.PrimaryWorkCountry is not null) entity.PrimaryWorkCountry = Norm(data.PrimaryWorkCountry);
        if (data.CompanyEmail is not null) entity.CompanyEmail = Norm(data.CompanyEmail);
        if (data.Website is not null) entity.Website = Norm(data.Website);
        if (data.LogoUrl is not null) entity.LogoDocumentId = Norm(data.LogoUrl);
        if (data.BannerUrl is not null) entity.BannerDocumentId = Norm(data.BannerUrl);

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
}
