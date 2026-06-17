using Microsoft.AspNetCore.Mvc;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Leave;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Authorization;
using ZelosHR.Api.Shared.Constants;
using ZelosHR.Api.Shared.Tenant;
using ZelosHR.Api.Shared.Validation;

namespace ZelosHR.Api.Entities.Holidays;

/// <summary>Public holidays — country calendar for leave working-day calculations.</summary>
[ApiController]
[ApiExplorerSettings(GroupName = SwaggerGroups.Holidays)]
[Route("api/v1/holidays")]
[Produces("application/json")]
public class PublicHolidaysController : ControllerBase
{
    private readonly LeaveService _service;
    private readonly ITenantContextAccessor _tenant;

    public PublicHolidaysController(LeaveService service, ITenantContextAccessor tenant)
    {
        _service = service;
        _tenant = tenant;
    }

    /// <summary>List public holidays by country and year.</summary>
    /// <remarks>
    /// `country_id` — list options via `GET /api/v1/countries/list`, then use returned `id`
    /// (same workflow as `compensation.currency_id` on employees).
    /// When `year` is set, recurring rows include `occurrence_date` for that calendar year.
    /// </remarks>
    [HttpGet("list")]
    [RequiresZelosHrPermission(ZelosHrPermissions.LeaveGet)]
    [ProducesResponseType(typeof(Respons<PublicHolidayListDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Respons<PublicHolidayListDto>>> List(
        [FromQuery(Name = PlatformQueryParams.CountryId)] string? countryId,
        [FromQuery] int? year,
        [FromQuery] int page = 1,
        [FromQuery] int size = 50,
        CancellationToken ct = default)
    {
        var ctx = _tenant.Current;
        var result = await _service.ListHolidaysAsync(
            countryId, year, page, size, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Get a public holiday by id.</summary>
    [HttpGet("get")]
    [RequiresZelosHrPermission(ZelosHrPermissions.LeaveGet)]
    [ProducesResponseType(typeof(Respons<PublicHolidayListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<PublicHolidayListItemDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Respons<PublicHolidayListItemDto>>> Get(
        [FromQuery(Name = PlatformQueryParams.HolidayId)] Guid holidayId,
        CancellationToken ct)
    {
        if (QueryParamValidation.BadRequestIfEmptyGuid<PublicHolidayListItemDto>(
                holidayId, PlatformQueryParams.HolidayId) is { } missingId)
            return missingId;

        var ctx = _tenant.Current;
        var result = await _service.GetHolidayByIdAsync(holidayId, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Create a public holiday (admin).</summary>
    /// <remarks>
    /// Body: holiday_name · date · is_recurring_annually · country_id.
    /// `country_id` — list options via `GET /api/v1/countries/list`, then use returned `id`
    /// (same workflow as `compensation.currency_id` on employees).
    /// </remarks>
    [HttpPost("add")]
    [RequiresZelosHrPermission(ZelosHrPermissions.LeaveAdmin)]
    [ProducesResponseType(typeof(Respons<PublicHolidayListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Respons<PublicHolidayListItemDto>>> Create(
        [FromBody] CreatePublicHolidayDto body,
        CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.CreateHolidayAsync(body, ctx.TenantId, ctx.OrgId, ctx.UserId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Update a public holiday (admin).</summary>
    [HttpPut("update")]
    [RequiresZelosHrPermission(ZelosHrPermissions.LeaveAdmin)]
    [ProducesResponseType(typeof(Respons<PublicHolidayListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<PublicHolidayListItemDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Respons<PublicHolidayListItemDto>>> Update(
        [FromQuery(Name = PlatformQueryParams.HolidayId)] Guid holidayId,
        [FromBody] UpdatePublicHolidayDto body,
        CancellationToken ct)
    {
        if (QueryParamValidation.BadRequestIfEmptyGuid<PublicHolidayListItemDto>(
                holidayId, PlatformQueryParams.HolidayId) is { } missingId)
            return missingId;

        var ctx = _tenant.Current;
        var result = await _service.UpdateHolidayAsync(holidayId, body, ctx.TenantId, ctx.OrgId, ctx.UserId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Remove a public holiday (admin).</summary>
    [HttpDelete("delete")]
    [RequiresZelosHrPermission(ZelosHrPermissions.LeaveAdmin)]
    [ProducesResponseType(typeof(Respons<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Respons<object>>> Delete(
        [FromQuery(Name = PlatformQueryParams.HolidayId)] Guid holidayId,
        CancellationToken ct)
    {
        if (QueryParamValidation.BadRequestIfEmptyGuid<object>(
                holidayId, PlatformQueryParams.HolidayId) is { } missingId)
            return missingId;

        var ctx = _tenant.Current;
        var result = await _service.DeleteHolidayAsync(holidayId, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }
}
