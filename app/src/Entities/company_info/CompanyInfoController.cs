using Microsoft.AspNetCore.Mvc;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Authorization;
using ZelosHR.Api.Shared.Constants;
using ZelosHR.Api.Shared.Tenant;
using ZelosHR.Api.Shared.Validation;

namespace ZelosHR.Api.Entities.CompanyInfo;

/// <summary>Company settings — legal/registration profile and office locations for the org.</summary>
[ApiController]
[ApiExplorerSettings(GroupName = SwaggerGroups.CompanySettings)]
[Route("api/v1/company/info")]
[Produces("application/json")]
public class CompanyInfoController : ControllerBase
{
    private readonly CompanyInfoService _service;
    private readonly ITenantContextAccessor _tenant;

    public CompanyInfoController(CompanyInfoService service, ITenantContextAccessor tenant)
    {
        _service = service;
        _tenant = tenant;
    }

    /// <summary>Get the company profile, with offices embedded.</summary>
    /// <remarks>Returns every office for the org in <c>offices[]</c> — no pagination, no separate list call.</remarks>
    [HttpGet("get")]
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeGet)]
    [ProducesResponseType(typeof(Respons<CompanyInfoReadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<CompanyInfoReadDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Respons<CompanyInfoReadDto>>> Get(CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.GetAsync(ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Create the company profile for this organisation.</summary>
    /// <remarks>
    /// One profile per org. Body: legal_name (required) plus optional registration/contact
    /// fields, logo_url/banner_url (document ids from POST /file/post/multiple), and an optional
    /// initial offices[] array. 400 if a profile already exists — use PUT /update instead.
    /// </remarks>
    [HttpPost("add")]
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeUpdate)]
    [ProducesResponseType(typeof(Respons<CompanyInfoReadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<CompanyInfoReadDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Respons<CompanyInfoReadDto>>> Add(
        [FromBody] CreateCompanyInfoDto body, CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.CreateAsync(body, ctx.TenantId, ctx.OrgId, ctx.UserId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Update the company profile.</summary>
    /// <remarks>
    /// Partial body — only supplied fields change. An optional offices[] array replaces the
    /// org's entire office list (diffed server-side): entries with no office_id are created,
    /// entries matching an existing office replace its fields in full, and existing offices not
    /// present in the array are deleted. Omit offices entirely to leave them untouched.
    /// </remarks>
    [HttpPut("update")]
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeUpdate)]
    [ProducesResponseType(typeof(Respons<CompanyInfoReadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<CompanyInfoReadDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Respons<CompanyInfoReadDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Respons<CompanyInfoReadDto>>> Update(
        [FromBody] UpdateCompanyInfoDto body, CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.UpdateAsync(body, ctx.TenantId, ctx.OrgId, ctx.UserId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Delete the company profile.</summary>
    /// <remarks>Also deletes every office for this org, in one transaction.</remarks>
    [HttpDelete("delete")]
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeUpdate)]
    [ProducesResponseType(typeof(Respons<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Respons<object>>> Delete(CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.DeleteAsync(ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Update a single office in place.</summary>
    /// <remarks>
    /// Partial body: name, country, city, phone, is_head_office — only supplied fields change.
    /// Use this for one-field edits instead of resending the full offices[] array on PUT /update.
    /// </remarks>
    [HttpPut("offices/update")]
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeUpdate)]
    [ProducesResponseType(typeof(Respons<CompanyOfficeReadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<CompanyOfficeReadDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Respons<CompanyOfficeReadDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Respons<CompanyOfficeReadDto>>> UpdateOffice(
        [FromQuery(Name = PlatformQueryParams.OfficeId)] Guid officeId,
        [FromBody] UpdateCompanyOfficeDto body,
        CancellationToken ct)
    {
        if (QueryParamValidation.BadRequestIfEmptyGuid<CompanyOfficeReadDto>(
                officeId, PlatformQueryParams.OfficeId) is { } missingId)
            return missingId;

        var ctx = _tenant.Current;
        var result = await _service.UpdateOfficeAsync(officeId, body, ctx.TenantId, ctx.OrgId, ctx.UserId, ct);
        return StatusCode(result.StatusCode, result);
    }
}
