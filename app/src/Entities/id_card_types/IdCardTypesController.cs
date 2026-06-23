using Microsoft.AspNetCore.Mvc;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Authorization;
using ZelosHR.Api.Shared.Constants;
using ZelosHR.Api.Shared.Tenant;
using ZelosHR.Api.Shared.Validation;

namespace ZelosHR.Api.Entities.IdCardTypes;

/// <summary>Company settings — ID card types used on employee identity records.</summary>
[ApiController]
[ApiExplorerSettings(GroupName = SwaggerGroups.CompanySettings)]
[Route("api/v1/id-card-types")]
[Produces("application/json")]
public class IdCardTypesController : ControllerBase
{
    private readonly IdCardTypesService _service;
    private readonly ITenantContextAccessor _tenant;

    public IdCardTypesController(IdCardTypesService service, ITenantContextAccessor tenant)
    {
        _service = service;
        _tenant = tenant;
    }

    /// <summary>List ID card types for the organisation.</summary>
    /// <remarks>
    /// Ghana system defaults (National ID, Voter's ID, Driver's License, National Health Insurance) seed on first access per org.
    /// UI columns: name · description · type (default|custom) · is_active.
    /// Query: search · is_active · sort_by (name|type|status|created_at) · sort_order · page · size.
    /// </remarks>
    [HttpGet("list")]
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeGet)]
    [ProducesResponseType(typeof(Respons<IdCardTypeListDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Respons<IdCardTypeListDto>>> List(
        [FromQuery] IdCardTypeListQuery query,
        CancellationToken ct = default)
    {
        var ctx = _tenant.Current;
        var result = await _service.ListAsync(query, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Get an ID card type by id.</summary>
    [HttpGet("get")]
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeGet)]
    [ProducesResponseType(typeof(Respons<IdCardTypeListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<IdCardTypeListItemDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Respons<IdCardTypeListItemDto>>> Get(
        [FromQuery(Name = PlatformQueryParams.IdCardTypeId)] Guid idCardTypeId,
        CancellationToken ct)
    {
        if (QueryParamValidation.BadRequestIfEmptyGuid<IdCardTypeListItemDto>(
                idCardTypeId, PlatformQueryParams.IdCardTypeId) is { } missingId)
            return missingId;

        var ctx = _tenant.Current;
        var result = await _service.GetByIdAsync(idCardTypeId, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Add a custom ID card type.</summary>
    /// <remarks>Body: name (required) · description (optional). Creates type=custom. System defaults cannot be added manually.</remarks>
    [HttpPost("add")]
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeUpdate)]
    [ProducesResponseType(typeof(Respons<IdCardTypeListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<IdCardTypeListItemDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Respons<IdCardTypeListItemDto>>> Add(
        [FromBody] CreateIdCardTypeDto body,
        CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.CreateAsync(body, ctx.TenantId, ctx.OrgId, ctx.UserId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Update an ID card type.</summary>
    /// <remarks>
    /// Partial body. Custom types: name · description · is_active.
    /// System defaults: description and is_active only — renaming returns 400.
    /// </remarks>
    [HttpPut("update")]
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeUpdate)]
    [ProducesResponseType(typeof(Respons<IdCardTypeListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<IdCardTypeListItemDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Respons<IdCardTypeListItemDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Respons<IdCardTypeListItemDto>>> Update(
        [FromQuery(Name = PlatformQueryParams.IdCardTypeId)] Guid idCardTypeId,
        [FromBody] UpdateIdCardTypeDto body,
        CancellationToken ct)
    {
        if (QueryParamValidation.BadRequestIfEmptyGuid<IdCardTypeListItemDto>(
                idCardTypeId, PlatformQueryParams.IdCardTypeId) is { } missingId)
            return missingId;

        var ctx = _tenant.Current;
        var result = await _service.UpdateAsync(
            idCardTypeId, body, ctx.TenantId, ctx.OrgId, ctx.UserId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Delete a custom ID card type.</summary>
    /// <remarks>Custom only. 400 for system defaults. Prefer is_active=false to deactivate.</remarks>
    [HttpDelete("delete")]
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeUpdate)]
    [ProducesResponseType(typeof(Respons<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Respons<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Respons<object>>> Delete(
        [FromQuery(Name = PlatformQueryParams.IdCardTypeId)] Guid idCardTypeId,
        CancellationToken ct)
    {
        if (QueryParamValidation.BadRequestIfEmptyGuid<object>(
                idCardTypeId, PlatformQueryParams.IdCardTypeId) is { } missingId)
            return missingId;

        var ctx = _tenant.Current;
        var result = await _service.DeleteAsync(idCardTypeId, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }
}
