using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.EmploymentTypes;
using ZelosHR.Api.Entities.Files;
using ZelosHR.Api.Shared.Constants;

namespace ZelosHR.Api.Entities.Employees;

/// <summary>
/// One-shot employee create. Required on create: <c>identity.full_name</c> and <c>identity.phone</c>.
/// When <c>identity.work_email</c> is set and <c>is_draft</c> is false (default), registration finalises and links <c>cp_users</c>.
/// Set <c>is_draft: true</c> to save as draft even when a work email is supplied.
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
/// **Identifications:** <c>identity.identifications[]</c> — each row uses <c>id_card_type_id</c> from
/// <c>GET /api/v1/id-card-types/list</c> plus <c>id_card_type_number</c> and optional <c>id_card_type_issue_date</c> / <c>id_card_type_expiry_date</c>.
///
/// **Currency:** Use <c>compensation.currency_id</c> (FK to <c>core_platform.cp_currencies</c>), not a currency code.
///
/// See operation **Examples** dropdown for <c>full_profile</c> and <c>minimal</c> payloads.
/// </remarks>
public sealed class CreateEmployeeAggregateRequest
{
    /// <summary>When true, saves the record as a draft regardless of whether work_email is supplied.</summary>
    public bool IsDraft { get; init; }

    public EmployeeAggregateIdentityDto Identity { get; init; } = new();
    public EmployeeAggregateEmploymentDto? Employment { get; init; }
    public EmployeeAggregateCompensationDto? Compensation { get; init; }
    public IReadOnlyList<EmployeeEducationWriteDto> Education { get; init; } = [];
    public IReadOnlyList<EmployeeCertificationWriteDto> Certifications { get; init; } = [];
    public EmployeeMedicalWriteDto? Medical { get; init; }
    public IReadOnlyList<EmployeeSkillWriteDto> Skills { get; init; } = [];
    public IReadOnlyList<EmployeeExperienceWriteDto> Experiences { get; init; } = [];
    public IReadOnlyList<EmployeeReferralWriteDto> Referrals { get; init; } = [];

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
///
/// **identity.identifications[] (default, sync_identifications false):** patch the list — include <c>id</c> from GET to update;
/// omit <c>id</c> to add. Rows you omit are **unchanged**.
///
/// **sync_identifications (true):** replace <c>identity.identifications</c> — the array you send is the **full desired set**;
/// any existing row not listed is **deleted**. Send <c>[]</c> with sync true to clear all identifications.
///
/// **delete_identification_ids:** remove specific identification rows by UUID without sending the array.
/// </remarks>
public sealed class UpdateEmployeeAggregateRequest
{
    /// <summary>When true, suppresses automatic finalization even if work_email is supplied. Use for explicit draft saves.</summary>
    public bool IsDraft { get; init; }

    public EmployeeAggregateIdentityDto? Identity { get; init; }
    public EmployeeAggregateEmploymentDto? Employment { get; init; }
    public EmployeeAggregateCompensationDto? Compensation { get; init; }

    public IReadOnlyList<EmployeeEducationUpsertDto>? Education { get; init; }
    public IReadOnlyList<EmployeeCertificationUpsertDto>? Certifications { get; init; }
    public EmployeeMedicalWriteDto? Medical { get; init; }
    public IReadOnlyList<EmployeeSkillUpsertDto>? Skills { get; init; }
    public IReadOnlyList<EmployeeExperienceUpsertDto>? Experiences { get; init; }
    public IReadOnlyList<EmployeeReferralUpsertDto>? Referrals { get; init; }

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

    /// <summary>
    /// Default <c>false</c>: patch <c>identity.identifications[]</c> (upsert sent rows only; others unchanged).
    /// <c>true</c> + <c>identity.identifications[]</c>: replace section — array is the full desired set; unlisted rows deleted.
    /// </summary>
    public bool SyncIdentifications { get; init; }

    public IReadOnlyList<Guid>? DeleteIdentificationIds { get; init; }

    public bool SyncEmergency { get; init; }
    public IReadOnlyList<Guid>? DeleteEmergencyIds { get; init; }

    public bool SyncPayment { get; init; }
    public IReadOnlyList<Guid>? DeletePaymentIds { get; init; }

    public bool SyncMedicalConditions { get; init; }
    public IReadOnlyList<Guid>? DeleteMedicalConditionIds { get; init; }

    public bool SyncAllergies { get; init; }
    public IReadOnlyList<Guid>? DeleteAllergyIds { get; init; }

    public bool SyncMedications { get; init; }
    public IReadOnlyList<Guid>? DeleteMedicationIds { get; init; }

