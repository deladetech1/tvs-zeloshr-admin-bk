namespace ZelosHR.Api.Entities.Employees;

/// <summary>Standard read model for uplift CRUD and tests.</summary>
public sealed class EmployeeReadDto
{
    public Guid Id { get; init; }
    public string EmployeeCode { get; init; } = default!;
    public string FirstName { get; init; } = default!;
    public string? MiddleName { get; init; }
    public string LastName { get; init; } = default!;
    public string FullName { get; init; } = default!;
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? GhanaCardNumber { get; init; }
    public string? JobTitle { get; init; }
    public string? Department { get; init; }
    public string? DepartmentId { get; init; }
    public string EmploymentType { get; init; } = default!;
    public string LifecycleStatus { get; init; } = default!;
    public string? ContractType { get; init; }
    public DateOnly? ContractEndDate { get; init; }
    public DateOnly? StartDate { get; init; }
    public DateOnly? ProbationEndDate { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}

public sealed class EmployeeWriteDto
{
    public string FirstName { get; init; } = default!;
    public string LastName { get; init; } = default!;
    public string? MiddleName { get; init; }
    public string? GhanaCardNumber { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? JobTitle { get; init; }
    public Guid? DepartmentId { get; init; }
    public string EmploymentType { get; init; } = "Full-time";
    public string? ContractType { get; init; }
    public DateOnly? StartDate { get; init; }
    public DateOnly? ProbationEndDate { get; init; }
    public DateOnly? ContractEndDate { get; init; }
    public DateOnly? DateOfBirth { get; init; }
    public string? Gender { get; init; }
    public string? Nationality { get; init; }
    public string? ResidentialAddress { get; init; }
    public string? GhanaPostGps { get; init; }
}
