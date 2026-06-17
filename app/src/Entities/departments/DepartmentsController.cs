using Microsoft.AspNetCore.Mvc;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.OrgStructure;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Tenant;

namespace ZelosHR.Api.Entities.Departments;

/// <summary>Legacy departments list — prefer <c>/api/v1/org-structure/departments/list</c>.</summary>
[ApiController]
[ApiExplorerSettings(GroupName = SwaggerGroups.OrganisationLegacy, IgnoreApi = true)]
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

    [HttpGet("statistics")]
    [ProducesResponseType(typeof(Respons<OrganisationSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Respons<OrganisationSummaryDto>>> Statistics(CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.GetSummaryAsync(ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [ApiExplorerSettings(IgnoreApi = true)]
    [HttpGet("summary")]
    public Task<ActionResult<Respons<OrganisationSummaryDto>>> Summary(CancellationToken ct) =>
        Statistics(ct);

    [HttpGet("list")]
    [ProducesResponseType(typeof(Respons<DepartmentListDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Respons<DepartmentListDto>>> List(
        [FromQuery] OrgStructureListQuery query,
        CancellationToken ct = default)
    {
        var ctx = _tenant.Current;
        var result = await _service.ListDepartmentsAsync(
            query.Search,
            query.SortBy,
            query.SortOrder,
            query.IncludeArchived,
            query.Page,
            query.Size,
            ctx.TenantId,
            ctx.OrgId,
            ct);
        return StatusCode(result.StatusCode, result);
    }
}
