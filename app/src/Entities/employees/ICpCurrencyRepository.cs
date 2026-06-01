namespace ZelosHR.Api.Entities.Employees;

public sealed record CpCurrencyDto(
    string Id,
    string Name,
    string Code,
    string Symbol,
    bool IsDefault,
    int DecimalPlaces = 2,
    string CurrencyPosition = "before");

public interface ICpCurrencyRepository
{
    Task<IReadOnlyList<CpCurrencyDto>> ListAsync(
        string tenantId,
        bool? isActive = null,
        CancellationToken ct = default);

    Task<CpCurrencyDto?> GetByIdAsync(string currencyId, string tenantId, CancellationToken ct = default);

    Task<CpCurrencyDto?> GetDefaultAsync(string tenantId, CancellationToken ct = default);

    Task<bool> ExistsAsync(string currencyId, string tenantId, CancellationToken ct = default);
}
