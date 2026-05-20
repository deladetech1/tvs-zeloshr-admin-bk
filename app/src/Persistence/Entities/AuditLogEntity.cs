namespace ZelosHR.Api.Persistence.Entities;

public sealed class AuditLogEntity
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = default!;
    public string OrgId { get; set; } = default!;
    public DateTimeOffset OccurredAt { get; set; }
    public string ActionTitle { get; set; } = default!;
    public string? ActionDescription { get; set; }
    public Guid? EmployeeId { get; set; }
    public string? EmployeeDisplayCode { get; set; }
    public string? EmployeeFullName { get; set; }
    public string? ActorId { get; set; }
    public string ActorFullName { get; set; } = default!;
    public string Category { get; set; } = default!;
    public string Severity { get; set; } = default!;
    public bool IsFlagged { get; set; }
    public bool IsSensitiveRead { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
