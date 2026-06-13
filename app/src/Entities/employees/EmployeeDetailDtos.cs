using System.ComponentModel.DataAnnotations;

namespace ZelosHR.Api.Entities.Employees;

public sealed class EmployeeDetailDto
{
    public required string EmployeeId { get; init; }
    public required string EmployeeCode { get; init; }
    public string? UserId { get; init; }
    public required string FirstName { get; init; }
    public string? MiddleName { get; init; }
    public required string LastName { get; init; }
    public required string FullName { get; init; }
    public string? WorkEmail { get; init; }
    public DateOnly DateOfBirth { get; init; }
    public required string Gender { get; init; }
    public required string Nationality { get; init; }
    public required string GhanaCardNumber { get; init; }
    public required string PersonalEmail { get; init; }
    public required string PersonalPhone { get; init; }
    public required string ResidentialAddress { get; init; }
    public required string GhanaPostGps { get; init; }
    public string? JobTitle { get; init; }
    public string? DepartmentId { get; init; }
    public string? DepartmentName { get; init; }
    public string? BranchId { get; init; }
    public string? BranchName { get; init; }
    public string? ManagerId { get; init; }
    public string? ManagerName { get; init; }
    public string? EmploymentType { get; init; }
    public required string EmploymentStatus { get; init; }
    public string? ContractType { get; init; }
    public DateOnly? ProbationEndDate { get; init; }
    public DateOnly? EmploymentStartDate { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public string? CreatedById { get; init; }
    public string? UpdatedById { get; init; }
    public string? CreatedBy { get; init; }
    public string? UpdatedBy { get; init; }
}

public sealed class UpdateEmployeeProfileDto
{
    [MaxLength(100)]
    public string? FirstName { get; set; }

    [MaxLength(100)]
    public string? MiddleName { get; set; }

    [MaxLength(100)]
    public string? LastName { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    [MaxLength(50)]
    public string? Gender { get; set; }

    [MaxLength(100)]
    public string? Nationality { get; set; }

    [MaxLength(50)]
    public string? GhanaCardNumber { get; set; }

    [EmailAddress]
    [MaxLength(255)]
    public string? PersonalEmail { get; set; }

    [MaxLength(50)]
    public string? PersonalPhone { get; set; }

    [MaxLength(500)]
    public string? ResidentialAddress { get; set; }

    [MaxLength(100)]
    public string? GhanaPostGps { get; set; }
}

public sealed class UpdateEmployeeEmploymentDto
{
    [MaxLength(150)]
    public string? JobTitle { get; set; }

    public Guid? DepartmentId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? ManagerId { get; set; }

    [MaxLength(50)]
    public string? EmploymentType { get; set; }

    [MaxLength(50)]
    public string? EmploymentStatus { get; set; }

    [MaxLength(50)]
    public string? ContractType { get; set; }

    public DateOnly? ProbationEndDate { get; set; }
    public DateOnly? EmploymentStartDate { get; set; }
}

public sealed class UpdateEmployeeLifecycleStateDto
{
    [Required]
    [MaxLength(50)]
    public string? LifecycleState { get; set; }
}
