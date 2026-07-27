namespace ZelosHR.Api.Persistence.Entities;

/// <summary>Maps to zeloshr.zhr_employee_portal_subdomain — one row per org.</summary>
public sealed class EmployeePortalSubdomainEntity
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = default!;
    public string OrgId { get; set; } = default!;
    public string BusId { get; set; } = default!;
    public string LocId { get; set; } = default!;
    public string Subdomain { get; set; } = default!;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}
