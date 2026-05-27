using Microsoft.AspNetCore.Mvc;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Branches;
using ZelosHR.Api.Entities.Departments;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Constants;
using ZelosHR.Api.Shared.Tenant;

namespace ZelosHR.Api.Entities.OrgStructure;

[ApiController]
[ApiExplorerSettings(GroupName = SwaggerGroups.Organisation, IgnoreApi = true)]
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

    [HttpGet("statistics")]
    public async Task<ActionResult<Respons<OrganisationSummaryDto>>> Statistics(CancellationToken ct)
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
        [FromQuery] string? search,
        [FromQuery] bool includeArchived = false,
        [FromQuery] int page = 1,
        [FromQuery] int size = 20,
        CancellationToken ct = default)
    {
        var ctx = _tenant.Current;
        var result = await _service.ListBranchesAsync(
            search, includeArchived, page, size, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("chart")]
    public async Task<ActionResult<Respons<OrgChartDto>>> Chart(CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.GetOrgChartAsync(ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("departments/add")]
    public async Task<ActionResult<Respons<CreateDepartmentResponseDto>>> CreateDepartment(
        [FromBody] CreateDepartmentRequestDto body,
        CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.CreateDepartmentAsync(body, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("departments/update")]
    public async Task<ActionResult<Respons<CreateDepartmentResponseDto>>> UpdateDepartment(
        [FromQuery(Name = PlatformQueryParams.DepartmentId)] Guid departmentId,
        [FromBody] UpdateDepartmentRequestDto body,
        CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.UpdateDepartmentAsync(departmentId, body, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpDelete("departments/delete")]
    public async Task<ActionResult<Respons<object>>> ArchiveDepartment(
        [FromQuery(Name = PlatformQueryParams.DepartmentId)] Guid departmentId,
        CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.ArchiveDepartmentAsync(departmentId, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("branches/add")]
    public async Task<ActionResult<Respons<BranchMutationResponseDto>>> CreateBranch(
        [FromBody] CreateBranchRequestDto body,
        CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.CreateBranchAsync(body, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("branches/update")]
    public async Task<ActionResult<Respons<BranchMutationResponseDto>>> UpdateBranch(
        [FromQuery(Name = PlatformQueryParams.BranchId)] Guid branchId,
        [FromBody] UpdateBranchRequestDto body,
        CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.UpdateBranchAsync(branchId, body, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpDelete("branches/delete")]
    public async Task<ActionResult<Respons<object>>> ArchiveBranch(
        [FromQuery(Name = PlatformQueryParams.BranchId)] Guid branchId,
        CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.ArchiveBranchAsync(branchId, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }
}
