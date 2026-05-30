namespace ZelosHR.Api.Entities.Employees;

public sealed record CpCurrencyDto(
    string Id,
    string Name,
    string Code,
    string Symbol,
    bool IsDefault);

public interface ICpCurrencyRepository
{
    Task<CpCurrencyDto?> GetByIdAsync(string currencyId, string tenantId, CancellationToken ct = default);
    Task<CpCurrencyDto?> GetDefaultAsync(string tenantId, CancellationToken ct = default);
    Task<bool> ExistsAsync(string currencyId, string tenantId, CancellationToken ct = default);
}
