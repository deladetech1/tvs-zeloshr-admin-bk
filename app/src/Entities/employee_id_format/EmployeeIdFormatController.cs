using Microsoft.AspNetCore.Mvc;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Authorization;
using ZelosHR.Api.Shared.Constants;
using ZelosHR.Api.Shared.Tenant;
using ZelosHR.Api.Shared.Validation;

namespace ZelosHR.Api.Entities.EmployeeIdFormat;

/// <summary>Company settings — org-wide employee ID / code generation format.</summary>
[ApiController]
[ApiExplorerSettings(GroupName = SwaggerGroups.CompanySettings)]
[Route("api/v1/company/id-format")]
[Produces("application/json")]
public sealed class EmployeeIdFormatController : ControllerBase
{
    private readonly EmployeeIdFormatService _service;
    private readonly ITenantContextAccessor _tenant;

    public EmployeeIdFormatController(EmployeeIdFormatService service, ITenantContextAccessor tenant)
    {
        _service = service;
        _tenant = tenant;
    }

    /// <summary>List the org's employee ID format settings.</summary>
    /// <remarks>Returns zero or one row for the current org. Does not auto-create defaults.</remarks>
    [HttpGet("list")]
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeGet)]
    [ProducesResponseType(typeof(Respons<EmployeeIdFormatListDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Respons<EmployeeIdFormatListDto>>> List(CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.ListAsync(ctx.TenantId, ctx.OrgId, ctx.UserId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Get the org's employee ID format settings.</summary>
    /// <remarks>404 when not configured — use POST /add to create. Includes <c>next_id_preview</c>.</remarks>
    [HttpGet("get")]
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeGet)]
    [ProducesResponseType(typeof(Respons<EmployeeIdFormatReadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<EmployeeIdFormatReadDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Respons<EmployeeIdFormatReadDto>>> Get(CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.GetAsync(ctx.TenantId, ctx.OrgId, ctx.UserId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Create the org's employee ID format settings.</summary>
    /// <remarks>One row per org. 400 if settings already exist — use PUT /update instead.</remarks>
    [HttpPost("add")]
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeUpdate)]
    [ProducesResponseType(typeof(Respons<EmployeeIdFormatReadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<EmployeeIdFormatReadDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Respons<EmployeeIdFormatReadDto>>> Add(
        [FromBody] CreateEmployeeIdFormatDto body, CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.CreateAsync(body, ctx.TenantId, ctx.OrgId, ctx.UserId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Update the org's employee ID format settings.</summary>
    /// <remarks>Same shape as POST /add, plus <c>id</c> from GET /get. Full replacement.</remarks>
    [HttpPut("update")]
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeUpdate)]
    [ProducesResponseType(typeof(Respons<EmployeeIdFormatReadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<EmployeeIdFormatReadDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Respons<EmployeeIdFormatReadDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Respons<EmployeeIdFormatReadDto>>> Update(
        [FromBody] UpdateEmployeeIdFormatDto body, CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.UpdateAsync(body, ctx.TenantId, ctx.OrgId, ctx.UserId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Delete the org's employee ID format settings.</summary>
    /// <remarks><c>id</c> must match the settings' current id from GET /get.</remarks>
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