    public bool SyncSkills { get; init; }
    public IReadOnlyList<Guid>? DeleteSkillIds { get; init; }

    public bool SyncExperiences { get; init; }
    public IReadOnlyList<Guid>? DeleteExperienceIds { get; init; }

    public bool SyncReferrals { get; init; }
    public IReadOnlyList<Guid>? DeleteReferralIds { get; init; }

    /// <inheritdoc cref="CreateEmployeeAggregateRequest.DocumentIds"/>
    public IReadOnlyList<string>? DocumentIds { get; init; }

    /// <summary>Remove file-registry document IDs (see also <see cref="DocumentIds"/>).</summary>
    public IReadOnlyList<string>? DeleteDocumentIds { get; init; }
}

public sealed class EmployeeAggregateIdentityDto
{
    /// <summary>Display name. Required on create.</summary>
    public string FullName { get; init; } = string.Empty;

    /// <summary>Optional admin-provided code. System code is always allocated separately.</summary>
    [JsonPropertyName("employee_code_custom")]
    public string? EmployeeCodeCustom { get; init; }

    public DateOnly? DateOfBirth { get; init; }

    [SwaggerAllowedValues(typeof(EmployeeFieldOptions), nameof(EmployeeFieldOptions.Genders))]
    public string? Gender { get; init; }

    /// <summary>Country of citizenship display name (e.g. <c>Ghana</c>). Wire: <c>country</c> — not <c>country_id</c>.</summary>
    [JsonPropertyName("country")]
    public string? Country { get; init; }

    public string? MaritalStatus { get; init; }
    public string? NextOfKinName { get; init; }
    public string? NextOfKinPhone { get; init; }
    public string? RelationshipToNextOfKin { get; init; }

    /// <summary>Emergency contacts for this employee.</summary>
    public IReadOnlyList<EmployeeEmergencyContactUpsertDto>? Emergency { get; init; }

    public string? PersonalEmail { get; init; }
    public string? WorkEmail { get; init; }

    /// <summary>Contact number in E.164 format (e.g. <c>+233201234567</c>). Required on create (<c>POST /add</c>).</summary>
    public string? Phone { get; init; }

    /// <summary>Public profile URL. Must be <c>http</c> or <c>https</c> when provided.</summary>
    public string? LinkedInUrl { get; init; }
    public string? ResidentialAddress { get; init; }

    /// <summary>
    /// **Write:** document id string from file upload (or read object with <c>doc_id</c> / <c>id</c> on round-trip update).
    /// **Read:** see <see cref="EmployeeAggregateIdentityReadDto.ProfileUrl"/>.
    /// </summary>
    [JsonConverter(typeof(ProfileUrlWriteJsonConverter))]
    public string? ProfileUrl { get; init; }

    /// <summary>
    /// Government / company ID documents. <c>id_card_type_id</c> from <c>GET /id-card-types/list</c>.
    /// On update include <c>id</c> from GET to update an existing row; omit to add.
    /// </summary>
    public IReadOnlyList<EmployeeIdentificationUpsertDto>? Identifications { get; init; }

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

    /// <summary>Country display name (e.g. <c>Ghana</c>). Wire: <c>country</c> — not <c>country_id</c>.</summary>
    [JsonPropertyName("country")]
    public string? Country { get; init; }

    public string? MaritalStatus { get; init; }
    public string? NextOfKinName { get; init; }
    public string? NextOfKinPhone { get; init; }
    public string? RelationshipToNextOfKin { get; init; }

    public IReadOnlyList<EmployeeEmergencyContactDto>? Emergency { get; init; }

    public string? PersonalEmail { get; init; }
    public string? WorkEmail { get; init; }
    public string? Phone { get; init; }
    public string? LinkedInUrl { get; init; }
    public string? ResidentialAddress { get; init; }

    /// <summary>Profile photo (<c>DocumentReadDto</c>): <c>doc_id</c>, <c>name</c>, <c>presigned_url</c> (~24h), <c>description</c>.</summary>
    public DocumentReadDto? ProfileUrl { get; init; }

    /// <summary>ID documents linked to configured id-card-types.</summary>
    public IReadOnlyList<EmployeeIdentificationDto>? Identifications { get; init; }

    public Dictionary<string, string?>? CustomFields { get; init; }
}

public class EmployeeAggregateEmploymentDto
{
    public string? JobTitle { get; init; }
    public Guid? DepartmentId { get; init; }
    public Guid? BranchId { get; init; }

