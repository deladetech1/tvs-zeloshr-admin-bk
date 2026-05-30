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
