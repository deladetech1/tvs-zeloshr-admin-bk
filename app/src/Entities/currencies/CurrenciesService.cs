using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Tenant;

namespace ZelosHR.Api.Entities.Currencies;

public sealed class CurrenciesService
{
    private readonly ICpCurrencyRepository _currencies;
    private readonly ITenantContext _tenant;

    public CurrenciesService(ICpCurrencyRepository currencies, ITenantContext tenant)
    {
        _currencies = currencies;
        _tenant = tenant;
    }

    public async Task<Respons<IReadOnlyList<GetCurrencySimpleReadDto>>> ListAsync(
        bool? isActive,
        CancellationToken ct = default)
    {
        var items = await _currencies.ListAsync(_tenant.TenantId, isActive, ct);
        return Respons<IReadOnlyList<GetCurrencySimpleReadDto>>.Ok(items.Select(ToReadDto).ToList());
    }

    public async Task<Respons<IReadOnlyList<GetCurrencySimpleReadDto>>> GetAsync(
        string currencyId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(currencyId))
        {
            return Respons<IReadOnlyList<GetCurrencySimpleReadDto>>.ValidationError(
                new Dictionary<string, string> { ["currency_id"] = "currency_id is required." });
        }

        var currency = await _currencies.GetByIdAsync(currencyId.Trim(), _tenant.TenantId, ct);
        if (currency is null)
        {
            return Respons<IReadOnlyList<GetCurrencySimpleReadDto>>.Fail(
                "Currency not found.",
                statusCode: 404);
        }

        return Respons<IReadOnlyList<GetCurrencySimpleReadDto>>.Ok([ToReadDto(currency)]);
    }

    private static GetCurrencySimpleReadDto ToReadDto(CpCurrencyDto c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Code = c.Code,
        Symbol = c.Symbol,
        DecimalPlaces = c.DecimalPlaces,
        CurrencyPosition = c.CurrencyPosition,
        IsDefault = c.IsDefault,
    };
}
