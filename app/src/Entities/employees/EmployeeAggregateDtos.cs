using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.EmploymentTypes;
using ZelosHR.Api.Entities.Files;
using ZelosHR.Api.Shared.Constants;

namespace ZelosHR.Api.Entities.Employees;

/// <summary>
/// One-shot employee create. Required on create: <c>identity.full_name</c> and <c>identity.phone</c>.
/// When <c>identity.work_email</c> is set, registration finalises and links <c>cp_users</c>.
/// All <c>employment</c> fields are optional; when <c>department_id</c> or <c>branch_id</c> is set, the ID must exist.
/// <c>work_arrangement: remote</c> with a <c>branch_id</c> is rejected (inconsistent data).
/// </summary>
/// <remarks>
/// **Custom fields:** Define schema first via <c>POST /api/v1/custom-fields/add</c> with
/// <c>section_name</c> = <c>employee-directory-identity</c> | <c>employee-directory-employment</c> | etc.
/// Load via <c>GET /api/v1/custom-fields/schema?entityType=employee</c>, then pass values under each section's
/// <c>custom_fields</c> object (keys = <c>field_key</c>).
///
/// **Documents:** Upload via <c>POST /api/v1/file/post/multiple</c>, pass returned IDs in <c>document_ids</c>.
///
/// **Currency:** Use <c>compensation.currency_id</c> (FK to <c>core_platform.cp_currencies</c>), not a currency code.
///
/// See operation **Examples** dropdown for <c>full_profile</c> and <c>minimal</c> payloads.
/// </remarks>
public sealed class CreateEmployeeAggregateRequest
{
    public EmployeeAggregateIdentityDto Identity { get; init; } = new();
    public EmployeeAggregateEmploymentDto? Employment { get; init; }
    public EmployeeAggregateCompensationDto? Compensation { get; init; }
    public IReadOnlyList<EmployeeEducationWriteDto> Education { get; init; } = [];
    public IReadOnlyList<EmployeeCertificationWriteDto> Certifications { get; init; } = [];

    /// <summary>
    /// Document IDs from <c>POST /api/v1/file/post/multiple</c> (upload first, then pass IDs here).
    /// On read, <c>GET /employees/get</c> returns <c>documents[]</c> (MyStoreGuard <c>DocumentReadDto</c>: <c>doc_id</c>, <c>name</c>, <c>presigned_url</c>, <c>description</c>).
    /// </summary>
    public IReadOnlyList<string>? DocumentIds { get; init; }
}

/// <summary>Partial employee update — only include sections/fields to change.</summary>
/// <remarks>
/// Pass <c>employee_id</c> on the query string (UUID from <c>GET /employees/get?employee_id=</c>).
///
/// **education[] / certifications[] (default, sync false):** patch the list — include <c>id</c> from GET to update;
/// omit <c>id</c> to add. Client-generated UUIDs on new rows are treated as add (only ids already on the employee update).
/// Rows you omit are **unchanged**.
///
/// **sync_education / sync_certifications (true):** replace the list — the array you send is the **full desired set**;
/// any existing row not listed is **deleted**. Send <c>[]</c> with sync true to clear the section.
///
/// **delete_education_ids / delete_certification_ids:** remove specific rows by UUID without sending the array.
/// </remarks>
public sealed class UpdateEmployeeAggregateRequest
{
    public EmployeeAggregateIdentityDto? Identity { get; init; }
    public EmployeeAggregateEmploymentDto? Employment { get; init; }
    public EmployeeAggregateCompensationDto? Compensation { get; init; }

    public IReadOnlyList<EmployeeEducationUpsertDto>? Education { get; init; }
    public IReadOnlyList<EmployeeCertificationUpsertDto>? Certifications { get; init; }

    /// <summary>
    /// Default <c>false</c>: patch <c>education[]</c> (upsert sent rows only; others unchanged).
    /// <c>true</c> + <c>education[]</c>: replace section — array is the full desired set; unlisted rows deleted.
    /// </summary>
    public bool SyncEducation { get; init; }

    /// <summary>
    /// Default <c>false</c>: patch <c>certifications[]</c> (upsert sent rows only; others unchanged).
    /// <c>true</c> + <c>certifications[]</c> (including <c>[]</c>): replace section; unlisted rows deleted.
    /// </summary>
    public bool SyncCertifications { get; init; }

