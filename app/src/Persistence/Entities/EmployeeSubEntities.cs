namespace ZelosHR.Api.Persistence.Entities;

public sealed class EmployeeEducationEntity
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string Institution { get; set; } = default!;
    public string? Degree { get; set; }
    public string? FieldOfStudy { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IsCurrent { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class EmployeeCertificationEntity
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string Name { get; set; } = default!;
    public string? IssuingBody { get; set; }
    public DateOnly? IssueDate { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public string? CredentialUrl { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class EmployeeIdentificationEntity
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid IdCardTypeId { get; set; }
    public IdCardTypeEntity? IdCardType { get; set; }
    public string IdNumber { get; set; } = default!;
    public DateOnly? IdIssueDate { get; set; }
    public DateOnly? IdExpiryDate { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class EmployeeEmergencyContactEntity
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmergencyContactName { get; set; } = default!;
    public string? EmergencyContactPhone { get; set; }
    public string? Relationship { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class EmployeePaymentMethodEntity
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string PaymentMode { get; set; } = default!;
    public string? BankName { get; set; }
    public string? AccountName { get; set; }
    public string? AccountNumber { get; set; }
    public string? BranchName { get; set; }
    public bool IsPrimary { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class EmployeeMedicalProfileEntity
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string? BloodGroup { get; set; }
    public bool HasMedicalCondition { get; set; }
    public bool TakesRegularMedication { get; set; }
    public string? DisabilityStatus { get; set; }
    public bool RequiresAccommodation { get; set; }
    public string? AccommodationDetails { get; set; }
    public string? EmergencyMedicalNotes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class EmployeeMedicalConditionEntity
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string Condition { get; set; } = default!;
    public string? Severity { get; set; }
    public string? Notes { get; set; }
    public DateOnly? DiagnosedDate { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class EmployeeAllergyEntity
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string Allergen { get; set; } = default!;
    public string? Reaction { get; set; }
    public string? Severity { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class EmployeeMedicationEntity
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string Name { get; set; } = default!;
    public string? Dosage { get; set; }
    public string? Frequency { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class EmployeeSkillEntity
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string Name { get; set; } = default!;
    public string? Proficiency { get; set; }
    public int? YearsOfExperience { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class EmployeeExperienceEntity
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string Company { get; set; } = default!;
    public string? JobTitle { get; set; }
    public string? EmploymentType { get; set; }
    public string? Location { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IsCurrent { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class EmployeeReferralEntity
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string Name { get; set; } = default!;
    public string? JobTitle { get; set; }
    public string? Company { get; set; }
    public string? Relationship { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
