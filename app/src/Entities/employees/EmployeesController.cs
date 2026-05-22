using Microsoft.AspNetCore.Mvc;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Authorization;
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
    private readonly EmployeeRegistrationService _registration;
    private readonly EmployeeSubResourcesService _subResources;
    private readonly ITenantContextAccessor _tenant;

    public EmployeesController(
        EmployeesService service,
        EmployeesDirectoryService directory,
        EmployeeRegistrationService registration,
        EmployeeSubResourcesService subResources,
        ITenantContextAccessor tenant)
    {
        _service = service;
        _directory = directory;
        _registration = registration;
        _subResources = subResources;
        _tenant = tenant;
    }

    /// <summary>Check if email exists in core_platform.cp_users before creating an employee.</summary>
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeGet)]
    [HttpGet("check-user")]
    [ProducesResponseType(typeof(Respons<CpUserCheckResult>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Respons<CpUserCheckResult>>> CheckUser(
        [FromQuery] string email,
        CancellationToken ct)
    {
        var result = await _registration.CheckCpUserAsync(email, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Search cp_users to import into ZelosHR (excludes already linked).</summary>
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeGet)]
    [HttpGet("import/search")]
    [ProducesResponseType(typeof(Respons<IReadOnlyList<CpUserDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Respons<IReadOnlyList<CpUserDto>>>> ImportSearch(
        [FromQuery] string query,
        CancellationToken ct)
    {
        var result = await _registration.ImportSearchAsync(query, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Create a draft employee linked to an existing cp_users row.</summary>
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeCreate)]
    [HttpPost("import")]
    [ProducesResponseType(typeof(Respons<EmployeeRegistrationReadDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Respons<EmployeeRegistrationReadDto>>> Import(
        [FromBody] ImportEmployeeRequest body,
        CancellationToken ct)
    {
        var result = await _registration.ImportAsync(body.UserId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Create employee (registration wizard, step 1). Use <c>fullName</c> here; platform identity
    /// (<c>cp_users.fullname</c>, <c>email</c>, <c>contact</c>) is created on <c>POST …/finalise</c>, not split first/last name.
    /// </summary>
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeCreate)]
    [HttpPost("draft")]
    [ProducesResponseType(typeof(Respons<EmployeeRegistrationReadDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Respons<EmployeeRegistrationReadDto>>> CreateDraft(
        [FromBody] CreateDraftRequest body,
        CancellationToken ct)
    {
        var result = await _registration.CreateDraftAsync(body.FullName, body.ExistingUserId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Update personal/contact (wizard). Identity is written to <c>cp_users</c> when <c>workEmail</c> is set;
    /// HR-only fields (nationality, ID, personal email) stay on <c>zhr_employees</c>.
    /// </summary>
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeUpdate)]
    [HttpPatch("{id:guid}/personal-contact")]
    [ProducesResponseType(typeof(Respons<EmployeeRegistrationReadDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Respons<EmployeeRegistrationReadDto>>> UpdatePersonalContact(
        Guid id,
        [FromBody] CreateEmployeeRequest body,
        CancellationToken ct)
    {
        var result = await _registration.UpdatePersonalContactAsync(id, body, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Update employment details (wizard step 2).</summary>
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeUpdate)]
    [HttpPatch("{id:guid}/employment-details")]
    [ProducesResponseType(typeof(Respons<EmployeeRegistrationReadDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Respons<EmployeeRegistrationReadDto>>> UpdateEmploymentDetails(
        Guid id,
        [FromBody] CreateEmployeeRequest body,
        CancellationToken ct)
    {
        var result = await _registration.UpdateEmploymentDetailsAsync(id, body, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Update compensation and statutory fields (wizard step 4).</summary>
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeUpdate)]
    [HttpPatch("{id:guid}/compensation")]
    [ProducesResponseType(typeof(Respons<EmployeeRegistrationReadDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Respons<EmployeeRegistrationReadDto>>> UpdateCompensation(
        Guid id,
        [FromBody] CreateEmployeeRequest body,
        CancellationToken ct)
    {
        var result = await _registration.UpdateCompensationAsync(id, body, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Finalise draft employee (wizard step 6).</summary>
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeUpdate)]
    [HttpPost("{id:guid}/finalise")]
    [ProducesResponseType(typeof(Respons<EmployeeRegistrationReadDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Respons<EmployeeRegistrationReadDto>>> Finalise(Guid id, CancellationToken ct)
    {
        var result = await _registration.FinaliseAsync(id, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Upload profile photo (max 5MB, jpeg/png).</summary>
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeUpdate)]
    [HttpPost("{id:guid}/photo")]
    [ProducesResponseType(typeof(Respons<string>), StatusCodes.Status200OK)]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<ActionResult<Respons<string>>> UploadPhoto(
        Guid id,
        IFormFile file,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(Respons<string>.ValidationError(
                new Dictionary<string, string> { ["file"] = "Photo file is required." }));

        await using var stream = file.OpenReadStream();
        var result = await _registration.UploadProfilePhotoAsync(
            id, stream, file.FileName, file.ContentType, ct);
        return StatusCode(result.StatusCode, result);
    }

    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeGet)]
    [HttpGet("{id:guid}/education")]
    public async Task<ActionResult<Respons<IReadOnlyList<EmployeeEducationDto>>>> ListEducation(
        Guid id, CancellationToken ct)
    {
        var result = await _subResources.ListEducationAsync(id, ct);
        return StatusCode(result.StatusCode, result);
    }

    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeUpdate)]
    [HttpPost("{id:guid}/education")]
    public async Task<ActionResult<Respons<EmployeeEducationDto>>> AddEducation(
        Guid id, [FromBody] EmployeeEducationWriteDto body, CancellationToken ct)
    {
        var result = await _subResources.AddEducationAsync(id, body, ct);
        return StatusCode(result.StatusCode, result);
    }

    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeUpdate)]
    [HttpPut("{id:guid}/education/{educationId:guid}")]
    public async Task<ActionResult<Respons<EmployeeEducationDto>>> UpdateEducation(
        Guid id, Guid educationId, [FromBody] EmployeeEducationWriteDto body, CancellationToken ct)
    {
        var result = await _subResources.UpdateEducationAsync(id, educationId, body, ct);
        return StatusCode(result.StatusCode, result);
    }

    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeUpdate)]
    [HttpDelete("{id:guid}/education/{educationId:guid}")]
    public async Task<ActionResult<Respons<object>>> DeleteEducation(
        Guid id, Guid educationId, CancellationToken ct)
    {
        var result = await _subResources.DeleteEducationAsync(id, educationId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeGet)]
    [HttpGet("{id:guid}/certifications")]
    public async Task<ActionResult<Respons<IReadOnlyList<EmployeeCertificationDto>>>> ListCertifications(
        Guid id, CancellationToken ct)
    {
        var result = await _subResources.ListCertificationsAsync(id, ct);
        return StatusCode(result.StatusCode, result);
    }

    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeUpdate)]
    [HttpPost("{id:guid}/certifications")]
    public async Task<ActionResult<Respons<EmployeeCertificationDto>>> AddCertification(
        Guid id, [FromBody] EmployeeCertificationWriteDto body, CancellationToken ct)
    {
        var result = await _subResources.AddCertificationAsync(id, body, ct);
        return StatusCode(result.StatusCode, result);
    }

    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeUpdate)]
    [HttpPut("{id:guid}/certifications/{certId:guid}")]
    public async Task<ActionResult<Respons<EmployeeCertificationDto>>> UpdateCertification(
        Guid id, Guid certId, [FromBody] EmployeeCertificationWriteDto body, CancellationToken ct)
    {
        var result = await _subResources.UpdateCertificationAsync(id, certId, body, ct);
        return StatusCode(result.StatusCode, result);
    }

    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeUpdate)]
    [HttpDelete("{id:guid}/certifications/{certId:guid}")]
    public async Task<ActionResult<Respons<object>>> DeleteCertification(
        Guid id, Guid certId, CancellationToken ct)
    {
        var result = await _subResources.DeleteCertificationAsync(id, certId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeGet)]
    [HttpGet("{id:guid}/documents")]
    public async Task<ActionResult<Respons<IReadOnlyList<EmployeeWizardDocumentDto>>>> ListDocuments(
        Guid id, [FromQuery] string? category, CancellationToken ct)
    {
        var result = await _subResources.ListDocumentsAsync(id, category, ct);
        return StatusCode(result.StatusCode, result);
    }

    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeUpdate)]
    [HttpPost("{id:guid}/documents")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<Respons<EmployeeWizardDocumentDto>>> UploadDocument(
        Guid id,
        IFormFile file,
        [FromForm] string category,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(Respons<EmployeeWizardDocumentDto>.ValidationError(
                new Dictionary<string, string> { ["file"] = "File is required." }));

        await using var stream = file.OpenReadStream();
        var result = await _subResources.UploadDocumentAsync(
            id, category, stream, file.FileName, file.ContentType, file.Length, ct);
        return StatusCode(result.StatusCode, result);
    }

    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeUpdate)]
    [HttpDelete("{id:guid}/documents/{documentId:guid}")]
    public async Task<ActionResult<Respons<object>>> DeleteDocument(
        Guid id, Guid documentId, CancellationToken ct)
    {
        var result = await _subResources.DeleteDocumentAsync(id, documentId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>KPI cards on the Employee Directory page.</summary>
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeGet)]
    [HttpGet("directory/summary")]
    [ProducesResponseType(typeof(Respons<EmployeeDirectorySummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Respons<EmployeeDirectorySummaryDto>>> DirectorySummary(CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _directory.GetSummaryAsync(ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Employee directory table with search, filters, sort, and pagination.</summary>
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeGet)]
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
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeGet)]
    [HttpGet("directory/filter-options")]
    [ProducesResponseType(typeof(Respons<EmployeeFilterOptionsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Respons<EmployeeFilterOptionsDto>>> FilterOptions(CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _directory.GetFilterOptionsAsync(ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Full employee record for profile and HR admin screens.</summary>
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeGet)]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Respons<EmployeeDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<EmployeeDetailDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Respons<EmployeeDetailDto>>> GetEmployee(Guid id, CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.GetByIdAsync(id, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Legacy one-shot create (split first/last + Ghana Card). Hidden from Swagger — use
    /// <c>POST /api/v1/employees/draft</c> and the wizard instead (aligns with <c>cp_users</c>).
    /// </summary>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Obsolete("Use POST /api/v1/employees/draft and the registration wizard; identity is stored in cp_users on finalise.")]
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeCreate)]
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
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeUpdate)]
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
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeUpdate)]
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
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeUpdate)]
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
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeUpdate)]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(Respons<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Respons<object>>> DeleteEmployee(Guid id, CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.SoftDeleteAsync(id, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeGet)]
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
