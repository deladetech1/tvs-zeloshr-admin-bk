namespace ZelosHR.Api.Entities.CustomFields;

/// <summary>Query filters for <c>GET /api/v1/custom-fields</c>.</summary>
public sealed class CustomFieldListQuery
{
    public string? Search { get; init; }
    public string? EntityType { get; init; }
    public string? FieldKey { get; init; }
    public string? Label { get; init; }
    public string? FieldType { get; init; }
    public bool? IsRequired { get; init; }
    public bool? IsSensitive { get; init; }
    public bool? IsFilterable { get; init; }
    public bool? IsSearchable { get; init; }
    public bool? IsActive { get; init; }
    public string? SectionName { get; init; }
    public bool IncludeDeleted { get; init; }
    public string? SortBy { get; init; }
    public string? SortOrder { get; init; }
    public int Page { get; init; } = 1;
    public int Size { get; init; } = 20;
}
