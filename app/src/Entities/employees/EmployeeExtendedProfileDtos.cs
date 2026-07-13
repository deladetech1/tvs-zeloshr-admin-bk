namespace ZelosHR.Api.Entities.Employees;

// --- Emergency contacts (identity.emergency[]) ---

public sealed record EmployeeEmergencyContactDto(
    Guid Id,
    string EmergencyContactName,
    string? EmergencyContactPhone,
    string? Relationship,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string? CreatedById = null,
    string? UpdatedById = null,
    string? CreatedBy = null,
    string? UpdatedBy = null);

public sealed record EmployeeEmergencyContactWriteDto(
    string EmergencyContactName,
    string? EmergencyContactPhone,
    string? Relationship);

public sealed record EmployeeEmergencyContactUpsertDto(
    Guid? Id,
    string EmergencyContactName,
    string? EmergencyContactPhone,
    string? Relationship);

// --- Payment methods (compensation.payment[]) ---

public sealed record EmployeePaymentMethodDto(
    Guid Id,
    string PaymentMode,
    string? BankName,
    string? AccountName,
    string? AccountNumber,
    string? BranchName,
    bool IsPrimary,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string? CreatedById = null,
    string? UpdatedById = null,
    string? CreatedBy = null,
    string? UpdatedBy = null);

public sealed record EmployeePaymentMethodWriteDto(
    string PaymentMode,
    string? BankName,
    string? AccountName,
    string? AccountNumber,
    string? BranchName,
    bool IsPrimary);

public sealed record EmployeePaymentMethodUpsertDto(
    Guid? Id,
    string PaymentMode,
    string? BankName,
    string? AccountName,
    string? AccountNumber,
    string? BranchName,
    bool IsPrimary);

// --- Medical ---

