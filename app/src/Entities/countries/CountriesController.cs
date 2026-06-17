using Microsoft.AspNetCore.Mvc;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Authorization;
using ZelosHR.Api.Shared.Constants;

namespace ZelosHR.Api.Entities.Countries;

/// <summary>Country catalog for holiday pickers — same workflow as currencies on employees.</summary>
[ApiController]
[ApiExplorerSettings(GroupName = SwaggerGroups.Countries)]
[Route("api/v1/countries")]
[Produces("application/json")]
public class CountriesController : ControllerBase
{
    private readonly CountriesService _service;

    public CountriesController(CountriesService service) => _service = service;

    /// <summary>List countries for pickers.</summary>
    /// <remarks>
    /// Returns <c>id</c>, <c>name</c>, <c>code</c>.
    /// Use returned <c>id</c> as <c>country_id</c> on <c>POST /holidays/add</c> (same pattern as <c>compensation.currency_id</c> on employees).
    /// </remarks>
    [HttpGet("list")]
    [RequiresZelosHrPermission(ZelosHrPermissions.LeaveGet)]
    [ProducesResponseType(typeof(Respons<IReadOnlyList<GetCountrySimpleReadDto>>), StatusCodes.Status200OK)]
    public ActionResult<Respons<IReadOnlyList<GetCountrySimpleReadDto>>> List()
    {
        var result = _service.List();
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Get one country by id.</summary>
    /// <remarks>Mystoreguard parity: <c>data</c> is a one-element array.</remarks>
    [HttpGet("get")]
    [RequiresZelosHrPermission(ZelosHrPermissions.LeaveGet)]
    [ProducesResponseType(typeof(Respons<IReadOnlyList<GetCountrySimpleReadDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<IReadOnlyList<GetCountrySimpleReadDto>>), StatusCodes.Status404NotFound)]
    public ActionResult<Respons<IReadOnlyList<GetCountrySimpleReadDto>>> Get(
        [FromQuery(Name = PlatformQueryParams.CountryId)] string countryId)
    {
        var result = _service.Get(countryId);
        return StatusCode(result.StatusCode, result);
    }
}
