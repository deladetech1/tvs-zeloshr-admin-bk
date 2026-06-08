using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Departments;
using ZelosHR.Api.Entities.Branches;

namespace ZelosHR.Api.Entities.OrgStructure;

public sealed class OrgStructureSummaryDto
{
    public OrganisationSummaryDto Tabs { get; init; } = new();
}

public sealed class CreateDepartmentRequestDto
{
    public required string Name { get; init; }
    public Guid? ParentDepartmentId { get; init; }

    /// <summary>Flat employee UUID (<c>head_of_department_id</c>).</summary>
    public Guid? HeadOfDepartmentId { get; init; }

    /// <summary>Nested head from list round-trip (<c>head_of_department.employee_id</c>).</summary>
    public DepartmentHeadReferenceDto? HeadOfDepartment { get; init; }

    public string? Description { get; init; }
    public int? HeadcountCapacity { get; init; }
}

public sealed class CreateDepartmentResponseDto
{
    public required string DepartmentId { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public DepartmentHeadDto? HeadOfDepartment { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public string? CreatedById { get; init; }
    public string? UpdatedById { get; init; }
    public string? CreatedBy { get; init; }
    public string? UpdatedBy { get; init; }
}

public sealed class UpdateDepartmentRequestDto
{
    public string? Name { get; init; }
    public Guid? ParentDepartmentId { get; init; }

    /// <summary>Flat employee UUID (<c>head_of_department_id</c>).</summary>
    public Guid? HeadOfDepartmentId { get; init; }

    /// <summary>Nested head from list round-trip (<c>head_of_department.employee_id</c>).</summary>
    public DepartmentHeadReferenceDto? HeadOfDepartment { get; init; }

    public string? Description { get; init; }
    public int? HeadcountCapacity { get; init; }
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
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public string? CreatedById { get; init; }
    public string? UpdatedById { get; init; }
    public string? CreatedBy { get; init; }
    public string? UpdatedBy { get; init; }
}
