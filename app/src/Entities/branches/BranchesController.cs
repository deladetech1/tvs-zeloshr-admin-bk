using Microsoft.AspNetCore.Mvc;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Tenant;

namespace ZelosHR.Api.Entities.Branches;

/// <summary>Legacy branches list — prefer <c>/api/v1/org-structure/branches</c> for writes.</summary>
[ApiController]
[ApiExplorerSettings(GroupName = SwaggerGroups.OrganisationLegacy)]
[Route("api/v1/branches")]
[Produces("application/json")]
public class BranchesController : ControllerBase
{
    private readonly BranchesService _service;
    private readonly ITenantContextAccessor _tenant;

    public BranchesController(BranchesService service, ITenantContextAccessor tenant)
    {
        _service = service;
        _tenant = tenant;
    }

    [HttpGet]
    [ProducesResponseType(typeof(Respons<BranchListDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Respons<BranchListDto>>> List(
        [FromQuery] bool includeArchived = false,
        CancellationToken ct = default)
    {
        var ctx = _tenant.Current;
        var result = await _service.ListBranchesAsync(ctx.TenantId, ctx.OrgId, includeArchived, ct);
        return StatusCode(result.StatusCode, result);
    }
}
