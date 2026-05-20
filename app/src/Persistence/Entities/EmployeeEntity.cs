namespace ZelosHR.Api.Persistence.Entities;

/// <summary>Maps to zeloshr.zhr_employees (schema owned by tvs-sqlscript).</summary>
public sealed class EmployeeEntity
{
    public Guid Id { get; set; }
    public string EmployeeCode { get; set; } = default!;
    public string TenantId { get; set; } = default!;
    public string OrgId { get; set; } = default!;
    public string FirstName { get; set; } = default!;
    public string? MiddleName { get; set; }
    public string LastName { get; set; } = default!;
    public DateOnly DateOfBirth { get; set; }
    public string Gender { get; set; } = default!;
    public string Nationality { get; set; } = default!;
    public string GhanaCardNumber { get; set; } = default!;
    public string PersonalEmail { get; set; } = default!;
    public string PersonalPhone { get; set; } = default!;
    public string ResidentialAddress { get; set; } = default!;
    public string GhanaPostGps { get; set; } = default!;
    public string LifecycleState { get; set; } = "Pre-hire";
    public string? JobTitle { get; set; }
    public Guid? DepartmentId { get; set; }
    public DepartmentEntity? Department { get; set; }
    public Guid? BranchId { get; set; }
    public BranchEntity? Branch { get; set; }
    public string? EmploymentType { get; set; }
    public Guid? ManagerId { get; set; }
    public EmployeeEntity? Manager { get; set; }
    public string EmploymentStatus { get; set; } = "Active";
    public string? ContractType { get; set; } = "Permanent";
    public DateOnly? ProbationEndDate { get; set; }
    public DateOnly? EmploymentStartDate { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
