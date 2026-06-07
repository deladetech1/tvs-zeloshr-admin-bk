namespace ZelosHR.Api.Persistence.Entities;

public sealed class BranchEntity
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = default!;
    public string OrgId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Address { get; set; }
    public string? Country { get; set; }
    public string? Description { get; set; }
    public bool IsArchived { get; set; }
    public string CustomFieldsData { get; set; } = "{}";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
