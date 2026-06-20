using Microsoft.AspNetCore.Mvc;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Shared.Constants;

namespace ZelosHR.Api.Entities.EmploymentTypes;

/// <summary>Ghana system defaults — seeded per org on first list.</summary>
public static class EmploymentTypeDefaults
{
    public static readonly IReadOnlyList<(string Name, string Description)> SystemTypes =
    [
        ("Full-time", "Standard salaried employment"),
        ("Part-time", "Reduced hours employment"),
        ("Contractor", "Fixed-scope contract work"),
        ("Casual", "On-demand, no fixed schedule"),
        ("Intern", "Temporary learning placement"),
    ];
}

public static class EmploymentTypeKind
{
    public const string Default = "default";
    public const string Custom = "custom";
}

public static class EmploymentTypeFieldOptions
{
    public static readonly IReadOnlyList<string> Kind =
    [
        EmploymentTypeKind.Default,
        EmploymentTypeKind.Custom,
    ];

    public static readonly IReadOnlyList<string> SortBy =
    [
        "name",
        "type",
        "employees",
        "status",
        "created_at",
    ];

    public static readonly IReadOnlyList<string> SortOrder =
    [
        "asc",
        "desc",
    ];
}

public sealed class EmploymentTypeListQuery
{
    [FromQuery(Name = PlatformQueryParams.Search)]
    public string? Search { get; init; }

    [FromQuery(Name = PlatformQueryParams.IsActive)]
    public bool? IsActive { get; init; }

    [SwaggerAllowedValues(typeof(EmploymentTypeFieldOptions), nameof(EmploymentTypeFieldOptions.SortBy))]
    [FromQuery(Name = PlatformQueryParams.SortBy)]
    public string SortBy { get; init; } = "name";

    [SwaggerAllowedValues(typeof(EmploymentTypeFieldOptions), nameof(EmploymentTypeFieldOptions.SortOrder))]
    [FromQuery(Name = PlatformQueryParams.SortOrder)]
    public string SortOrder { get; init; } = "asc";

    [FromQuery(Name = PlatformQueryParams.Page)]
    public int Page { get; init; } = 1;

    [FromQuery(Name = PlatformQueryParams.Size)]
    public int Size { get; init; } = 20;
}

public sealed record EmploymentTypeListItemDto
{
    public required string EmploymentTypeId { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }

    [SwaggerAllowedValues(typeof(EmploymentTypeFieldOptions), nameof(EmploymentTypeFieldOptions.Kind))]
    public required string Type { get; init; }

    public bool IsSystemDefault { get; init; }
    public int EmployeeCount { get; init; }
    public bool IsActive { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public string? CreatedById { get; init; }
    public string? UpdatedById { get; init; }
    public string? CreatedBy { get; init; }
    public string? UpdatedBy { get; init; }
}

public sealed class EmploymentTypeListDto
{
    public IReadOnlyList<EmploymentTypeListItemDto> Items { get; init; } = [];
}

public sealed class CreateEmploymentTypeDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
}

public sealed class UpdateEmploymentTypeDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
}

/// <summary>Nested employment type on employee read (<c>employment.employment_type</c>).</summary>
public sealed class EmployeeEmploymentTypeRefDto
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }

    [SwaggerAllowedValues(typeof(EmploymentTypeFieldOptions), nameof(EmploymentTypeFieldOptions.Kind))]
    public required string Type { get; init; }
}
