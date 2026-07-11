namespace ZelosHR.Api.Persistence.Entities;

/// <summary>Maps to zeloshr.zhr_employee_change_requests.</summary>
public sealed class EmployeeChangeRequestEntity
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = default!;
    public string OrgId { get; set; } = default!;
    public Guid EmployeeId { get; set; }
    public string FieldPath { get; set; } = default!;
    public string? OldValueJson { get; set; }
    public string NewValueJson { get; set; } = default!;
    public string Status { get; set; } = default!;
    public string RequestedBy { get; set; } = default!;
    public string? ReviewedBy { get; set; }
    public string? ReviewNote { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}
