using Microsoft.EntityFrameworkCore;
using Npgsql;
using ZelosHR.Api.Entities.CompanyLocalization;
using ZelosHR.Api.Persistence.Entities;

namespace ZelosHR.Api.Persistence.Repositories;

public sealed class CompanyLocalizationRepository(ZelosHrDbContext db) : ICompanyLocalizationRepository
{
    public Task<CompanyLocalizationEntity?> GetEntityAsync(
        string tenantId, string orgId, CancellationToken ct = default) =>
        db.CompanyLocalizations.AsNoTracking()
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.OrgId == orgId, ct);

    public async Task<CompanyLocalizationEntity> EnsureStubAsync(
        string tenantId,
        string orgId,
        string currencyId,
        string? actorUserId,
        CancellationToken ct = default)
    {
        var existing = await GetEntityAsync(tenantId, orgId, ct);
        if (existing is not null)
            return existing;

        var now = DateTimeOffset.UtcNow;
        var entity = new CompanyLocalizationEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OrgId = orgId,
            TimeZone = CompanyLocalizationDefaults.TimeZone,
            CurrencyId = currencyId.Trim(),
            DateFormat = CompanyLocalizationDefaults.DateFormat,
            NumberFormat = CompanyLocalizationDefaults.NumberFormat,
            FirstDayOfWeek = CompanyLocalizationDefaults.FirstDayOfWeek,
            YearStartMonth = CompanyLocalizationDefaults.YearStartMonth,
            YearStartDay = CompanyLocalizationDefaults.YearStartDay,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = actorUserId,
            UpdatedBy = actorUserId,
        };

        db.CompanyLocalizations.Add(entity);
        try
        {
            await db.SaveChangesAsync(ct);
            return entity;
        }
        catch (DbUpdateException ex) when (IsCompanyLocalizationTenantOrgUnique(ex))
        {
            db.Entry(entity).State = EntityState.Detached;
            var raced = await GetEntityAsync(tenantId, orgId, ct);
            return raced ?? throw new InvalidOperationException(
                "Localization settings stub insert raced but row is missing.");
        }
    }

    public async Task<CompanyLocalizationEntity> CreateAsync(
        string tenantId,
        string orgId,
        CreateCompanyLocalizationDto data,
        string? actorUserId,
        CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var entity = new CompanyLocalizationEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OrgId = orgId,
            TimeZone = data.TimeZone!.Trim(),
            CurrencyId = data.CurrencyId!.Trim(),
            DateFormat = data.DateFormat!.Trim(),
            NumberFormat = data.NumberFormat!.Trim(),
            FirstDayOfWeek = data.FirstDayOfWeek!.Trim(),
            YearStartMonth = data.YearStartMonth!.Trim(),
            YearStartDay = data.YearStartDay!.Value,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = actorUserId,
            UpdatedBy = actorUserId,
        };
        db.CompanyLocalizations.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task<CompanyLocalizationEntity?> UpdateAsync(
        string tenantId,
        string orgId,
        UpdateCompanyLocalizationDto data,
        string? actorUserId,
        CancellationToken ct = default)
    {
        var entity = await db.CompanyLocalizations
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.OrgId == orgId, ct);
        if (entity is null)
            return null;

        // Full replacement (same shape as create) — every field is required, same as create.
        entity.TimeZone = data.TimeZone!.Trim();
        entity.CurrencyId = data.CurrencyId!.Trim();
        entity.DateFormat = data.DateFormat!.Trim();
        entity.NumberFormat = data.NumberFormat!.Trim();
        entity.FirstDayOfWeek = data.FirstDayOfWeek!.Trim();
        entity.YearStartMonth = data.YearStartMonth!.Trim();
        entity.YearStartDay = data.YearStartDay!.Value;

        entity.UpdatedAt = DateTimeOffset.UtcNow;
        entity.UpdatedBy = actorUserId;
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task<bool> DeleteAsync(string tenantId, string orgId, CancellationToken ct = default)
    {
        var entity = await db.CompanyLocalizations
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.OrgId == orgId, ct);
        if (entity is null)
            return false;

        db.CompanyLocalizations.Remove(entity);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static bool IsCompanyLocalizationTenantOrgUnique(DbUpdateException ex) =>
        ex.InnerException is PostgresException pg
        && pg.SqlState == PostgresErrorCodes.UniqueViolation
        && pg.ConstraintName?.Contains("zhr_company_localization", StringComparison.OrdinalIgnoreCase) == true;
}
