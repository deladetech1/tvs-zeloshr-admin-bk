using Microsoft.AspNetCore.Mvc;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Shared.Constants;

namespace ZelosHR.Api.Entities.IdCardTypes;

/// <summary>Ghana system defaults — seeded per org on first list.</summary>
public static class IdCardTypeDefaults
{
    public static readonly IReadOnlyList<(string Name, string Description)> SystemTypes =
    [
        ("National ID", "Ghana Card national identity document"),
        ("Voter's ID", "Electoral Commission voter identification card"),
        ("Driver's License", "DVLA driver's licence"),
        ("National Health Insurance", "NHIS health insurance identity card"),
    ];
}

public static class IdCardTypeKind
{
    public const string Default = "default";
    public const string Custom = "custom";
}

public static class IdCardTypeFieldOptions
{
    public static readonly IReadOnlyList<string> Kind =
    [
        IdCardTypeKind.Default,
        IdCardTypeKind.Custom,
    ];

    public static readonly IReadOnlyList<string> SortBy =
    [
        "name",
        "type",
        "status",
        "created_at",
    ];

    public static readonly IReadOnlyList<string> SortOrder =
    [
        "asc",
        "desc",
    ];
}

public sealed class IdCardTypeListQuery
{
    [FromQuery(Name = PlatformQueryParams.Search)]
    public string? Search { get; init; }

    [FromQuery(Name = PlatformQueryParams.IsActive)]
    public bool? IsActive { get; init; }

    [SwaggerAllowedValues(typeof(IdCardTypeFieldOptions), nameof(IdCardTypeFieldOptions.SortBy))]
    [FromQuery(Name = PlatformQueryParams.SortBy)]
    public string SortBy { get; init; } = "name";

    [SwaggerAllowedValues(typeof(IdCardTypeFieldOptions), nameof(IdCardTypeFieldOptions.SortOrder))]
    [FromQuery(Name = PlatformQueryParams.SortOrder)]
    public string SortOrder { get; init; } = "asc";

    [FromQuery(Name = PlatformQueryParams.Page)]
    public int Page { get; init; } = 1;

    [FromQuery(Name = PlatformQueryParams.Size)]
    public int Size { get; init; } = 20;
}

public sealed record IdCardTypeListItemDto
{
    public required string IdCardTypeId { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }

    [SwaggerAllowedValues(typeof(IdCardTypeFieldOptions), nameof(IdCardTypeFieldOptions.Kind))]
    public required string Type { get; init; }

    public bool IsSystemDefault { get; init; }
    public bool IsActive { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public string? CreatedById { get; init; }
    public string? UpdatedById { get; init; }
    public string? CreatedBy { get; init; }
    public string? UpdatedBy { get; init; }
}

public sealed class IdCardTypeListDto
{
    public IReadOnlyList<IdCardTypeListItemDto> Items { get; init; } = [];
}

public sealed class CreateIdCardTypeDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
}

public sealed class UpdateIdCardTypeDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
}
