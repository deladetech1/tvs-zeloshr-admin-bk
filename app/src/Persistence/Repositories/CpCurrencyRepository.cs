using Microsoft.EntityFrameworkCore;
using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Persistence.Entities;
using ZelosHR.Api.Shared.Constants;

namespace ZelosHR.Api.Persistence.Repositories;

public sealed class CpCurrencyRepository(ZelosHrDbContext db) : ICpCurrencyRepository
{
    private static readonly string NotDeleted = CorePlatformConstants.DeleteStatus.NotDeleted;

    private IQueryable<CpCurrencyEntity> Active(string tenantId) =>
        db.Set<CpCurrencyEntity>()
            .AsNoTracking()
            .Where(c => c.TenantId == tenantId
                        && c.DeleteStatus == NotDeleted
                        && c.IsActive);

    public Task<CpCurrencyDto?> GetByIdAsync(string currencyId, string tenantId, CancellationToken ct = default) =>
        Active(tenantId)
            .Where(c => c.Id == currencyId)
            .Select(c => new CpCurrencyDto(c.Id, c.Name, c.Code, c.Symbol, c.IsDefault))
            .FirstOrDefaultAsync(ct);

    public Task<CpCurrencyDto?> GetDefaultAsync(string tenantId, CancellationToken ct = default) =>
        Active(tenantId)
            .Where(c => c.IsDefault)
            .OrderByDescending(c => c.IsDefault)
            .Select(c => new CpCurrencyDto(c.Id, c.Name, c.Code, c.Symbol, c.IsDefault))
            .FirstOrDefaultAsync(ct);

    public Task<bool> ExistsAsync(string currencyId, string tenantId, CancellationToken ct = default) =>
        Active(tenantId).AnyAsync(c => c.Id == currencyId, ct);
}
