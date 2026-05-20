using Microsoft.AspNetCore.Mvc;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Tenant;

namespace ZelosHR.Api.Entities.Employees;

/// <summary>Employee directory, profiles, employment assignment, lifecycle, and soft delete.</summary>
[ApiController]
[ApiExplorerSettings(GroupName = SwaggerGroups.Employees)]
[Route("api/v1/employees")]
[Produces("application/json")]
public class EmployeesController : ControllerBase
{
    private readonly EmployeesService _service;
    private readonly EmployeesDirectoryService _directory;
    private readonly ITenantContextAccessor _tenant;

    public EmployeesController(
        EmployeesService service,
        EmployeesDirectoryService directory,
        ITenantContextAccessor tenant)
    {
        _service = service;
        _directory = directory;
        _tenant = tenant;
    }

    /// <summary>KPI cards on the Employee Directory page.</summary>
    [HttpGet("directory/summary")]
    [ProducesResponseType(typeof(Respons<EmployeeDirectorySummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Respons<EmployeeDirectorySummaryDto>>> DirectorySummary(CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _directory.GetSummaryAsync(ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Employee directory table with search, filters, sort, and pagination.</summary>
    [HttpGet("directory")]
    [ProducesResponseType(typeof(Respons<EmployeeDirectoryListDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Respons<EmployeeDirectoryListDto>>> Directory(
        [FromQuery] string? search,
        [FromQuery] Guid? departmentId,
        [FromQuery] Guid? branchId,
        [FromQuery] string? employmentType,
        [FromQuery] string? status,
        [FromQuery] string sortBy = "name",
        [FromQuery] string sortOrder = "asc",
        [FromQuery] int page = 1,
        [FromQuery] int size = 10,
        [FromQuery] bool includeInactive = false,
        CancellationToken ct = default)
    {
        var ctx = _tenant.Current;
        var query = new EmployeeDirectoryQuery
        {
            Search = search,
            DepartmentId = departmentId,
            BranchId = branchId,
            EmploymentType = employmentType,
            Status = status,
            SortBy = sortBy,
            SortOrder = sortOrder,
            Page = page,
            Size = size,
            IncludeInactive = includeInactive,
        };

        var result = await _directory.GetDirectoryAsync(query, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Filter dropdown options for the directory toolbar.</summary>
    [HttpGet("directory/filter-options")]
    [ProducesResponseType(typeof(Respons<EmployeeFilterOptionsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Respons<EmployeeFilterOptionsDto>>> FilterOptions(CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _directory.GetFilterOptionsAsync(ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Full employee record for profile and HR admin screens.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Respons<EmployeeDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<EmployeeDetailDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Respons<EmployeeDetailDto>>> GetEmployee(Guid id, CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.GetByIdAsync(id, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Respons<CreateEmployeeControllerReadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<CreateEmployeeControllerReadDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Respons<CreateEmployeeControllerReadDto>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Respons<CreateEmployeeControllerReadDto>>> CreateEmployee(
        [FromBody] CreateEmployeeControllerWriteDto data,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            var modelErrors = ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .ToDictionary(e => e.Key, e => e.Value!.Errors[0].ErrorMessage);
            return BadRequest(Respons<CreateEmployeeControllerReadDto>.ValidationError(modelErrors));
        }

        var ctx = _tenant.Current;
        var serviceResult = await _service.CreateEmployeeAsync(
            new CreateEmployeeServiceWriteDto
            {
                FirstName = data.FirstName!,
                MiddleName = data.MiddleName,
                LastName = data.LastName!,
                DateOfBirth = data.DateOfBirth!.Value,
                Gender = data.Gender!,
                Nationality = data.Nationality!,
                GhanaCardNumber = data.GhanaCardNumber!,
                PersonalEmail = data.PersonalEmail!,
                PersonalPhone = data.PersonalPhone!,
                ResidentialAddress = data.ResidentialAddress!,
                GhanaPostGps = data.GhanaPostGps!,
            },
            ctx.TenantId,
            ctx.OrgId,
            ct);

        if (!serviceResult.Success || serviceResult.Data is null)
            return StatusCode(serviceResult.StatusCode, serviceResult);

        var read = new CreateEmployeeControllerReadDto
        {
            EmployeeId = serviceResult.Data.Id.ToString(),
            EmployeeCode = serviceResult.Data.EmployeeCode,
            FirstName = serviceResult.Data.FirstName,
            MiddleName = serviceResult.Data.MiddleName,
            LastName = serviceResult.Data.LastName,
            LifecycleState = serviceResult.Data.LifecycleState,
        };

        return Ok(Respons<CreateEmployeeControllerReadDto>.Ok(read, "Employee created successfully"));
    }

    /// <summary>Update personal / identity fields (partial PATCH).</summary>
    [HttpPatch("{id:guid}")]
    [ProducesResponseType(typeof(Respons<EmployeeDetailDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Respons<EmployeeDetailDto>>> UpdateEmployee(
        Guid id,
        [FromBody] UpdateEmployeeProfileDto data,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            var modelErrors = ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .ToDictionary(e => e.Key, e => e.Value!.Errors[0].ErrorMessage);
            return BadRequest(Respons<EmployeeDetailDto>.ValidationError(modelErrors));
        }

        var ctx = _tenant.Current;
        var result = await _service.UpdateProfileAsync(id, data, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Assign department, branch, manager, contract, and employment status.</summary>
    [HttpPatch("{id:guid}/employment")]
    [ProducesResponseType(typeof(Respons<EmployeeDetailDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Respons<EmployeeDetailDto>>> UpdateEmployment(
        Guid id,
        [FromBody] UpdateEmployeeEmploymentDto data,
        CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.UpdateEmploymentAsync(id, data, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Transition lifecycle state (Pre-hire → Active → Terminated, etc.).</summary>
    [HttpPatch("{id:guid}/lifecycle-state")]
    [ProducesResponseType(typeof(Respons<EmployeeDetailDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Respons<EmployeeDetailDto>>> UpdateLifecycleState(
        Guid id,
        [FromBody] UpdateEmployeeLifecycleStateDto body,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.LifecycleState))
            return BadRequest(Respons<EmployeeDetailDto>.ValidationError(
                new Dictionary<string, string> { ["lifecycleState"] = "Lifecycle state is required." }));

        var ctx = _tenant.Current;
        var result = await _service.UpdateLifecycleStateAsync(
            id, body.LifecycleState!, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Soft-delete employee (sets is_deleted, employment inactive).</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(Respons<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Respons<object>>> DeleteEmployee(Guid id, CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.SoftDeleteAsync(id, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(Respons<GetEmployeesControllerReadDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Respons<GetEmployeesControllerReadDto>>> ListEmployees(CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var serviceResult = await _service.ListEmployeesAsync(ctx.TenantId, ctx.OrgId, ct);
        if (!serviceResult.Success)
            return StatusCode(serviceResult.StatusCode, serviceResult);

        var items = serviceResult.Data?.Items
            .Select(e => new EmployeeListItemControllerReadDto
            {
                EmployeeId = e.Id.ToString(),
                EmployeeCode = e.EmployeeCode,
                FullName = e.FullName,
                LifecycleState = e.LifecycleState,
            })
            .ToList() ?? [];

        return Ok(Respons<GetEmployeesControllerReadDto>.Ok(new GetEmployeesControllerReadDto { Items = items }));
    }
}
