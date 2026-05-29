using ZelosHR.Api.Configs;

namespace ZelosHR.Api.Entities.Employees;

public sealed class CreateEmployeeAggregateRequest
{
    [SwaggerAllowedValues(typeof(EmployeeFieldOptions), nameof(EmployeeFieldOptions.CreateStatuses),
        Description = "draft = save without finalising; finalised = create and link platform user when work_email is set.")]
    public string Status { get; init; } = "finalised";

    public EmployeeAggregateIdentityDto Identity { get; init; } = new();
    public EmployeeAggregateEmploymentDto? Employment { get; init; }
    public EmployeeAggregateCompensationDto? Compensation { get; init; }
    public IReadOnlyList<EmployeeEducationWriteDto> Education { get; init; } = [];
    public IReadOnlyList<EmployeeCertificationWriteDto> Certifications { get; init; } = [];

    /// <summary>
    /// Document UUIDs from <c>POST /employees/documents/upload</c> (upload first, then pass IDs here).
    /// </summary>
    public IReadOnlyList<Guid>? Documents { get; init; }
}

/// <summary>Partial employee update — only include sections/fields to change.</summary>
public sealed class UpdateEmployeeAggregateRequest
{
    /// <summary>Employee UUID (same as <c>data.id</c> from <c>GET /detail</c>).</summary>
    public required Guid Id { get; init; }

    [SwaggerAllowedValues(typeof(EmployeeFieldOptions), nameof(EmployeeFieldOptions.CreateStatuses),
        Description = "Set to finalised to complete a draft (links platform user when work_email is set).")]
    public string? Status { get; init; }

    public EmployeeAggregateIdentityDto? Identity { get; init; }
    public EmployeeAggregateEmploymentDto? Employment { get; init; }
    public EmployeeAggregateCompensationDto? Compensation { get; init; }

    [SwaggerAllowedValues(typeof(EmployeeFieldOptions), nameof(EmployeeFieldOptions.LifecycleStatesForUpdate))]
    public string? LifecycleState { get; init; }

    public IReadOnlyList<EmployeeEducationUpsertDto>? Education { get; init; }
    public IReadOnlyList<EmployeeCertificationUpsertDto>? Certifications { get; init; }
    public IReadOnlyList<Guid>? DeleteEducationIds { get; init; }
    public IReadOnlyList<Guid>? DeleteCertificationIds { get; init; }

    /// <inheritdoc cref="CreateEmployeeAggregateRequest.Documents"/>
    public IReadOnlyList<Guid>? Documents { get; init; }

    /// <summary>Remove uploaded documents by UUID (see also <see cref="Documents"/>).</summary>
    public IReadOnlyList<Guid>? DeleteDocumentIds { get; init; }
}

public sealed class EmployeeAggregateIdentityDto
{
    /// <summary>Display name. Required on create unless using <c>POST /employees/import</c> first.</summary>
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
    public string? Phone { get; init; }
    public string? LinkedInUrl { get; init; }
    public string? ResidentialAddress { get; init; }

    /// <summary>Custom field values for the identity section (<c>section_name</c> = <c>identity</c>).</summary>
    public Dictionary<string, string?>? CustomFields { get; init; }
}

public class EmployeeAggregateEmploymentDto
{
    public string? JobTitle { get; init; }
    public Guid? DepartmentId { get; init; }
    public Guid? BranchId { get; init; }

    [SwaggerAllowedValues(typeof(EmployeeFieldOptions), nameof(EmployeeFieldOptions.EmploymentTypes))]
    public string? EmploymentType { get; init; }

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

    /// <summary>Custom field values for the employment section (<c>section_name</c> = <c>employment</c>).</summary>
    public Dictionary<string, string?>? CustomFields { get; init; }
}

public class EmployeeAggregateCompensationDto
{
    public decimal? GrossSalary { get; init; }

    [SwaggerAllowedValues(typeof(EmployeeFieldOptions), nameof(EmployeeFieldOptions.PayFrequencies))]
    public string? PayFrequency { get; init; }

    [SwaggerAllowedValues(typeof(EmployeeFieldOptions), nameof(EmployeeFieldOptions.Currencies))]
    public string? Currency { get; init; }

    /// <summary>Custom field values for the compensation section (<c>section_name</c> = <c>compensation</c>).</summary>
    public Dictionary<string, string?>? CustomFields { get; init; }
}

public sealed class EmployeeAggregateReadDto
{
    public required Guid Id { get; init; }
    public required string EmployeeCode { get; init; }

    [SwaggerAllowedValues(typeof(EmployeeFieldOptions), nameof(EmployeeFieldOptions.CreateStatuses))]
    public required string Status { get; init; }

    public bool IsDraft { get; init; }
    public string? UserId { get; init; }
    public EmployeeAggregateIdentityDto Identity { get; init; } = new();
    public EmployeeAggregateEmploymentReadDto? Employment { get; init; }
    public EmployeeAggregateCompensationReadDto? Compensation { get; init; }
    public IReadOnlyList<EmployeeEducationDto> Education { get; init; } = [];
    public IReadOnlyList<EmployeeCertificationDto> Certifications { get; init; } = [];

    /// <summary>Uploaded document UUIDs for this employee.</summary>
    public IReadOnlyList<Guid> Documents { get; init; } = [];

    public string? ProfilePhotoUrl { get; init; }
}

public sealed class EmployeeAggregateEmploymentReadDto : EmployeeAggregateEmploymentDto
{
    public string? DepartmentName { get; init; }
    public string? BranchName { get; init; }
}

public sealed class EmployeeAggregateCompensationReadDto : EmployeeAggregateCompensationDto
{
    public decimal? AnnualizedCost { get; init; }
}

public sealed class EmployeeListQuery
{
    public string? Search { get; init; }

    [SwaggerAllowedValues(typeof(EmployeeFieldOptions), nameof(EmployeeFieldOptions.LifecycleStatesAll))]
    public string? LifecycleState { get; init; }

    [SwaggerAllowedValues(typeof(EmployeeFieldOptions), nameof(EmployeeFieldOptions.EmploymentStatuses))]
    public string? EmploymentStatus { get; init; }

    public Guid? DepartmentId { get; init; }
    public Guid? BranchId { get; init; }

    [SwaggerAllowedValues(typeof(EmployeeFieldOptions), nameof(EmployeeFieldOptions.EmploymentTypes))]
    public string? EmploymentType { get; init; }

    public string? WorkLocation { get; init; }

    [SwaggerAllowedValues(typeof(EmployeeFieldOptions), nameof(EmployeeFieldOptions.ListSortBy))]
    public string SortBy { get; init; } = "name";

    [SwaggerAllowedValues(typeof(EmployeeFieldOptions), nameof(EmployeeFieldOptions.SortOrder))]
    public string SortOrder { get; init; } = "asc";

    public int Page { get; init; } = 1;
    public int Size { get; init; } = 20;
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

    [SwaggerAllowedValues(typeof(EmployeeFieldOptions), nameof(EmployeeFieldOptions.LifecycleStatesAll))]
    public required string LifecycleState { get; init; }

    [SwaggerAllowedValues(typeof(EmployeeFieldOptions), nameof(EmployeeFieldOptions.EmploymentStatuses))]
    public required string EmploymentStatus { get; init; }

    [SwaggerAllowedValues(typeof(EmployeeFieldOptions), nameof(EmployeeFieldOptions.EmploymentTypes))]
    public string? EmploymentType { get; init; }
}
