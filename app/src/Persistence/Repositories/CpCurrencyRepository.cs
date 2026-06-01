using Microsoft.EntityFrameworkCore;
using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Persistence.Entities;
using ZelosHR.Api.Shared.Constants;

namespace ZelosHR.Api.Persistence.Repositories;

public sealed class CpCurrencyRepository(ZelosHrDbContext db) : ICpCurrencyRepository
{
    private static readonly string NotDeleted = CorePlatformConstants.DeleteStatus.NotDeleted;

    private IQueryable<CpCurrencyEntity> Scoped(string tenantId) =>
        db.Set<CpCurrencyEntity>()
            .AsNoTracking()
            .Where(c => c.TenantId == tenantId && c.DeleteStatus == NotDeleted);

    private static IQueryable<CpCurrencyEntity> ApplyActiveFilter(IQueryable<CpCurrencyEntity> query, bool? isActive) =>
        isActive switch
        {
            true => query.Where(c => c.IsActive),
            false => query.Where(c => !c.IsActive),
            _ => query,
        };

    public async Task<IReadOnlyList<CpCurrencyDto>> ListAsync(
        string tenantId,
        bool? isActive = null,
        CancellationToken ct = default)
    {
        var query = ApplyActiveFilter(Scoped(tenantId), isActive)
            .OrderByDescending(c => c.IsDefault)
            .ThenBy(c => c.Name);

        return await query
            .Select(c => new CpCurrencyDto(
                c.Id,
                c.Name,
                c.Code,
                c.Symbol,
                c.IsDefault,
                c.DecimalPlaces,
                c.CurrencyPosition))
            .ToListAsync(ct);
    }

    public Task<CpCurrencyDto?> GetByIdAsync(string currencyId, string tenantId, CancellationToken ct = default) =>
        Scoped(tenantId)
            .Where(c => c.Id == currencyId && c.IsActive)
            .Select(c => new CpCurrencyDto(
                c.Id,
                c.Name,
                c.Code,
                c.Symbol,
                c.IsDefault,
                c.DecimalPlaces,
                c.CurrencyPosition))
            .FirstOrDefaultAsync(ct);

    public Task<CpCurrencyDto?> GetDefaultAsync(string tenantId, CancellationToken ct = default) =>
        Scoped(tenantId)
            .Where(c => c.IsDefault && c.IsActive)
            .OrderByDescending(c => c.IsDefault)
            .Select(c => new CpCurrencyDto(
                c.Id,
                c.Name,
                c.Code,
                c.Symbol,
                c.IsDefault,
                c.DecimalPlaces,
                c.CurrencyPosition))
            .FirstOrDefaultAsync(ct);

    public Task<bool> ExistsAsync(string currencyId, string tenantId, CancellationToken ct = default) =>
        Scoped(tenantId).AnyAsync(c => c.Id == currencyId && c.IsActive, ct);
}
