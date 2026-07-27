using Microsoft.AspNetCore.Mvc;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Authorization;
using ZelosHR.Api.Shared.Constants;
using ZelosHR.Api.Shared.Tenant;
using ZelosHR.Api.Shared.Validation;

namespace ZelosHR.Api.Entities.EmployeePortal;

/// <summary>Company settings — employee portal subdomain (e.g. btl.zeloshr.com).</summary>
[ApiController]
[ApiExplorerSettings(GroupName = SwaggerGroups.CompanySettings)]
[Route("api/v1/company/portal-subdomain")]
[Produces("application/json")]
public sealed class EmployeePortalSubdomainController : ControllerBase
{
    private readonly EmployeePortalSubdomainService _service;
    private readonly ITenantContextAccessor _tenant;

    public EmployeePortalSubdomainController(
        EmployeePortalSubdomainService service,
        ITenantContextAccessor tenant)
    {
        _service = service;
        _tenant = tenant;
    }

    /// <summary>List the org's employee portal subdomain settings.</summary>
    /// <remarks>Returns zero or one row for the current org. Does not auto-create defaults.</remarks>
    [HttpGet("list")]
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeGet)]
    [ProducesResponseType(typeof(Respons<EmployeePortalSubdomainListDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Respons<EmployeePortalSubdomainListDto>>> List(CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.ListAsync(ctx.TenantId, ctx.OrgId, ctx.UserId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Get the org's employee portal subdomain settings.</summary>
    /// <remarks>404 when not configured — use POST /add to create.</remarks>
    [HttpGet("get")]
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeGet)]
    [ProducesResponseType(typeof(Respons<EmployeePortalSubdomainReadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<EmployeePortalSubdomainReadDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Respons<EmployeePortalSubdomainReadDto>>> Get(CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.GetAsync(ctx.TenantId, ctx.OrgId, ctx.UserId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Create the org's employee portal subdomain.</summary>
    /// <remarks>
    /// One row per org. Stores <c>bus_id</c> and <c>loc_id</c> from the current session headers.
    /// Subdomain must be globally unique (e.g. <c>btl</c> → <c>btl.zeloshr.com</c>).
    /// </remarks>
    [HttpPost("add")]
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeUpdate)]
    [ProducesResponseType(typeof(Respons<EmployeePortalSubdomainReadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<EmployeePortalSubdomainReadDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Respons<EmployeePortalSubdomainReadDto>>> Add(
        [FromBody] CreateEmployeePortalSubdomainDto body, CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.CreateAsync(
            body, ctx.TenantId, ctx.OrgId, ctx.BusId, ctx.LocId, ctx.UserId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Update the org's employee portal subdomain.</summary>
    /// <remarks>Full replacement. Refreshes <c>bus_id</c> and <c>loc_id</c> from current session headers.</remarks>
    [HttpPut("update")]
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeUpdate)]
    [ProducesResponseType(typeof(Respons<EmployeePortalSubdomainReadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<EmployeePortalSubdomainReadDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Respons<EmployeePortalSubdomainReadDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Respons<EmployeePortalSubdomainReadDto>>> Update(
        [FromBody] UpdateEmployeePortalSubdomainDto body, CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.UpdateAsync(
            body, ctx.TenantId, ctx.OrgId, ctx.BusId, ctx.LocId, ctx.UserId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Delete the org's employee portal subdomain.</summary>
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
