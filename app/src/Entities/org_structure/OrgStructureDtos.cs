using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Departments;
using ZelosHR.Api.Entities.Branches;

namespace ZelosHR.Api.Entities.OrgStructure;

public sealed class OrgStructureSummaryDto
{
    public OrganisationSummaryDto Tabs { get; init; } = new();
}

public sealed class OrgChartNodeDto
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    [SwaggerAllowedValues(typeof(OrgStructureFieldOptions), nameof(OrgStructureFieldOptions.NodeTypes))]
    public required string NodeType { get; init; }
    public string? ParentId { get; init; }
    public DepartmentHeadDto? HeadOfDepartment { get; init; }
    public int EmployeeCount { get; init; }
    public IReadOnlyList<OrgChartNodeDto> Children { get; init; } = [];
}

public sealed class OrgChartDto
{
    public IReadOnlyList<OrgChartNodeDto> Roots { get; init; } = [];
}

public sealed class CreateDepartmentRequestDto
{
    public required string Name { get; init; }
    public Guid? ParentDepartmentId { get; init; }
    public Guid? HeadOfDepartmentId { get; init; }
    public string? Description { get; init; }
}

public sealed class CreateDepartmentResponseDto
{
    public required string DepartmentId { get; init; }
    public required string Name { get; init; }
}

public sealed class UpdateDepartmentRequestDto
{
    public string? Name { get; init; }
    public Guid? ParentDepartmentId { get; init; }
    public Guid? HeadOfDepartmentId { get; init; }
    public string? Description { get; init; }
}

public sealed class CreateBranchRequestDto
{
    public required string Name { get; init; }
    public string? Address { get; init; }
    public string? Country { get; init; }
    public string? Description { get; init; }
}

public sealed class UpdateBranchRequestDto
{
    public string? Name { get; init; }
    public string? Address { get; init; }
    public string? Country { get; init; }
    public string? Description { get; init; }
}

public sealed class BranchMutationResponseDto
{
    public required string BranchId { get; init; }
    public required string Name { get; init; }
    public string? Address { get; init; }
    public string? Country { get; init; }
    public string? Description { get; init; }
}
