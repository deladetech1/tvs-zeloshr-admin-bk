using Microsoft.AspNetCore.Mvc;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Tenant;

namespace ZelosHR.Api.Entities.Departments;

/// <summary>Legacy departments list — prefer <c>/api/v1/org-structure</c> for writes.</summary>
[ApiController]
[ApiExplorerSettings(GroupName = SwaggerGroups.OrganisationLegacy)]
[Route("api/v1/departments")]
[Produces("application/json")]
public class DepartmentsController : ControllerBase
{
    private readonly DepartmentsService _service;
    private readonly ITenantContextAccessor _tenant;

    public DepartmentsController(DepartmentsService service, ITenantContextAccessor tenant)
    {
        _service = service;
        _tenant = tenant;
    }

    [HttpGet("summary")]
    [ProducesResponseType(typeof(Respons<OrganisationSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Respons<OrganisationSummaryDto>>> Summary(CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.GetSummaryAsync(ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(Respons<DepartmentListDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Respons<DepartmentListDto>>> List(
        [FromQuery] string? search,
        [FromQuery] string sortBy = "name",
        [FromQuery] string sortOrder = "asc",
        [FromQuery] bool includeArchived = false,
        [FromQuery] int page = 1,
        [FromQuery] int size = 15,
        CancellationToken ct = default)
    {
        var ctx = _tenant.Current;
        var result = await _service.ListDepartmentsAsync(
            search, sortBy, sortOrder, includeArchived, page, size, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }
}
