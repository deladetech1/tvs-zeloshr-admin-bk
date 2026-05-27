using System.Text.Json.Serialization;

namespace ZelosHR.Api.Entities.Employees;

public sealed class CreateEmployeeAggregateRequest
{
    /// <summary><c>draft</c> or <c>finalised</c>.</summary>
    public string Status { get; init; } = "finalised";

    public EmployeeAggregateImportDto? Import { get; init; }
    public EmployeeAggregateIdentityDto Identity { get; init; } = new();
    public EmployeeAggregateEmploymentDto? Employment { get; init; }
    public EmployeeAggregateCompensationDto? Compensation { get; init; }
    public IReadOnlyList<EmployeeEducationWriteDto> Education { get; init; } = [];
    public IReadOnlyList<EmployeeCertificationWriteDto> Certifications { get; init; } = [];
    public Dictionary<string, string?>? CustomFields { get; init; }
}

/// <summary>Partial employee update — only include sections/fields to change.</summary>
public sealed class UpdateEmployeeAggregateRequest
{
    public EmployeeAggregateIdentityDto? Identity { get; init; }
    public EmployeeAggregateEmploymentDto? Employment { get; init; }
    public EmployeeAggregateCompensationDto? Compensation { get; init; }
    public string? LifecycleState { get; init; }
    public IReadOnlyList<EmployeeEducationUpsertDto>? Education { get; init; }
    public IReadOnlyList<EmployeeCertificationUpsertDto>? Certifications { get; init; }
    public Dictionary<string, string?>? CustomFields { get; init; }
    public IReadOnlyList<Guid>? DeleteEducationIds { get; init; }
    public IReadOnlyList<Guid>? DeleteCertificationIds { get; init; }
}

public sealed class EmployeeAggregateImportDto
{
    public string? ExistingUserId { get; init; }
}

public sealed class EmployeeAggregateIdentityDto
{
    public string FullName { get; init; } = string.Empty;
    public DateOnly? DateOfBirth { get; init; }
    public string? Gender { get; init; }
    public string? Nationality { get; init; }
    public string? NationalityIdType { get; init; }
    public string? IdNumber { get; init; }
    public string? PersonalEmail { get; init; }
    public string? WorkEmail { get; init; }
    public string? Phone { get; init; }
    public string? LinkedInUrl { get; init; }
    public string? ResidentialAddress { get; init; }
    public string? GpsAddress { get; init; }
    public string? State { get; init; }
}

public class EmployeeAggregateEmploymentDto
{
    public string? JobTitle { get; init; }
    public Guid? DepartmentId { get; init; }
    public Guid? BranchId { get; init; }
    public string? EmploymentType { get; init; }
    public string? EmploymentStatus { get; init; }
    public string? ContractType { get; init; }
    public string? WorkArrangement { get; init; }
    public string? WorkLocation { get; init; }
    public string? PayGrade { get; init; }
    public DateOnly? StartDate { get; init; }
    public DateOnly? ProbationEndDate { get; init; }
    public string? WorkingHours { get; init; }
    public string? NoticePeriod { get; init; }
    public Guid? ReportsToId { get; init; }
    public Guid? DottedLineManagerId { get; init; }
}

public class EmployeeAggregateCompensationDto
{
    public decimal? GrossSalary { get; init; }
    public string? PayFrequency { get; init; }
    public DateOnly? SalaryEffectiveFrom { get; init; }
    public string? Currency { get; init; }
    public string? SsnitNumber { get; init; }
    public string? TinNumber { get; init; }
    public string? Tier2PensionProvider { get; init; }
    public string? Tier3PensionProvider { get; init; }
    public string? PaymentMethod { get; init; }
    public string? BankAccountNumber { get; init; }
    public string? MobileMoneyNumber { get; init; }
}

public sealed class EmployeeAggregateReadDto
{
    public required Guid Id { get; init; }
    public required string EmployeeCode { get; init; }
    public required string Status { get; init; }
    public bool IsDraft { get; init; }
    public string? UserId { get; init; }
    public EmployeeAggregateIdentityDto Identity { get; init; } = new();
    public EmployeeAggregateEmploymentReadDto? Employment { get; init; }
    public EmployeeAggregateCompensationReadDto? Compensation { get; init; }
    public IReadOnlyList<EmployeeEducationDto> Education { get; init; } = [];
    public IReadOnlyList<EmployeeCertificationDto> Certifications { get; init; } = [];
    public Dictionary<string, string?> CustomFields { get; init; } = new();
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
    public string? MaskedSsnitNumber { get; init; }
    public string? MaskedTinNumber { get; init; }
}

public sealed class EmployeeListQuery
{
    public string? Search { get; init; }
    public string? LifecycleState { get; init; }
    public string? EmploymentStatus { get; init; }
    public Guid? DepartmentId { get; init; }
    public Guid? BranchId { get; init; }
    public string? EmploymentType { get; init; }
    public string? WorkLocation { get; init; }
    public string SortBy { get; init; } = "name";
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
    public required string LifecycleState { get; init; }
    public required string EmploymentStatus { get; init; }
    public string? EmploymentType { get; init; }
}
