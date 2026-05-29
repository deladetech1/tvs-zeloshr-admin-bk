using ZelosHR.Api.Configs;

namespace ZelosHR.Api.Entities.CustomFields;

/// <summary>Query filters for <c>GET /api/v1/custom-fields/list</c>.</summary>
public sealed class CustomFieldListQuery
{
    public string? Search { get; init; }

    [SwaggerAllowedValues(typeof(CustomFieldEntityTypes), nameof(CustomFieldEntityTypes.All))]
    public string? EntityType { get; init; }

    public string? FieldKey { get; init; }
    public string? Label { get; init; }

    [SwaggerAllowedValues(typeof(CustomFieldFieldTypes), nameof(CustomFieldFieldTypes.All))]
    public string? FieldType { get; init; }

    public bool? IsRequired { get; init; }
    public bool? IsSensitive { get; init; }
    public bool? IsFilterable { get; init; }
    public bool? IsSearchable { get; init; }
    public bool? IsActive { get; init; }
    public string? SectionName { get; init; }
    public bool IncludeDeleted { get; init; }

    [SwaggerAllowedValues(typeof(CustomFieldListSortOptions), nameof(CustomFieldListSortOptions.SortBy))]
    public string? SortBy { get; init; }

    [SwaggerAllowedValues(typeof(CustomFieldListSortOptions), nameof(CustomFieldListSortOptions.SortOrder))]
    public string? SortOrder { get; init; }

    public int Page { get; init; } = 1;
    public int Size { get; init; } = 20;
}

/// <summary>Query filters for <c>GET /api/v1/custom-fields/audit-logs</c>.</summary>
public sealed class CustomFieldAuditLogQuery
{
    [SwaggerAllowedValues(typeof(CustomFieldEntityTypes), nameof(CustomFieldEntityTypes.All))]
    public string? EntityType { get; init; }

    public Guid? EntityId { get; init; }
    public string? FieldKey { get; init; }

    [SwaggerAllowedValues(typeof(CustomFieldChangeTypes), nameof(CustomFieldChangeTypes.All))]
    public string? ChangeType { get; init; }

    public string? ChangedBy { get; init; }
    public DateTimeOffset? ChangedFrom { get; init; }
    public DateTimeOffset? ChangedTo { get; init; }
    public int Page { get; init; } = 1;
    public int Size { get; init; } = 20;
}
