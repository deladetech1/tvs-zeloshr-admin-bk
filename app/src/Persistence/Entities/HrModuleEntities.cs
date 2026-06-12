namespace ZelosHR.Api.Persistence.Entities;

public sealed class AttendanceRecordEntity
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = default!;
    public string OrgId { get; set; } = default!;
    public Guid EmployeeId { get; set; }
    public string EmployeeFullName { get; set; } = default!;
    public string? EmployeeCode { get; set; }
    public string? DepartmentName { get; set; }
    public string? BranchName { get; set; }
    public DateOnly AttendanceDate { get; set; }
    public TimeOnly? ClockIn { get; set; }
    public TimeOnly? ClockOut { get; set; }
    public string Status { get; set; } = default!;
    public decimal? HoursWorked { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class LeaveRequestEntity
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = default!;
    public string OrgId { get; set; } = default!;
    public Guid EmployeeId { get; set; }
    public string EmployeeFullName { get; set; } = default!;
    public string LeaveType { get; set; } = default!;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal DaysRequested { get; set; }
    public string Status { get; set; } = default!;
    public string? ApproverName { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset SubmittedAt { get; set; }
}

public sealed class LeaveBalanceEntity
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = default!;
    public string OrgId { get; set; } = default!;
    public Guid EmployeeId { get; set; }
    public string EmployeeFullName { get; set; } = default!;
    public string LeaveType { get; set; } = default!;
    public decimal EntitledDays { get; set; }
    public decimal UsedDays { get; set; }
    public decimal RemainingDays { get; set; }
}

public sealed class LeaveTypeEntity
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = default!;
    public string OrgId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? CountryCode { get; set; }
    public decimal DefaultEntitledDays { get; set; }
    public bool IsPaid { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class PublicHolidayEntity
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = default!;
    public string OrgId { get; set; } = default!;
    public string CountryCode { get; set; } = default!;
    public string Name { get; set; } = default!;
    public DateOnly HolidayDate { get; set; }
    public bool IsRecurring { get; set; }
    public Guid? BranchId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class JobPostingEntity
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = default!;
    public string OrgId { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string? DepartmentName { get; set; }
    public string? BranchName { get; set; }
    public string? EmploymentType { get; set; }
    public string Status { get; set; } = default!;
    public int ApplicantsCount { get; set; }
    public DateOnly PostedAt { get; set; }
    public DateOnly? ClosingDate { get; set; }
}

public sealed class OnboardingTaskEntity
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = default!;
    public string OrgId { get; set; } = default!;
    public Guid EmployeeId { get; set; }
    public string EmployeeFullName { get; set; } = default!;
    public string TaskName { get; set; } = default!;
    public string Category { get; set; } = default!;
    public DateOnly DueDate { get; set; }
    public string Status { get; set; } = default!;
    public string? AssignedTo { get; set; }
}

public sealed class PerformanceReviewEntity
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = default!;
    public string OrgId { get; set; } = default!;
    public Guid EmployeeId { get; set; }
    public string EmployeeFullName { get; set; } = default!;
    public string ReviewPeriod { get; set; } = default!;
    public string? ReviewerName { get; set; }
    public string? OverallRating { get; set; }
    public string Status { get; set; } = default!;
    public DateOnly DueDate { get; set; }
}

public sealed class DisciplinaryCaseEntity
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = default!;
    public string OrgId { get; set; } = default!;
    public Guid EmployeeId { get; set; }
    public string EmployeeFullName { get; set; } = default!;
    public string CaseType { get; set; } = default!;
    public string Severity { get; set; } = default!;
    public string Status { get; set; } = default!;
    public DateOnly OpenedAt { get; set; }
    public string? Description { get; set; }
}

public sealed class EmployeeDocumentEntity
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = default!;
    public string OrgId { get; set; } = default!;
    public Guid EmployeeId { get; set; }
    public string? EmployeeFullName { get; set; }
    public string Category { get; set; } = default!;
    public string FileName { get; set; } = default!;
    public long FileSizeBytes { get; set; }
    public string BlobUrl { get; set; } = default!;
    public string ContentType { get; set; } = default!;
    public string? UploadedBy { get; set; }
    public DateTimeOffset UploadedAt { get; set; }
    public bool IsDeleted { get; set; }
    public string? Status { get; set; }
    public int? FileSizeKb { get; set; }
    public string? DocumentName { get; set; }
}