    public IReadOnlyList<Guid>? DeleteEducationIds { get; init; }
    public IReadOnlyList<Guid>? DeleteCertificationIds { get; init; }

    /// <inheritdoc cref="CreateEmployeeAggregateRequest.DocumentIds"/>
    public IReadOnlyList<string>? DocumentIds { get; init; }

    /// <summary>Remove file-registry document IDs (see also <see cref="DocumentIds"/>).</summary>
    public IReadOnlyList<string>? DeleteDocumentIds { get; init; }
}

public sealed class EmployeeAggregateIdentityDto
{
    /// <summary>Display name. Required on create.</summary>
    public string FullName { get; init; } = string.Empty;

    public DateOnly? DateOfBirth { get; init; }

    [SwaggerAllowedValues(typeof(EmployeeFieldOptions), nameof(EmployeeFieldOptions.Genders))]
    public string? Gender { get; init; }

    /// <summary>Country of citizenship (e.g. <c>Ghana</c>). Wire: <c>country</c>.</summary>
    public string? Country { get; init; }

    [SwaggerAllowedValues(typeof(EmployeeFieldOptions), nameof(EmployeeFieldOptions.IdTypes),
        Description = "Suggested values; free text is accepted.")]
    public string? IdType { get; init; }

    public DateOnly? IdIssueDate { get; init; }
    public DateOnly? IdExpiryDate { get; init; }
    public string? IdNumber { get; init; }
    public string? PersonalEmail { get; init; }
    public string? WorkEmail { get; init; }

    /// <summary>Contact number. Required on create (<c>POST /add</c>).</summary>
    public string? Phone { get; init; }
    public string? LinkedInUrl { get; init; }
    public string? ResidentialAddress { get; init; }

    /// <summary>
    /// **Write:** document id string from file upload (or read object with <c>doc_id</c> / <c>id</c> on round-trip update).
    /// **Read:** see <see cref="EmployeeAggregateIdentityReadDto.ProfileUrl"/>.
    /// </summary>
    [JsonConverter(typeof(ProfileUrlWriteJsonConverter))]
    public string? ProfileUrl { get; init; }

    /// <summary>Custom field **values** for <c>section_name = employee-directory-identity</c>.</summary>
    public Dictionary<string, string?>? CustomFields { get; init; }
}

/// <summary>Identity section on employee read — <c>profile_url</c> includes presigned URL metadata.</summary>
public sealed class EmployeeAggregateIdentityReadDto
{
    public string FullName { get; init; } = string.Empty;
    public DateOnly? DateOfBirth { get; init; }

    [SwaggerAllowedValues(typeof(EmployeeFieldOptions), nameof(EmployeeFieldOptions.Genders))]
    public string? Gender { get; init; }

    public string? Country { get; init; }

    [SwaggerAllowedValues(typeof(EmployeeFieldOptions), nameof(EmployeeFieldOptions.IdTypes),
        Description = "Suggested values; free text is accepted.")]
    public string? IdType { get; init; }

    public DateOnly? IdIssueDate { get; init; }
    public DateOnly? IdExpiryDate { get; init; }
    public string? IdNumber { get; init; }
    public string? PersonalEmail { get; init; }
    public string? WorkEmail { get; init; }
    public string? Phone { get; init; }
    public string? LinkedInUrl { get; init; }
    public string? ResidentialAddress { get; init; }

    /// <summary>Profile photo (<c>DocumentReadDto</c>): <c>doc_id</c>, <c>name</c>, <c>presigned_url</c> (~24h), <c>description</c>.</summary>
    public DocumentReadDto? ProfileUrl { get; init; }

    public Dictionary<string, string?>? CustomFields { get; init; }
}

public class EmployeeAggregateEmploymentDto
{
    public string? JobTitle { get; init; }
    public Guid? DepartmentId { get; init; }
    public Guid? BranchId { get; init; }

    /// <summary>FK from <c>GET /employment-types/list</c>. Denormalised name is stored on the employee row.</summary>
    public Guid? EmploymentTypeId { get; init; }

    [SwaggerAllowedValues(typeof(EmployeeFieldOptions), nameof(EmployeeFieldOptions.EmploymentStatuses))]
    public string? EmploymentStatus { get; init; }

    [SwaggerAllowedValues(typeof(EmployeeFieldOptions), nameof(EmployeeFieldOptions.ContractTypes))]
    public string? ContractType { get; init; }

