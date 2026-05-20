namespace ZelosHR.Api.Persistence.Entities;

public sealed class DepartmentEntity
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = default!;
    public string OrgId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public bool IsArchived { get; set; }
}
