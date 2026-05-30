using Microsoft.AspNetCore.Mvc;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Authorization;
using ZelosHR.Api.Shared.Constants;
using ZelosHR.Api.Shared.Tenant;

namespace ZelosHR.Api.Entities.Employees;

/// <summary>Employee profiles, employment assignment, lifecycle, and soft delete.</summary>
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
    private readonly EmployeeAggregateService _aggregate;
    private readonly EmployeeBulkImportService _bulkImport;
    private readonly ITenantContextAccessor _tenant;

    public EmployeesController(
        EmployeesService service,
        EmployeesDirectoryService directory,
        EmployeeRegistrationService registration,
        EmployeeSubResourcesService subResources,
        EmployeeAggregateService aggregate,
        EmployeeBulkImportService bulkImport,
        ITenantContextAccessor tenant)
    {
        _service = service;
        _directory = directory;
        _registration = registration;
        _subResources = subResources;
        _aggregate = aggregate;
        _bulkImport = bulkImport;
        _tenant = tenant;
    }

    /// <summary>
    /// Create employee in one request (identity, employment, compensation, education[], certifications[], custom fields, document_ids).
    /// </summary>
    /// <remarks>
    /// **Try the Examples dropdown** for full finalised and minimal draft payloads.
    ///
    /// | Step | Action |
    /// |------|--------|
    /// | 1 | (Optional) Define custom fields: `POST /custom-fields/add` |
    /// | 2 | (Optional) Load form schema: `GET /custom-fields/schema?entityType=employee` |
    /// | 3 | (Optional) Upload files: `POST /file/post/multiple` → use IDs in `document_ids` |
    /// | 4 | POST this endpoint with `status: draft` or `finalised` |
    ///
    /// `compensation.currency_id` must reference `core_platform.cp_currencies` (not `"GHS"` string).
    /// </remarks>
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeCreate)]
    [HttpPost("add")]
    [ProducesResponseType(typeof(Respons<EmployeeAggregateReadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<EmployeeAggregateReadDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Respons<EmployeeAggregateReadDto>>> CreateEmployeeAggregate(
        [FromBody] CreateEmployeeAggregateRequest body,
        CancellationToken ct)
    {
        var result = await _aggregate.CreateAsync(body, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Update employee (partial body). Body must include <c>id</c> (employee UUID).
    /// Send only sections/fields to change. Set <c>status</c> to <c>finalised</c> to complete a draft.
    /// Nested <c>education</c>/<c>certifications</c> items: include <c>id</c> to update, omit <c>id</c> to add.
    /// </summary>
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeUpdate)]
    [HttpPut("update")]
    [ProducesResponseType(typeof(Respons<EmployeeAggregateReadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<EmployeeAggregateReadDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Respons<EmployeeAggregateReadDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Respons<EmployeeAggregateReadDto>>> UpdateEmployee(
        [FromBody] UpdateEmployeeAggregateRequest body,
        CancellationToken ct)
    {
        if (body.Id == Guid.Empty)
        {
            return BadRequest(Respons<EmployeeAggregateReadDto>.ValidationError(
                new Dictionary<string, string> { ["id"] = "Employee id is required in the request body." }));
        }

        var result = await _aggregate.UpdateAsync(body.Id, body, ct);
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
    /// (<c>cp_users.fullname</c>, <c>email</c>, <c>contact</c>) is created when you <c>PUT …/update</c> with <c>status: finalised</c>.
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

    /// <summary>Bulk create employees from a CSV file (header row required).</summary>
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeCreate)]
    [HttpPost("bulk")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    [ProducesResponseType(typeof(Respons<EmployeeBulkImportResult>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Respons<EmployeeBulkImportResult>>> BulkImport(
        IFormFile file,
        [FromQuery] string status = "draft",
        CancellationToken ct = default)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(Respons<EmployeeBulkImportResult>.ValidationError(
                new Dictionary<string, string> { ["file"] = "CSV file is required." }));
        }

        await using var stream = file.OpenReadStream();
        var result = await _bulkImport.ImportCsvAsync(stream, status, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Upload profile photo (max 5MB, jpeg/png).</summary>
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeUpdate)]
    [HttpPost("photo/upload")]
    [ProducesResponseType(typeof(Respons<string>), StatusCodes.Status200OK)]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<ActionResult<Respons<string>>> UploadPhoto(
        [FromQuery(Name = PlatformQueryParams.EmployeeId)] Guid employeeId,
        IFormFile file,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(Respons<string>.ValidationError(
                new Dictionary<string, string> { ["file"] = "Photo file is required." }));

        await using var stream = file.OpenReadStream();
        var result = await _registration.UploadProfilePhotoAsync(
            employeeId, stream, file.FileName, file.ContentType, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Upload wizard document (max 10MB, PDF/jpeg/png). Prefer <c>POST /file/post/multiple</c>.</summary>
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeUpdate)]
    [ApiExplorerSettings(IgnoreApi = true)]
    [HttpPost("documents/upload")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<Respons<EmployeeWizardDocumentDto>>> UploadDocument(
        [FromQuery(Name = PlatformQueryParams.EmployeeId)] Guid employeeId,
        IFormFile file,
        [FromForm] string category,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(Respons<EmployeeWizardDocumentDto>.ValidationError(
                new Dictionary<string, string> { ["file"] = "File is required." }));

        await using var stream = file.OpenReadStream();
        var result = await _subResources.UploadDocumentAsync(
            employeeId, category, stream, file.FileName, file.ContentType, file.Length, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Remove uploaded wizard document. Prefer <c>DELETE /file/delete</c>.</summary>
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeUpdate)]
    [ApiExplorerSettings(IgnoreApi = true)]
    [HttpDelete("documents/delete")]
    public async Task<ActionResult<Respons<object>>> DeleteDocument(
        [FromQuery(Name = PlatformQueryParams.EmployeeId)] Guid employeeId,
        [FromQuery(Name = PlatformQueryParams.DocumentId)] Guid documentId,
        CancellationToken ct)
    {
        var result = await _subResources.DeleteDocumentAsync(employeeId, documentId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Employee module statistics (directory KPIs).</summary>
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeGet)]
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(Respons<EmployeeDirectorySummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Respons<EmployeeDirectorySummaryDto>>> Statistics(CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _directory.GetSummaryAsync(ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>KPI cards on the Employee Directory page.</summary>
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeGet)]
    [ApiExplorerSettings(IgnoreApi = true)]
    [HttpGet("directory/summary")]
    [ProducesResponseType(typeof(Respons<EmployeeDirectorySummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Respons<EmployeeDirectorySummaryDto>>> DirectorySummary(CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _directory.GetSummaryAsync(ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Employee record — same aggregate shape as create/update.</summary>
    /// <remarks>
    /// Returns nested sections with `custom_fields` split by section, joined currency metadata
    /// (`currency_code`, `currency_name`, `currency_symbol`, `annualized_cost`), and `document_ids`.
    /// Use `GET /file/list?document_ids=…` to resolve presigned download URLs.
    /// </remarks>
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeGet)]
    [HttpGet("id")]
    [ProducesResponseType(typeof(Respons<EmployeeAggregateReadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<EmployeeAggregateReadDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Respons<EmployeeAggregateReadDto>>> GetEmployeeById(
        [FromQuery(Name = PlatformQueryParams.EmployeeId)] Guid employeeId, CancellationToken ct)
    {
        var result = await _aggregate.GetAsync(employeeId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Legacy one-shot create (split first/last + Ghana Card). Hidden from Swagger — use
    /// <c>POST /api/v1/employees/add</c> or the registration wizard instead.
    /// </summary>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Obsolete("Use POST /api/v1/employees/add or the registration wizard.")]
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeCreate)]
    [HttpPost("legacy")]
    [ProducesResponseType(typeof(Respons<CreateEmployeeControllerReadDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Respons<CreateEmployeeControllerReadDto>>> CreateEmployeeLegacy(
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

    /// <summary>Soft-delete employee (sets is_deleted, employment inactive).</summary>
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeUpdate)]
    [HttpDelete("delete")]
    [ProducesResponseType(typeof(Respons<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Respons<object>>> DeleteEmployee(
        [FromQuery(Name = PlatformQueryParams.EmployeeId)] Guid employeeId, CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.SoftDeleteAsync(employeeId, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Paginated employee list with search and org filters.</summary>
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeGet)]
    [HttpGet("list")]
    [ProducesResponseType(typeof(Respons<EmployeeListDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Respons<EmployeeListDto>>> ListEmployees(
        [FromQuery] EmployeeListQuery query,
        CancellationToken ct = default)
    {
        var result = await _aggregate.ListAsync(query, ct);
        return StatusCode(result.StatusCode, result);
    }
}