    /// <summary>FK from <c>GET /employment-types/list</c>. Denormalised name is stored on the employee row.</summary>
    public Guid? EmploymentTypeId { get; init; }

    /// <summary>Bulk CSV import only — resolved by name when <see cref="EmploymentTypeId"/> is omitted.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string? EmploymentTypeName { get; init; }

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
    public decimal? WorkingHours { get; init; }
    public string? NoticePeriod { get; init; }
    public Guid? ReportsToId { get; init; }

    /// <summary>Secondary reporting line. Maps to <c>dotted_line_manager_id</c> column.</summary>
    public Guid? SecondaryReportsToId { get; init; }

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
    public decimal? NetSalary { get; init; }

    /// <summary>Wire: <c>ssnit_insurance_number</c>. Maps to <c>ssnit_number</c> column.</summary>
    public string? SsnitInsuranceNumber { get; init; }

    [SwaggerAllowedValues(typeof(EmployeeFieldOptions), nameof(EmployeeFieldOptions.PayFrequencies),
        Description = "Annualized cost uses Monthly × 12 | Bi-weekly × 26 | Weekly × 52 | Annual × 1.")]
    public string? PayFrequency { get; init; }

    /// <summary>FK to <c>core_platform.cp_currencies.id</c> (tenant-scoped, seeded). Not a currency code.</summary>
    public string? CurrencyId { get; init; }

    /// <summary>Payment methods for payroll disbursement.</summary>
    public IReadOnlyList<EmployeePaymentMethodUpsertDto>? Payment { get; init; }

    /// <summary>Custom field values for <c>section_name = employee-directory-compensation</c>.</summary>
    public Dictionary<string, string?>? CustomFields { get; init; }
}

public sealed class EmployeeAggregateReadDto
{
    public required Guid Id { get; init; }

    /// <summary>Display code: <c>employee_code_custom ?? employee_code_system</c>.</summary>
    public required string EmployeeCode { get; init; }

    public required string EmployeeCodeSystem { get; init; }
    public string? EmployeeCodeCustom { get; init; }
    public string? UserId { get; init; }
    public EmployeeAggregateIdentityReadDto Identity { get; init; } = new();
    public EmployeeAggregateEmploymentReadDto? Employment { get; init; }
    public EmployeeAggregateCompensationReadDto? Compensation { get; init; }
    public IReadOnlyList<EmployeeEducationDto>? Education { get; init; }
    public IReadOnlyList<EmployeeCertificationDto>? Certifications { get; init; }
    public EmployeeMedicalReadDto? Medical { get; init; }
    public IReadOnlyList<EmployeeSkillDto>? Skills { get; init; }
    public IReadOnlyList<EmployeeExperienceDto>? Experiences { get; init; }
    public IReadOnlyList<EmployeeReferralDto>? Referrals { get; init; }

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

    /// <summary>Write-only FK — read uses <see cref="SecondaryReportsTo"/> (<c>secondary_reports_to.id</c>).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public new Guid? SecondaryReportsToId { get; init; }

    /// <summary>Secondary reporting line reference.</summary>
    public EmployeeReportsToRefDto? SecondaryReportsTo { get; init; }

    /// <summary>Derived — has at least one active non-draft direct report.</summary>
    public bool IsLineManager { get; init; }

    /// <summary>Derived — heads at least one non-archived department.</summary>
    public bool IsHeadOfDepartment { get; init; }
}

public sealed class EmployeeAggregateCompensationReadDto : EmployeeAggregateCompensationDto
{
    public decimal? AnnualizedCost { get; init; }
    public string? CurrencyCode { get; init; }
    public string? CurrencyName { get; init; }
    public string? CurrencySymbol { get; init; }

    /// <summary>Read shape for payment methods.</summary>
    public new IReadOnlyList<EmployeePaymentMethodDto>? Payment { get; init; }
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

    /// <summary>Filter line managers — employees with at least one active non-draft direct report.</summary>
    [FromQuery(Name = PlatformQueryParams.IsLineManager)]
    public bool? IsLineManager { get; init; }

    /// <summary>Filter department heads — employees listed as head on a non-archived department.</summary>
    [FromQuery(Name = PlatformQueryParams.IsHeadOfDepartment)]
    public bool? IsHeadOfDepartment { get; init; }
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

    /// <summary>True when at least one active non-draft employee reports to this person (<c>reports_to_id</c>).</summary>
    public bool IsLineManager { get; init; }

    /// <summary>True when this person is <c>head_of_department_id</c> on at least one non-archived department.</summary>
    public bool IsHeadOfDepartment { get; init; }
}