    [SwaggerAllowedValues(typeof(EmployeeFieldOptions), nameof(EmployeeFieldOptions.WorkArrangements),
        Description = "How the employee works (not the site name — use work_location for that).")]
    public string? WorkArrangement { get; init; }

    public string? WorkLocation { get; init; }
    public string? PayGrade { get; init; }
    public DateOnly? StartDate { get; init; }
    public DateOnly? ProbationEndDate { get; init; }
    public string? WorkingHours { get; init; }
    public string? NoticePeriod { get; init; }
    public Guid? ReportsToId { get; init; }
    public Guid? DottedLineManagerId { get; init; }

    /// <summary>Custom field values for <c>section_name = employee-directory-employment</c>.</summary>
    public Dictionary<string, string?>? CustomFields { get; init; }
}

/// <summary>Nested reports-to manager on employee read (<c>employment.reports_to</c>).</summary>
public sealed class EmployeeReportsToRefDto
{
    public required string Id { get; init; }
    public string? Name { get; init; }
    public string? Position { get; init; }

    /// <summary>Profile photo (<c>DocumentReadDto</c>) — same shape as <c>identity.profile_url</c>.</summary>
    public DocumentReadDto? PhotoUrl { get; init; }
}

/// <summary>Compensation on write. Read response adds <c>annualized_cost</c> and joined currency metadata.</summary>
public class EmployeeAggregateCompensationDto
{
    public decimal? GrossSalary { get; init; }

    [SwaggerAllowedValues(typeof(EmployeeFieldOptions), nameof(EmployeeFieldOptions.PayFrequencies),
        Description = "Annualized cost uses Monthly × 12 | Bi-weekly × 26 | Weekly × 52 | Annual × 1.")]
    public string? PayFrequency { get; init; }

    /// <summary>FK to <c>core_platform.cp_currencies.id</c> (tenant-scoped, seeded). Not a currency code.</summary>
    public string? CurrencyId { get; init; }

    /// <summary>Custom field values for <c>section_name = employee-directory-compensation</c>.</summary>
    public Dictionary<string, string?>? CustomFields { get; init; }
}

public sealed class EmployeeAggregateReadDto
{
    public required Guid Id { get; init; }
    public required string EmployeeCode { get; init; }
    public string? UserId { get; init; }
    public EmployeeAggregateIdentityReadDto Identity { get; init; } = new();
    public EmployeeAggregateEmploymentReadDto? Employment { get; init; }
    public EmployeeAggregateCompensationReadDto? Compensation { get; init; }
    public IReadOnlyList<EmployeeEducationDto>? Education { get; init; }
    public IReadOnlyList<EmployeeCertificationDto>? Certifications { get; init; }

    /// <summary>Attached files. **Read:** MyStoreGuard <c>DocumentReadDto</c> per item (<c>doc_id</c>, <c>name</c>, <c>presigned_url</c>, <c>description</c>).
    /// **Write** (create/update): pass registry ID strings in <c>document_ids</c> from <c>POST /file/post/multiple</c>.
    /// </summary>
    public IReadOnlyList<DocumentReadDto>? Documents { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public string? CreatedById { get; init; }
    public string? UpdatedById { get; init; }
    public string? CreatedBy { get; init; }
    public string? UpdatedBy { get; init; }
}

public sealed class EmployeeAggregateEmploymentReadDto : EmployeeAggregateEmploymentDto
{
    public string? DepartmentName { get; init; }
    public string? BranchName { get; init; }

    /// <summary>Write-only FK — read uses <see cref="EmploymentType"/> (<c>employment_type.id</c>).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public new Guid? EmploymentTypeId { get; init; }

    /// <summary>Employment type reference (<c>employment_type.id</c> matches write <c>employment_type_id</c>).</summary>
    public EmployeeEmploymentTypeRefDto? EmploymentType { get; init; }

    /// <summary>Write-only FK — read uses <see cref="ReportsTo"/> (<c>reports_to.id</c>).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public new Guid? ReportsToId { get; init; }

