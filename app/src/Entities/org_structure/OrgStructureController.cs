using Microsoft.AspNetCore.Mvc;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Branches;
using ZelosHR.Api.Entities.Departments;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Constants;
using ZelosHR.Api.Shared.Tenant;
using ZelosHR.Api.Shared.Validation;

namespace ZelosHR.Api.Entities.OrgStructure;

/// <summary>Organisation structure — org chart, departments, and branches.</summary>
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

    /// <summary>Organisation tab counts (departments, branches, archived).</summary>
    [HttpGet("statistics")]
    public async Task<ActionResult<Respons<OrganisationSummaryDto>>> Statistics(CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.GetSummaryAsync(ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>List departments (paginated, sortable).</summary>
    [HttpGet("departments/list")]
    [ProducesResponseType(typeof(Respons<DepartmentListDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Respons<DepartmentListDto>>> ListDepartments(
        [FromQuery] string? search,
        [FromQuery]
        [SwaggerAllowedValues(typeof(OrgStructureFieldOptions), nameof(OrgStructureFieldOptions.DepartmentSortBy))]
        string sortBy = "name",
        [FromQuery]
        [SwaggerAllowedValues(typeof(OrgStructureFieldOptions), nameof(OrgStructureFieldOptions.SortOrder))]
        string sortOrder = "asc",
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

    /// <summary>Legacy alias for <c>GET /departments/list</c>.</summary>
    [HttpGet("departments")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public Task<ActionResult<Respons<DepartmentListDto>>> ListDepartmentsLegacy(
        [FromQuery] string? search,
        [FromQuery] string sortBy = "name",
        [FromQuery] string sortOrder = "asc",
        [FromQuery] bool includeArchived = false,
        [FromQuery] int page = 1,
        [FromQuery] int size = 15,
        CancellationToken ct = default) =>
        ListDepartments(search, sortBy, sortOrder, includeArchived, page, size, ct);

    /// <summary>List branches (paginated).</summary>
    [HttpGet("branches/list")]
    [ProducesResponseType(typeof(Respons<BranchListDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Respons<BranchListDto>>> ListBranches(
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

    /// <summary>Legacy alias for <c>GET /branches/list</c>.</summary>
    [HttpGet("branches")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public Task<ActionResult<Respons<BranchListDto>>> ListBranchesLegacy(
        [FromQuery] string? search,
        [FromQuery] bool includeArchived = false,
        [FromQuery] int page = 1,
        [FromQuery] int size = 20,
        CancellationToken ct = default) =>
        ListBranches(search, includeArchived, page, size, ct);

    /// <summary>Reporting-line org chart (CEO → managers with department badges → direct reports).</summary>
    [HttpGet("chart")]
    [ProducesResponseType(typeof(Respons<OrgChartDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Respons<OrgChartDto>>> Chart(CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.GetOrgChartAsync(ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Create a department.</summary>
    [HttpPost("departments/add")]
    public async Task<ActionResult<Respons<CreateDepartmentResponseDto>>> CreateDepartment(
        [FromBody] CreateDepartmentRequestDto body,
        CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.CreateDepartmentAsync(body, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Update a department.</summary>
    [HttpPut("departments/update")]
    public async Task<ActionResult<Respons<CreateDepartmentResponseDto>>> UpdateDepartment(
        [FromQuery(Name = PlatformQueryParams.DepartmentId)] Guid departmentId,
        [FromBody] UpdateDepartmentRequestDto body,
        CancellationToken ct)
    {
        if (QueryParamValidation.BadRequestIfEmptyGuid<CreateDepartmentResponseDto>(
                departmentId, PlatformQueryParams.DepartmentId) is { } missingId)
            return missingId;

        var ctx = _tenant.Current;
        var result = await _service.UpdateDepartmentAsync(departmentId, body, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Permanently delete a department.</summary>
    [HttpDelete("departments/delete")]
    public async Task<ActionResult<Respons<object>>> DeleteDepartment(
        [FromQuery(Name = PlatformQueryParams.DepartmentId)] Guid departmentId,
        CancellationToken ct)
    {
        if (QueryParamValidation.BadRequestIfEmptyGuid<object>(
                departmentId, PlatformQueryParams.DepartmentId) is { } missingId)
            return missingId;

        var ctx = _tenant.Current;
        var result = await _service.DeleteDepartmentAsync(departmentId, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Create a branch.</summary>
    [HttpPost("branches/add")]
    public async Task<ActionResult<Respons<BranchMutationResponseDto>>> CreateBranch(
        [FromBody] CreateBranchRequestDto body,
        CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.CreateBranchAsync(body, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Update a branch.</summary>
    [HttpPut("branches/update")]
    public async Task<ActionResult<Respons<BranchMutationResponseDto>>> UpdateBranch(
        [FromQuery(Name = PlatformQueryParams.BranchId)] Guid branchId,
        [FromBody] UpdateBranchRequestDto body,
        CancellationToken ct)
    {
        if (QueryParamValidation.BadRequestIfEmptyGuid<BranchMutationResponseDto>(
                branchId, PlatformQueryParams.BranchId) is { } missingId)
            return missingId;

        var ctx = _tenant.Current;
        var result = await _service.UpdateBranchAsync(branchId, body, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Permanently delete a branch.</summary>
    [HttpDelete("branches/delete")]
    public async Task<ActionResult<Respons<object>>> DeleteBranch(
        [FromQuery(Name = PlatformQueryParams.BranchId)] Guid branchId,
        CancellationToken ct)
    {
        if (QueryParamValidation.BadRequestIfEmptyGuid<object>(
                branchId, PlatformQueryParams.BranchId) is { } missingId)
            return missingId;

        var ctx = _tenant.Current;
        var result = await _service.DeleteBranchAsync(branchId, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }
}
