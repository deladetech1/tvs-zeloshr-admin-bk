using Microsoft.AspNetCore.Mvc;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Branches;
using ZelosHR.Api.Entities.Departments;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Tenant;

namespace ZelosHR.Api.Entities.OrgStructure;

/// <summary>Departments, branches, org chart — full CRUD (archive on delete).</summary>
[ApiController]
[ApiExplorerSettings(GroupName = SwaggerGroups.Organisation)]
[Route("api/v1/org-structure")]
[Produces("application/json")]
public class OrgStructureController : ControllerBase
{
    private readonly OrgStructureService _service;
    private readonly ITenantContextAccessor _tenant;

    public OrgStructureController(OrgStructureService service, ITenantContextAccessor tenant)
    {
        _service = service;
        _tenant = tenant;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<Respons<OrganisationSummaryDto>>> Summary(CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.GetSummaryAsync(ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("departments")]
    public async Task<ActionResult<Respons<DepartmentListDto>>> Departments(
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

    [HttpGet("branches")]
    public async Task<ActionResult<Respons<BranchListDto>>> Branches(
        [FromQuery] bool includeArchived = false,
        CancellationToken ct = default)
    {
        var ctx = _tenant.Current;
        var result = await _service.ListBranchesAsync(ctx.TenantId, ctx.OrgId, includeArchived, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Hierarchical org chart for "View org chart" (nested departments).</summary>
    [HttpGet("chart")]
    public async Task<ActionResult<Respons<OrgChartDto>>> Chart(CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.GetOrgChartAsync(ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("departments")]
    public async Task<ActionResult<Respons<CreateDepartmentResponseDto>>> CreateDepartment(
        [FromBody] CreateDepartmentRequestDto body,
        CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.CreateDepartmentAsync(body, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("departments/{id:guid}")]
    public async Task<ActionResult<Respons<CreateDepartmentResponseDto>>> UpdateDepartment(
        Guid id, [FromBody] UpdateDepartmentRequestDto body, CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.UpdateDepartmentAsync(id, body, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpDelete("departments/{id:guid}")]
    public async Task<ActionResult<Respons<object>>> ArchiveDepartment(Guid id, CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.ArchiveDepartmentAsync(id, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("branches")]
    public async Task<ActionResult<Respons<BranchMutationResponseDto>>> CreateBranch(
        [FromBody] CreateBranchRequestDto body, CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.CreateBranchAsync(body, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("branches/{id:guid}")]
    public async Task<ActionResult<Respons<BranchMutationResponseDto>>> UpdateBranch(
        Guid id, [FromBody] UpdateBranchRequestDto body, CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.UpdateBranchAsync(id, body, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpDelete("branches/{id:guid}")]
    public async Task<ActionResult<Respons<object>>> ArchiveBranch(Guid id, CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.ArchiveBranchAsync(id, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }
}
