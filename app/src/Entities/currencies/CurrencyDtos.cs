using ZelosHR.Api.Configs;

namespace ZelosHR.Api.Entities.Currencies;

/// <summary>Mystoreguard-aligned currency read model (<c>GetCurrencySimpleControllerReadDto</c>).</summary>
public sealed class GetCurrencySimpleReadDto
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Code { get; init; }
    public required string Symbol { get; init; }
    public int DecimalPlaces { get; init; } = 2;

    [SwaggerAllowedValues("before", "after")]
    public string CurrencyPosition { get; init; } = "before";

    public bool IsDefault { get; init; }
}
