using Microsoft.AspNetCore.Mvc;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Authorization;
using ZelosHR.Api.Shared.Tenant;

namespace ZelosHR.Api.Entities.EmployeeIdFormat;

/// <summary>Employee settings — org-wide employee ID / code generation format.</summary>
[ApiController]
[ApiExplorerSettings(GroupName = SwaggerGroups.EmployeeSettings)]
[Route("api/v1/employee-settings/id-format")]
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

    /// <summary>Get the org's employee ID format settings.</summary>
    /// <remarks>
    /// Always returns 200 for a valid org context. Auto-creates default settings on first GET
    /// (prefix ZEL, 4 digits, starting number 1, hyphen separator, auto-generate on).
    /// Includes <c>next_id_preview</c> for the settings UI.
    /// </remarks>
    [HttpGet("get")]
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeGet)]
    [ProducesResponseType(typeof(Respons<EmployeeIdFormatReadDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Respons<EmployeeIdFormatReadDto>>> Get(CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.GetAsync(ctx.TenantId, ctx.OrgId, ctx.UserId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Create the org's employee ID format settings.</summary>
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
}