    /// <summary>Manager reference — write uses flat <c>reports_to_id</c>.</summary>
    public EmployeeReportsToRefDto? ReportsTo { get; init; }
}

public sealed class EmployeeAggregateCompensationReadDto : EmployeeAggregateCompensationDto
{
    public decimal? AnnualizedCost { get; init; }
    public string? CurrencyCode { get; init; }
    public string? CurrencyName { get; init; }
    public string? CurrencySymbol { get; init; }
}

/// <summary>Query filters for <c>GET /api/v1/employees/list</c> — matches frontend <c>EmployeeParams</c>.</summary>
public sealed class EmployeeListQuery
{
    [FromQuery(Name = PlatformQueryParams.Search)]
    public string? Search { get; init; }

    [FromQuery(Name = PlatformQueryParams.EmploymentStatus)]
    [SwaggerAllowedValues(typeof(EmployeeFieldOptions), nameof(EmployeeFieldOptions.EmploymentStatuses),
        Description = "Exact match on stored employment_status (Draft, Active, Probation, …).")]
    public string? EmploymentStatus { get; init; }

    [FromQuery(Name = PlatformQueryParams.Status)]
    [SwaggerAllowedValues(typeof(EmployeeFieldOptions), nameof(EmployeeFieldOptions.ListStatusFilters),
        Description = "Smart filter using simple commands (active, probation, on_leave, …). Ignored when employment_status is set.")]
    public string? Status { get; init; }

    [FromQuery(Name = PlatformQueryParams.DepartmentId)]
    public Guid? DepartmentId { get; init; }

    [FromQuery(Name = PlatformQueryParams.BranchId)]
    public Guid? BranchId { get; init; }

    [FromQuery(Name = PlatformQueryParams.EmploymentType)]
    [SwaggerAllowedValues(typeof(EmployeeFieldOptions), nameof(EmployeeFieldOptions.EmploymentTypes))]
    public string? EmploymentType { get; init; }

    [FromQuery(Name = PlatformQueryParams.WorkLocation)]
    public string? WorkLocation { get; init; }

    /// <summary>Employment start on or after this date (<c>YYYY-MM-DD</c>).</summary>
    [FromQuery(Name = PlatformQueryParams.StartDate)]
    public DateOnly? StartDate { get; init; }

    /// <summary>Employment start on or before this date (<c>YYYY-MM-DD</c>).</summary>
    [FromQuery(Name = PlatformQueryParams.EndDate)]
    public DateOnly? EndDate { get; init; }

    [SwaggerAllowedValues(typeof(EmployeeFieldOptions), nameof(EmployeeFieldOptions.ListSortBy))]
    [FromQuery(Name = PlatformQueryParams.SortBy)]
    public string SortBy { get; init; } = "name";

    [SwaggerAllowedValues(typeof(EmployeeFieldOptions), nameof(EmployeeFieldOptions.SortOrder))]
    [FromQuery(Name = PlatformQueryParams.SortOrder)]
    public string SortOrder { get; init; } = "asc";

    [FromQuery(Name = PlatformQueryParams.Page)]
    public int Page { get; init; } = 1;

    [FromQuery(Name = PlatformQueryParams.Size)]
    public int Size { get; init; } = 20;

    [FromQuery(Name = PlatformQueryParams.IncludeInactive)]
    public bool IncludeInactive { get; init; }
}

public sealed class EmployeeListDto
{
    public IReadOnlyList<EmployeeListItemDto> Items { get; init; } = [];
}

public sealed class EmployeeListItemDto
{
    public required string EmployeeId { get; init; }
    public required string EmployeeCode { get; init; }
    public required string FullName { get; init; }
    public string? JobTitle { get; init; }
    public string? DepartmentName { get; init; }
    public string? BranchName { get; init; }
    public string? WorkLocation { get; init; }

    [SwaggerAllowedValues(typeof(EmployeeFieldOptions), nameof(EmployeeFieldOptions.EmploymentStatuses))]
    public string? EmploymentStatus { get; init; }

    [SwaggerAllowedValues(typeof(EmployeeFieldOptions), nameof(EmployeeFieldOptions.Engagements))]
    public string? Engagement { get; init; }

    public IReadOnlyList<string> WorkStates { get; init; } = [];

    [SwaggerAllowedValues(typeof(EmployeeFieldOptions), nameof(EmployeeFieldOptions.EmploymentTypes))]
    public string? EmploymentType { get; init; }
    public DocumentReadDto? ProfileUrl { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public string? CreatedById { get; init; }
    public string? UpdatedById { get; init; }
    public string? CreatedBy { get; init; }
    public string? UpdatedBy { get; init; }
}
