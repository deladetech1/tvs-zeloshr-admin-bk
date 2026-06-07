namespace ZelosHR.Api.Entities.Departments;

public sealed class OrganisationSummaryDto
{
    public int DepartmentCount { get; init; }
    public int BranchCount { get; init; }
    public int ArchivedCount { get; init; }
}

public sealed class DepartmentHeadDto
{
    public string? EmployeeId { get; init; }
    public required string FullName { get; init; }
    public string? JobTitle { get; init; }
    public required string Initials { get; init; }
}

public sealed class DepartmentListItemDto
{
    public required string DepartmentId { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public string? ParentDepartmentId { get; init; }
    public string? ParentDepartmentName { get; init; }
    public DepartmentHeadDto? HeadOfDepartment { get; init; }
    public int EmployeeCount { get; init; }
    public bool IsArchived { get; init; }
    public int HierarchyLevel { get; init; }
    public int? HeadcountCapacity { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public string? CreatedById { get; init; }
    public string? UpdatedById { get; init; }
    public string? CreatedBy { get; init; }
    public string? UpdatedBy { get; init; }
}

public sealed class DepartmentListDto
{
    public OrganisationSummaryDto Summary { get; init; } = new();
    public IReadOnlyList<DepartmentListItemDto> Items { get; init; } = [];
    public string? ShowingLabel { get; init; }
}
