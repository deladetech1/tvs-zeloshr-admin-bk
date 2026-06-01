namespace ZelosHR.Api.Entities.Employees;

public sealed record CreateEmployeeRequest
{
    public string FullName { get; init; } = string.Empty;
    public DateOnly? DateOfBirth { get; init; }
    public string? Gender { get; init; }
    public string? Country { get; init; }
    public string? IdType { get; init; }
    public DateOnly? IdIssueDate { get; init; }
    public DateOnly? IdExpiryDate { get; init; }
    public string? IdNumber { get; init; }
    public string? PersonalEmail { get; init; }
    public string? WorkEmail { get; init; }
    public string? Phone { get; init; }
    public string? LinkedInUrl { get; init; }
    public string? ResidentialAddress { get; init; }
    public string? JobTitle { get; init; }
    public Guid? DepartmentId { get; init; }
    public Guid? BranchId { get; init; }
    public string? EmploymentType { get; init; }
    public string? WorkArrangement { get; init; }
    public string? WorkLocation { get; init; }
    public string? PayGrade { get; init; }
    public DateOnly? StartDate { get; init; }
    public DateOnly? ProbationEndDate { get; init; }
    public string? WorkingHours { get; init; }
    public string? NoticePeriod { get; init; }
    public Guid? ReportsToId { get; init; }
    public Guid? DottedLineManagerId { get; init; }
    public decimal? GrossSalary { get; init; }
    public string? PayFrequency { get; init; }
    public DateOnly? SalaryEffectiveFrom { get; init; }
    public string? CurrencyId { get; init; }
    public string? SsnitNumber { get; init; }
    public string? TinNumber { get; init; }
    public string? Tier2PensionProvider { get; init; }
    public string? Tier3PensionProvider { get; init; }
    public string? PaymentMethod { get; init; }
    public string? BankAccountNumber { get; init; }
    public string? MobileMoneyNumber { get; init; }
    public bool Finalise { get; init; }
}

public sealed record EmployeeRegistrationReadDto
{
    public required Guid Id { get; init; }
    public required string EmployeeCode { get; init; }
    public required string FullName { get; init; }
    public string? UserId { get; init; }
    public bool IsDraft { get; init; }
    public string LifecycleStatus { get; init; } = "draft";
    public string? JobTitle { get; init; }
    public Guid? DepartmentId { get; init; }
    public string? WorkEmail { get; init; }
    public string? ProfileUrl { get; init; }
    public decimal? AnnualizedCost { get; init; }
    public string? CurrencyId { get; init; }
    public string? MaskedSsnitNumber { get; init; }
    public string? MaskedTinNumber { get; init; }
}

public sealed record ImportEmployeesRequest
{
    /// <summary>One or more <c>cp_users</c> IDs to link as draft employees.</summary>
    public required IReadOnlyList<string> UserIds { get; init; }
}

public sealed class ImportEmployeeRowResult
{
    public required string UserId { get; init; }
    public bool Success { get; init; }
    public Guid? EmployeeId { get; init; }
    public string? EmployeeCode { get; init; }
    public string? FullName { get; init; }
    public string? Error { get; init; }
}

public sealed class ImportEmployeesResult
{
    public IReadOnlyList<ImportEmployeeRowResult> Items { get; init; } = [];
    public int SuccessCount { get; init; }
    public int FailureCount { get; init; }
}

/// <summary>Step 1 — draft only. Platform user is created on finalise (see <c>cp_users</c>).</summary>
public sealed record CreateDraftRequest
{
    /// <summary>Display name until finalise; maps to <c>cp_users.fullname</c> when the platform user is created.</summary>
    public string FullName { get; init; } = string.Empty;

    /// <summary>Import an existing <c>cp_users</c> row instead of creating one on finalise.</summary>
    public string? ExistingUserId { get; init; }
}
