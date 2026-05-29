using ZelosHR.Api.Entities.Employees;

namespace ZelosHR.Api.Persistence.Entities;

/// <summary>Maps to zeloshr.zhr_employees (schema owned by tvs-sqlscript).</summary>
public sealed class EmployeeEntity
{
    public Guid Id { get; set; }
    public string EmployeeCode { get; set; } = default!;
    public string TenantId { get; set; } = default!;
    public string OrgId { get; set; } = default!;
    public string? UserId { get; set; }
    public string FullName { get; set; } = default!;

    public string? FirstName { get; set; }
    public string? MiddleName { get; set; }
    public string? LastName { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? Nationality { get; set; }
    public string? NationalityIdType { get; set; }
    public DateOnly? IdIssueDate { get; set; }
    public DateOnly? IdExpiryDate { get; set; }
    public string? IdNumber { get; set; }
    public string? GhanaCardNumber { get; set; }
    public string? PersonalEmail { get; set; }
    public string? WorkEmail { get; set; }
    public string? PersonalPhone { get; set; }
    public string? Phone { get; set; }
    public string? LinkedInUrl { get; set; }
    public string? ResidentialAddress { get; set; }
    public string? GhanaPostGps { get; set; }
    public string? State { get; set; }
    public string? ProfilePhotoUrl { get; set; }

    public string LifecycleState { get; set; } = EmployeeLifecycleStates.PreHire;
    public string LifecycleStatus { get; set; } = "draft";
    public bool IsDraft { get; set; } = true;

    public string? JobTitle { get; set; }
    public Guid? DepartmentId { get; set; }
    public DepartmentEntity? Department { get; set; }
    public Guid? BranchId { get; set; }
    public BranchEntity? Branch { get; set; }
    public string? EmploymentType { get; set; }
    public string? WorkArrangement { get; set; }
    public string? WorkLocation { get; set; }
    public string? PayGrade { get; set; }
    public Guid? ManagerId { get; set; }
    public EmployeeEntity? Manager { get; set; }
    public Guid? ReportsToId { get; set; }
    public EmployeeEntity? ReportsTo { get; set; }
    public Guid? DottedLineManagerId { get; set; }
    public EmployeeEntity? DottedLineManager { get; set; }
    public string EmploymentStatus { get; set; } = EmploymentStatusValues.Active;
    public string? ContractType { get; set; } = "Permanent";
    public DateOnly? ProbationEndDate { get; set; }
    public DateOnly? EmploymentStartDate { get; set; }
    public DateOnly? StartDate { get; set; }
    public string? WorkingHours { get; set; }
    public string? NoticePeriod { get; set; }

    public decimal? GrossSalary { get; set; }
    public string? PayFrequency { get; set; }
    public decimal? AnnualizedCost { get; set; }
    public DateOnly? SalaryEffectiveFrom { get; set; }
    public string Currency { get; set; } = "GHS";
    public string? SsnitNumber { get; set; }
    public string? TinNumber { get; set; }
    public string? Tier2PensionProvider { get; set; }
    public string? Tier3PensionProvider { get; set; }
    public string? PaymentMethod { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? MobileMoneyNumber { get; set; }

    public bool IsDeleted { get; set; }
    public string CustomFieldsData { get; set; } = "{}";
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
