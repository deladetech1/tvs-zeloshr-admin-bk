using Microsoft.AspNetCore.Mvc;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Authorization;
using ZelosHR.Api.Shared.Constants;
using ZelosHR.Api.Shared.Tenant;
using ZelosHR.Api.Shared.Validation;

namespace ZelosHR.Api.Entities.CompanyLocalization;

/// <summary>Company settings — regional formats and the leave/financial year start date.</summary>
[ApiController]
[ApiExplorerSettings(GroupName = SwaggerGroups.CompanySettings)]
[Route("api/v1/company/localization")]
[Produces("application/json")]
public class CompanyLocalizationController : ControllerBase
{
    private readonly CompanyLocalizationService _service;
    private readonly ITenantContextAccessor _tenant;

    public CompanyLocalizationController(CompanyLocalizationService service, ITenantContextAccessor tenant)
    {
        _service = service;
        _tenant = tenant;
    }

    /// <summary>Get the org's localization settings.</summary>
    [HttpGet("get")]
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeGet)]
    [ProducesResponseType(typeof(Respons<CompanyLocalizationReadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<CompanyLocalizationReadDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Respons<CompanyLocalizationReadDto>>> Get(CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.GetAsync(ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Create the org's localization settings.</summary>
    /// <remarks>
    /// One row per org. All fields required: time_zone (IANA id), currency_id (from
    /// GET /currencies/list), date_format, number_format, first_day_of_week, year_start_month,
    /// year_start_day. 400 if settings already exist — use PUT /update instead.
    /// </remarks>
    [HttpPost("add")]
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeUpdate)]
    [ProducesResponseType(typeof(Respons<CompanyLocalizationReadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<CompanyLocalizationReadDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Respons<CompanyLocalizationReadDto>>> Add(
        [FromBody] CreateCompanyLocalizationDto body, CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.CreateAsync(body, ctx.TenantId, ctx.OrgId, ctx.UserId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Update the org's localization settings.</summary>
    /// <remarks>
    /// Same shape as POST /add, plus id (must match the settings' current id from GET /get).
    /// Full replacement, not a partial patch — all fields are required, same as create.
    /// </remarks>
    [HttpPut("update")]
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeUpdate)]
    [ProducesResponseType(typeof(Respons<CompanyLocalizationReadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<CompanyLocalizationReadDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Respons<CompanyLocalizationReadDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Respons<CompanyLocalizationReadDto>>> Update(
        [FromBody] UpdateCompanyLocalizationDto body, CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.UpdateAsync(body, ctx.TenantId, ctx.OrgId, ctx.UserId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Delete the org's localization settings.</summary>
    /// <remarks>id must match the settings' current id (from GET /get).</remarks>
    [HttpDelete("delete")]
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeUpdate)]
    [ProducesResponseType(typeof(Respons<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Respons<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Respons<object>>> Delete(
        [FromQuery(Name = PlatformQueryParams.Id)] Guid id, CancellationToken ct)
    {
        if (QueryParamValidation.BadRequestIfEmptyGuid<object>(id, PlatformQueryParams.Id) is { } missingId)
            return missingId;

        var ctx = _tenant.Current;
        var result = await _service.DeleteAsync(ctx.TenantId, ctx.OrgId, id, ct);
        return StatusCode(result.StatusCode, result);
    }
}
