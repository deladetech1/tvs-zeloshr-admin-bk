using Microsoft.AspNetCore.Mvc;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Authorization;
using ZelosHR.Api.Shared.Constants;

namespace ZelosHR.Api.Entities.Currencies;

/// <summary>Tenant currency catalog (<c>core_platform.cp_currencies</c>) for employee compensation and forms.</summary>
[ApiController]
[ApiExplorerSettings(GroupName = SwaggerGroups.Currencies)]
[Route("api/v1/currencies")]
[Produces("application/json")]
public class CurrenciesController : ControllerBase
{
    private readonly CurrenciesService _service;

    public CurrenciesController(CurrenciesService service) => _service = service;

    /// <summary>List currencies for the tenant.</summary>
    /// <remarks>
    /// Returns <c>id</c>, <c>name</c>, <c>code</c>, <c>symbol</c>, <c>decimal_places</c>, <c>currency_position</c>, <c>is_default</c>.
    /// Use returned <c>id</c> as <c>compensation.currency_id</c> on employee create/update.
    /// </remarks>
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeGet)]
    [HttpGet("list")]
    [ProducesResponseType(typeof(Respons<IReadOnlyList<GetCurrencySimpleReadDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Respons<IReadOnlyList<GetCurrencySimpleReadDto>>>> List(
        [FromQuery(Name = "is_active")] bool? isActive,
        CancellationToken ct)
    {
        var result = await _service.ListAsync(isActive, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Get one currency by ID.</summary>
    /// <remarks>Mystoreguard parity: <c>data</c> is a one-element array.</remarks>
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeGet)]
    [HttpGet("get")]
    [ProducesResponseType(typeof(Respons<IReadOnlyList<GetCurrencySimpleReadDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<IReadOnlyList<GetCurrencySimpleReadDto>>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Respons<IReadOnlyList<GetCurrencySimpleReadDto>>>> Get(
        [FromQuery(Name = PlatformQueryParams.CurrencyId)] string currencyId,
        CancellationToken ct)
    {
        var result = await _service.GetAsync(currencyId, ct);
        return StatusCode(result.StatusCode, result);
    }
}
