using Microsoft.AspNetCore.Mvc;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Authorization;
using ZelosHR.Api.Shared.Constants;
using ZelosHR.Api.Shared.Tenant;
using ZelosHR.Api.Shared.Validation;

namespace ZelosHR.Api.Entities.EmploymentTypes;

/// <summary>Company settings — employment types referenced by leave policies and employee records.</summary>
[ApiController]
[ApiExplorerSettings(GroupName = SwaggerGroups.CompanySettings)]
[Route("api/v1/employment-types")]
[Produces("application/json")]
public class EmploymentTypesController : ControllerBase
{
    private readonly EmploymentTypesService _service;
    private readonly ITenantContextAccessor _tenant;

    public EmploymentTypesController(EmploymentTypesService service, ITenantContextAccessor tenant)
    {
        _service = service;
        _tenant = tenant;
    }

    /// <summary>List employment types for the organisation.</summary>
    /// <remarks>
    /// Ghana system defaults (Full-time, Part-time, Contractor, Casual, Intern) seed on first access per org.
    /// UI columns: name · description · type (default|custom) · employee_count · is_active.
    /// Query: search · is_active · sort_by (name|type|employees|status|created_at) · sort_order · page · size.
    /// </remarks>
    [HttpGet("list")]
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeGet)]
    [ProducesResponseType(typeof(Respons<EmploymentTypeListDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Respons<EmploymentTypeListDto>>> List(
        [FromQuery] EmploymentTypeListQuery query,
        CancellationToken ct = default)
    {
        var ctx = _tenant.Current;
        var result = await _service.ListAsync(query, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Get an employment type by id.</summary>
    /// <remarks>Same row shape as list items. Use employment_type_id on employee write; read returns nested employment.employment_type.id.</remarks>
    [HttpGet("get")]
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeGet)]
    [ProducesResponseType(typeof(Respons<EmploymentTypeListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<EmploymentTypeListItemDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Respons<EmploymentTypeListItemDto>>> Get(
        [FromQuery(Name = PlatformQueryParams.EmploymentTypeId)] Guid employmentTypeId,
        CancellationToken ct)
    {
        if (QueryParamValidation.BadRequestIfEmptyGuid<EmploymentTypeListItemDto>(
                employmentTypeId, PlatformQueryParams.EmploymentTypeId) is { } missingId)
            return missingId;

        var ctx = _tenant.Current;
        var result = await _service.GetByIdAsync(employmentTypeId, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Add a custom employment type.</summary>
    /// <remarks>Body: name (required) · description (optional). Creates type=custom. System defaults cannot be added manually.</remarks>
    [HttpPost("add")]
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeUpdate)]
    [ProducesResponseType(typeof(Respons<EmploymentTypeListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<EmploymentTypeListItemDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Respons<EmploymentTypeListItemDto>>> Add(
        [FromBody] CreateEmploymentTypeDto body,
        CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.CreateAsync(body, ctx.TenantId, ctx.OrgId, ctx.UserId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Update an employment type.</summary>
    /// <remarks>
    /// Partial body. Custom types: name · description · is_active.
    /// System defaults: description and is_active only — renaming returns 400.
    /// See Swagger request examples: custom_full · system_description_only · deactivate_custom.
    /// </remarks>
    [HttpPut("update")]
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeUpdate)]
    [ProducesResponseType(typeof(Respons<EmploymentTypeListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<EmploymentTypeListItemDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Respons<EmploymentTypeListItemDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Respons<EmploymentTypeListItemDto>>> Update(
        [FromQuery(Name = PlatformQueryParams.EmploymentTypeId)] Guid employmentTypeId,
        [FromBody] UpdateEmploymentTypeDto body,
        CancellationToken ct)
    {
        if (QueryParamValidation.BadRequestIfEmptyGuid<EmploymentTypeListItemDto>(
                employmentTypeId, PlatformQueryParams.EmploymentTypeId) is { } missingId)
            return missingId;

        var ctx = _tenant.Current;
        var result = await _service.UpdateAsync(
            employmentTypeId, body, ctx.TenantId, ctx.OrgId, ctx.UserId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Delete a custom employment type.</summary>
    /// <remarks>Custom only. 400 for system defaults. 409 when employees are assigned. Prefer is_active=false to deactivate.</remarks>
    [HttpDelete("delete")]
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeUpdate)]
    [ProducesResponseType(typeof(Respons<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Respons<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Respons<object>>> Delete(
        [FromQuery(Name = PlatformQueryParams.EmploymentTypeId)] Guid employmentTypeId,
        CancellationToken ct)
    {
        if (QueryParamValidation.BadRequestIfEmptyGuid<object>(
                employmentTypeId, PlatformQueryParams.EmploymentTypeId) is { } missingId)
            return missingId;

        var ctx = _tenant.Current;
        var result = await _service.DeleteAsync(employmentTypeId, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }
}
