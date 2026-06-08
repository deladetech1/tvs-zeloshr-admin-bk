using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Files;

namespace ZelosHR.Api.Entities.OrgStructure;

/// <summary>Department badge on org-chart manager nodes (headcount bar in UI).</summary>
public sealed class OrgChartDepartmentBadgeDto
{
    public required string DepartmentId { get; init; }
    public required string Name { get; init; }
    public int EmployeeCount { get; init; }
    public int? HeadcountCapacity { get; init; }
}

/// <summary>
/// One person in the reporting tree. <c>children</c> are direct reports;
/// nested levels (CEO → dept heads → ICs) come from <c>reports_to_id</c> on employees.
/// </summary>
public sealed class OrgChartNodeDto
{
    public required string Id { get; init; }
    public required string FullName { get; init; }
    public string? JobTitle { get; init; }
    /// <summary>Profile photo with presigned URL (~24h), same shape as GET /employees/list.</summary>
    public DocumentReadDto? ProfileUrl { get; init; }
    [SwaggerAllowedValues(typeof(OrgStructureFieldOptions), nameof(OrgStructureFieldOptions.NodeTypes))]
    public required string NodeType { get; init; }
    public string? ParentId { get; init; }
    public OrgChartDepartmentBadgeDto? Department { get; init; }
    public IReadOnlyList<OrgChartNodeDto> Children { get; init; } = [];
}

public sealed class OrgChartDto
{
    public IReadOnlyList<OrgChartNodeDto> Roots { get; init; } = [];
}

public sealed record OrgChartEmployeeRow(
    Guid Id,
    string FullName,
    string? FirstName,
    string? MiddleName,
    string? LastName,
    string? JobTitle,
    Guid? ReportsToId,
    string? UserId,
    string? ProfilePhotoUrl);

public sealed record OrgChartDepartmentHeadRow(
    Guid DepartmentId,
    string Name,
    Guid HeadOfDepartmentId,
    int EmployeeCount,
    int? HeadcountCapacity);
