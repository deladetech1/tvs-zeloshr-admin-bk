namespace ZelosHR.Api.Persistence.Entities;

/// <summary>Maps to zeloshr.zhr_employee_id_format — one row per org.</summary>
public sealed class EmployeeIdFormatEntity
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = default!;
    public string OrgId { get; set; } = default!;
    public string Prefix { get; set; } = default!;
    public int DigitCount { get; set; }
    public int StartingNumber { get; set; }
    public string Separator { get; set; } = default!;
    public bool AutoGenerate { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}
