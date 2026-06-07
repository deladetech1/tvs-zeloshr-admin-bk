namespace ZelosHR.Api.Persistence.Entities;

public sealed class DepartmentEntity
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = default!;
    public string OrgId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public Guid? ParentDepartmentId { get; set; }
    public DepartmentEntity? ParentDepartment { get; set; }
    public Guid? HeadOfDepartmentId { get; set; }
    public EmployeeEntity? HeadOfDepartment { get; set; }
    public bool IsArchived { get; set; }
    public string CustomFieldsData { get; set; } = "{}";
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
