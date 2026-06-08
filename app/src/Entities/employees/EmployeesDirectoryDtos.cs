using ZelosHR.Api.Configs;

namespace ZelosHR.Api.Entities.Employees;

public sealed class EmployeeDirectorySummaryDto
{
    public int TotalEmployees { get; init; }
    public int ActiveEmployees { get; init; }
    public int OnProbation { get; init; }
    public int OnContract { get; init; }
}

public sealed class EmployeeDirectoryItemDto
{
    public required string EmployeeId { get; init; }
    public required string EmployeeCode { get; init; }
    public required string FullName { get; init; }
    public required string Initials { get; init; }
    public string? JobTitle { get; init; }
    public string? DepartmentId { get; init; }
    public string? DepartmentName { get; init; }
    public string? BranchId { get; init; }
    public string? BranchName { get; init; }
    public string? EmploymentType { get; init; }
    public string? ManagerId { get; init; }
    public string? ManagerName { get; init; }
    public required string Status { get; init; }

    [SwaggerAllowedValues(typeof(EmployeeFieldOptions), nameof(EmployeeFieldOptions.Engagements))]
    public string? Engagement { get; init; }

    public IReadOnlyList<string> WorkStates { get; init; } = [];
}

public sealed class EmployeeDirectoryListDto
{
    public EmployeeDirectorySummaryDto Summary { get; init; } = new();
    public IReadOnlyList<EmployeeDirectoryItemDto> Items { get; init; } = [];
    public string? EmptyMessage { get; init; }
}

public sealed class EmployeeFilterOptionsDto
{
    public IReadOnlyList<FilterOptionDto> Departments { get; init; } = [];
    public IReadOnlyList<FilterOptionDto> Branches { get; init; } = [];
    public IReadOnlyList<string> EmploymentTypes { get; init; } = [];

    /// <summary>Exact employment_status values for legacy/exact-match filters.</summary>
    public IReadOnlyList<string> Statuses { get; init; } = [];

    /// <summary>Simple list filter commands (active, probation, on_leave, …).</summary>
    public IReadOnlyList<string> StatusFilters { get; init; } = [];
}

public sealed class FilterOptionDto
{
    public required string Id { get; init; }
    public required string Name { get; init; }
}