public sealed class EmployeeMedicalProfileDto
{
    public Guid Id { get; init; }
    public string? BloodGroup { get; init; }
    public bool HasMedicalCondition { get; init; }
    public bool TakesRegularMedication { get; init; }
    public string? DisabilityStatus { get; init; }
    public bool RequiresAccommodation { get; init; }
    public string? AccommodationDetails { get; init; }
    public string? EmergencyMedicalNotes { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public string? CreatedById { get; init; }
    public string? UpdatedById { get; init; }
    public string? CreatedBy { get; init; }
    public string? UpdatedBy { get; init; }
}

public sealed class EmployeeMedicalProfileWriteDto
{
    public string? BloodGroup { get; init; }
    public bool HasMedicalCondition { get; init; }
    public bool TakesRegularMedication { get; init; }
    public string? DisabilityStatus { get; init; }
    public bool RequiresAccommodation { get; init; }
    public string? AccommodationDetails { get; init; }
    public string? EmergencyMedicalNotes { get; init; }
}

public sealed record EmployeeMedicalConditionDto(
    Guid Id,
    string Condition,
    string? Severity,
    string? Notes,
    DateOnly? DiagnosedDate,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string? CreatedById = null,
    string? UpdatedById = null,
    string? CreatedBy = null,
    string? UpdatedBy = null);

public sealed record EmployeeMedicalConditionWriteDto(
    string Condition,
    string? Severity,
    string? Notes,
    DateOnly? DiagnosedDate);

public sealed record EmployeeMedicalConditionUpsertDto(
    Guid? Id,
    string Condition,
    string? Severity,
    string? Notes,
    DateOnly? DiagnosedDate);

public sealed record EmployeeAllergyDto(
    Guid Id,
    string Allergen,
    string? Reaction,
    string? Severity,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string? CreatedById = null,
    string? UpdatedById = null,
    string? CreatedBy = null,
    string? UpdatedBy = null);

public sealed record EmployeeAllergyWriteDto(
    string Allergen,
    string? Reaction,
    string? Severity);

public sealed record EmployeeAllergyUpsertDto(
    Guid? Id,
    string Allergen,
    string? Reaction,
    string? Severity);

public sealed record EmployeeMedicationDto(
    Guid Id,
    string Name,
    string? Dosage,
    string? Frequency,
    string? Notes,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string? CreatedById = null,
    string? UpdatedById = null,
    string? CreatedBy = null,
    string? UpdatedBy = null);

public sealed record EmployeeMedicationWriteDto(
    string Name,
    string? Dosage,
    string? Frequency,
    string? Notes);

public sealed record EmployeeMedicationUpsertDto(
    Guid? Id,
    string Name,
    string? Dosage,
    string? Frequency,
    string? Notes);

/// <summary>Medical section on read — flat profile fields plus nested arrays.</summary>
public sealed class EmployeeMedicalReadDto
{
    public Guid? Id { get; init; }
    public string? BloodGroup { get; init; }
    public bool HasMedicalCondition { get; init; }
    public bool TakesRegularMedication { get; init; }
    public string? DisabilityStatus { get; init; }
    public bool RequiresAccommodation { get; init; }
    public string? AccommodationDetails { get; init; }
    public string? EmergencyMedicalNotes { get; init; }
    public DateTimeOffset? CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
    public string? CreatedById { get; init; }
    public string? UpdatedById { get; init; }
    public string? CreatedBy { get; init; }
    public string? UpdatedBy { get; init; }
    public IReadOnlyList<EmployeeMedicalConditionDto>? MedicalConditions { get; init; }
    public IReadOnlyList<EmployeeAllergyDto>? Allergies { get; init; }
    public IReadOnlyList<EmployeeMedicationDto>? Medications { get; init; }
}

/// <summary>Medical section on write — flat profile fields plus array rows.</summary>
public sealed class EmployeeMedicalWriteDto
{
    public string? BloodGroup { get; init; }
    public bool HasMedicalCondition { get; init; }
    public bool TakesRegularMedication { get; init; }
    public string? DisabilityStatus { get; init; }
    public bool RequiresAccommodation { get; init; }
    public string? AccommodationDetails { get; init; }
    public string? EmergencyMedicalNotes { get; init; }
    public IReadOnlyList<EmployeeMedicalConditionUpsertDto>? MedicalConditions { get; init; }
    public IReadOnlyList<EmployeeAllergyUpsertDto>? Allergies { get; init; }
    public IReadOnlyList<EmployeeMedicationUpsertDto>? Medications { get; init; }
}

// --- Skills ---

public sealed record EmployeeSkillDto(
    Guid Id,
    string Name,
    string? Proficiency,
    int? YearsOfExperience,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string? CreatedById = null,
    string? UpdatedById = null,
    string? CreatedBy = null,
    string? UpdatedBy = null);

public sealed record EmployeeSkillWriteDto(
    string Name,
    string? Proficiency,
    int? YearsOfExperience);

public sealed record EmployeeSkillUpsertDto(
    Guid? Id,
    string Name,
    string? Proficiency,
    int? YearsOfExperience);

// --- Experiences ---

public sealed record EmployeeExperienceDto(
    Guid Id,
    string Company,
    string? JobTitle,
    string? EmploymentType,
    string? Location,
    DateOnly? StartDate,
    DateOnly? EndDate,
    bool IsCurrent,
    string? Description,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string? CreatedById = null,
    string? UpdatedById = null,
    string? CreatedBy = null,
    string? UpdatedBy = null);

public sealed record EmployeeExperienceWriteDto(
    string Company,
    string? JobTitle,
    string? EmploymentType,
    string? Location,
    DateOnly? StartDate,
    DateOnly? EndDate,
    bool IsCurrent,
    string? Description);

public sealed record EmployeeExperienceUpsertDto(
    Guid? Id,
    string Company,
    string? JobTitle,
    string? EmploymentType,
    string? Location,
    DateOnly? StartDate,
    DateOnly? EndDate,
    bool IsCurrent,
    string? Description);

// --- Referrals ---

public sealed record EmployeeReferralDto(
    Guid Id,
    string Name,
    string? JobTitle,
    string? Company,
    string? Relationship,
    string? Email,
    string? Phone,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string? CreatedById = null,
    string? UpdatedById = null,
    string? CreatedBy = null,
    string? UpdatedBy = null);

public sealed record EmployeeReferralWriteDto(
    string Name,
    string? JobTitle,
    string? Company,
    string? Relationship,
    string? Email,
    string? Phone);

public sealed record EmployeeReferralUpsertDto(
    Guid? Id,
    string Name,
    string? JobTitle,
    string? Company,
    string? Relationship,
    string? Email,
    string? Phone);
