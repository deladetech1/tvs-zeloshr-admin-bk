using ZelosHR.Api.Entities.Shared;

namespace ZelosHR.Api.Entities.Countries;

public sealed class CountriesService
{
    public Respons<IReadOnlyList<GetCountrySimpleReadDto>> List()
    {
        var items = CountryCatalog.All.Select(CountryCatalog.ToReadDto).ToList();
        return Respons<IReadOnlyList<GetCountrySimpleReadDto>>.Ok(items);
    }

    public Respons<IReadOnlyList<GetCountrySimpleReadDto>> Get(string countryId)
    {
        if (string.IsNullOrWhiteSpace(countryId))
        {
            return Respons<IReadOnlyList<GetCountrySimpleReadDto>>.ValidationError(
                new Dictionary<string, string> { ["country_id"] = "country_id is required." });
        }

        if (!CountryCatalog.TryGetById(countryId, out var country))
        {
            return Respons<IReadOnlyList<GetCountrySimpleReadDto>>.Fail(
                "Country not found.",
                statusCode: 404);
        }

        return Respons<IReadOnlyList<GetCountrySimpleReadDto>>.Ok([CountryCatalog.ToReadDto(country)]);
    }
}
