namespace ZelosHR.Api.Persistence.Entities;

public sealed class LifecycleEventEntity
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = default!;
    public string OrgId { get; set; } = default!;
    public Guid EmployeeId { get; set; }
    public string EmployeeFullName { get; set; } = default!;
    public string EventType { get; set; } = default!;
    public string? DepartmentName { get; set; }
    public string? BranchName { get; set; }
    public DateOnly DueDate { get; set; }
    public string Status { get; set; } = default!;
    public string Urgency { get; set; } = default!;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
