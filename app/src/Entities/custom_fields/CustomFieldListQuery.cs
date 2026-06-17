using Microsoft.AspNetCore.Mvc;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Shared.Constants;

namespace ZelosHR.Api.Entities.CustomFields;

/// <summary>Query filters for <c>GET /api/v1/custom-fields/list</c> — matches frontend <c>CustomFieldParams</c>.</summary>
public sealed class CustomFieldListQuery
{
    [FromQuery(Name = PlatformQueryParams.Search)]
    public string? Search { get; init; }

    [FromQuery(Name = PlatformQueryParams.EntityType)]
    [SwaggerAllowedValues(typeof(CustomFieldEntityTypes), nameof(CustomFieldEntityTypes.All))]
    public string? EntityType { get; init; }

    [FromQuery(Name = PlatformQueryParams.FieldKey)]
    public string? FieldKey { get; init; }

    [FromQuery(Name = PlatformQueryParams.Label)]
    public string? Label { get; init; }

    [FromQuery(Name = PlatformQueryParams.FieldType)]
    [SwaggerAllowedValues(typeof(CustomFieldFieldTypes), nameof(CustomFieldFieldTypes.All))]
    public string? FieldType { get; init; }

    [FromQuery(Name = PlatformQueryParams.IsRequired)]
    public bool? IsRequired { get; init; }

    [FromQuery(Name = PlatformQueryParams.IsSensitive)]
    public bool? IsSensitive { get; init; }

    [FromQuery(Name = PlatformQueryParams.IsFilterable)]
    public bool? IsFilterable { get; init; }

    [FromQuery(Name = PlatformQueryParams.IsSearchable)]
    public bool? IsSearchable { get; init; }

    [FromQuery(Name = PlatformQueryParams.IsActive)]
    public bool? IsActive { get; init; }

    [FromQuery(Name = PlatformQueryParams.SectionName)]
    [SwaggerAllowedValues(typeof(EmployeeCustomFieldSections), nameof(EmployeeCustomFieldSections.All))]
    public string? SectionName { get; init; }

    [FromQuery(Name = PlatformQueryParams.IncludeDeleted)]
    public bool IncludeDeleted { get; init; }

    [FromQuery(Name = PlatformQueryParams.SortBy)]
    [SwaggerAllowedValues(typeof(CustomFieldListSortOptions), nameof(CustomFieldListSortOptions.SortBy))]
    public string SortBy { get; init; } = "label";

    [FromQuery(Name = PlatformQueryParams.SortOrder)]
    [SwaggerAllowedValues(typeof(CustomFieldListSortOptions), nameof(CustomFieldListSortOptions.SortOrder))]
    public string SortOrder { get; init; } = "asc";

    [FromQuery(Name = PlatformQueryParams.Page)]
    public int Page { get; init; } = 1;

    [FromQuery(Name = PlatformQueryParams.Size)]
    public int Size { get; init; } = 20;
}

/// <summary>Query filters for <c>GET /api/v1/custom-fields/audit-logs</c>.</summary>
public sealed class CustomFieldAuditLogQuery
{
    [FromQuery(Name = PlatformQueryParams.EntityType)]
    [SwaggerAllowedValues(typeof(CustomFieldEntityTypes), nameof(CustomFieldEntityTypes.All))]
    public string? EntityType { get; init; }

    [FromQuery(Name = PlatformQueryParams.EntityId)]
    public Guid? EntityId { get; init; }

    [FromQuery(Name = PlatformQueryParams.FieldKey)]
    public string? FieldKey { get; init; }

    [FromQuery(Name = PlatformQueryParams.ChangeType)]
    [SwaggerAllowedValues(typeof(CustomFieldChangeTypes), nameof(CustomFieldChangeTypes.All))]
    public string? ChangeType { get; init; }

    [FromQuery(Name = PlatformQueryParams.ChangedBy)]
    public string? ChangedBy { get; init; }

    [FromQuery(Name = PlatformQueryParams.ChangedFrom)]
    public DateTimeOffset? ChangedFrom { get; init; }

    [FromQuery(Name = PlatformQueryParams.ChangedTo)]
    public DateTimeOffset? ChangedTo { get; init; }

    [FromQuery(Name = PlatformQueryParams.Page)]
    public int Page { get; init; } = 1;

    [FromQuery(Name = PlatformQueryParams.Size)]
    public int Size { get; init; } = 20;
}
