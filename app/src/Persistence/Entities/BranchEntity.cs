namespace ZelosHR.Api.Persistence.Entities;

public sealed class BranchEntity
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = default!;
    public string OrgId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? City { get; set; }
    public string? Region { get; set; }
    public string? CountryCode { get; set; }
    public bool IsArchived { get; set; }
    public string CustomFieldsData { get; set; } = "{}";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
