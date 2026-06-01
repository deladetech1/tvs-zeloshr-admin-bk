using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Employees;

namespace ZelosHR.Api.Entities.CustomFields;

public sealed class CustomFieldsSummaryDto
{
    public int TotalDefinitions { get; init; }
    public int ActiveDefinitions { get; init; }
    public int DeletedDefinitions { get; init; }
}

public sealed class CustomFieldDefinitionDto
{
    public required string Id { get; init; }

    [SwaggerAllowedValues(typeof(CustomFieldEntityTypes), nameof(CustomFieldEntityTypes.All))]
    public required string EntityType { get; init; }

    public required string FieldKey { get; init; }
    public required string Label { get; init; }
    public string? Description { get; init; }

    [SwaggerAllowedValues(typeof(CustomFieldFieldTypes), nameof(CustomFieldFieldTypes.All))]
    public required string FieldType { get; init; }

    public bool IsRequired { get; init; }
    public bool IsSensitive { get; init; }
    public bool IsFilterable { get; init; }
    public bool IsSearchable { get; init; }
    public int DisplayOrder { get; init; }
    [SwaggerAllowedValues(typeof(EmployeeCustomFieldSections), nameof(EmployeeCustomFieldSections.All))]
    public string? SectionName { get; init; }
    public int SectionOrder { get; init; }
    public string? Options { get; init; }
    public string? ValidationRules { get; init; }
    public string? DefaultValue { get; init; }
    public string? Placeholder { get; init; }
    public bool IsActive { get; init; }
    public bool IsDeleted { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public string? CreatedBy { get; init; }
    public string? UpdatedBy { get; init; }
}

public sealed class CustomFieldDefinitionListDto
{
    public CustomFieldsSummaryDto Summary { get; init; } = new();
    public IReadOnlyList<CustomFieldDefinitionDto> Items { get; init; } = [];
}

public sealed class CustomFieldSchemaDto
{
    [SwaggerAllowedValues(typeof(CustomFieldEntityTypes), nameof(CustomFieldEntityTypes.All))]
    public required string EntityType { get; init; }

    public IReadOnlyList<CustomFieldDefinitionDto> Fields { get; init; } = [];
}

public sealed class CreateCustomFieldDefinitionDto
{
    /// <summary>Target entity. Use <c>employee</c> for employee profile fields.</summary>
    [SwaggerAllowedValues(typeof(CustomFieldEntityTypes), nameof(CustomFieldEntityTypes.All))]
    public required string EntityType { get; set; }

    /// <summary>Stable key used in employee <c>custom_fields</c> objects (snake_case recommended).</summary>
    public required string FieldKey { get; set; }

    /// <summary>Human-readable label shown in UI.</summary>
    public required string Label { get; set; }

    public string? Description { get; set; }

    [SwaggerAllowedValues(typeof(CustomFieldFieldTypes), nameof(CustomFieldFieldTypes.All))]
    public required string FieldType { get; set; }

    public bool IsRequired { get; set; }
    public bool IsSensitive { get; set; }
    public bool IsFilterable { get; set; } = true;
    public bool IsSearchable { get; set; } = true;
    public int DisplayOrder { get; set; }

    /// <summary>Which employee section receives values for this field (see Allowed on schema).</summary>
    [SwaggerAllowedValues(typeof(EmployeeCustomFieldSections), nameof(EmployeeCustomFieldSections.All),
        Description = "One section per definition — use employee-directory-identity | employee-directory-employment | employee-directory-compensation | employee-directory-education | employee-directory-certification.")]
    public string? SectionName { get; set; }

    public int SectionOrder { get; set; }

    /// <summary>JSON array on the wire; UI choices shown as yes | no in examples (select | multiselect).</summary>
    public string? Options { get; set; }
    public string? ValidationRules { get; set; }
    public string? DefaultValue { get; set; }
    public string? Placeholder { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class UpdateCustomFieldDefinitionDto
{
    public string? Label { get; set; }
    public string? Description { get; set; }

    [SwaggerAllowedValues(typeof(CustomFieldFieldTypes), nameof(CustomFieldFieldTypes.All))]
    public string? FieldType { get; set; }

    public bool? IsRequired { get; set; }
    public bool? IsSensitive { get; set; }
    public bool? IsFilterable { get; set; }
    public bool? IsSearchable { get; set; }
    public int? DisplayOrder { get; set; }
    [SwaggerAllowedValues(typeof(EmployeeCustomFieldSections), nameof(EmployeeCustomFieldSections.All))]
    public string? SectionName { get; set; }
    public int? SectionOrder { get; set; }
    public string? Options { get; set; }
    public string? ValidationRules { get; set; }
    public string? DefaultValue { get; set; }
    public string? Placeholder { get; set; }
    public bool? IsActive { get; set; }
}

public sealed class ReorderCustomFieldDefinitionDto
{
    public required IReadOnlyList<ReorderCustomFieldItemDto> Items { get; set; }
}

public sealed class ReorderCustomFieldItemDto
{
    public required Guid Id { get; set; }
    public int DisplayOrder { get; set; }
    public int? SectionOrder { get; set; }
}

public sealed class CustomFieldAuditLogDto
{
    public required string Id { get; init; }

    [SwaggerAllowedValues(typeof(CustomFieldEntityTypes), nameof(CustomFieldEntityTypes.All))]
    public required string EntityType { get; init; }

    public required string EntityId { get; init; }
    public required string FieldKey { get; init; }
    public string? OldValue { get; init; }
    public string? NewValue { get; init; }
    public required string ChangedBy { get; init; }
    public DateTimeOffset ChangedAt { get; init; }

    [SwaggerAllowedValues(typeof(CustomFieldChangeTypes), nameof(CustomFieldChangeTypes.All))]
    public required string ChangeType { get; init; }
}

public sealed class CustomFieldAuditLogListDto
{
    public IReadOnlyList<CustomFieldAuditLogDto> Items { get; init; } = [];
}
