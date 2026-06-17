namespace ZelosHR.Api.Entities.Countries;

/// <summary>Country catalog read model — same shape as <c>GET /currencies/list</c> rows.</summary>
public sealed class GetCountrySimpleReadDto
{
    public required string Id { get; init; }
    public required string Name { get; init; }

    /// <summary>ISO 3166-1 alpha-2 code (display only — do not send as <c>country_id</c>).</summary>
    public required string Code { get; init; }
}
