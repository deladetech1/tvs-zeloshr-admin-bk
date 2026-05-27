namespace ZelosHR.Api.Persistence.Entities;

public sealed class CustomFieldDefinitionEntity
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = default!;
    public string OrgId { get; set; } = default!;
    public string EntityType { get; set; } = default!;
    public string FieldKey { get; set; } = default!;
    public string Label { get; set; } = default!;
    public string? Description { get; set; }
    public string FieldType { get; set; } = default!;
    public bool IsRequired { get; set; }
    public bool IsSensitive { get; set; }
    public bool IsFilterable { get; set; }
    public bool IsSearchable { get; set; }
    public int DisplayOrder { get; set; }
    public string? SectionName { get; set; }
    public int SectionOrder { get; set; }
    public string? Options { get; set; }
    public string? ValidationRules { get; set; }
    public string? DefaultValue { get; set; }
    public string? Placeholder { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}

public sealed class CustomFieldAuditLogEntity
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = default!;
    public string OrgId { get; set; } = default!;
    public string EntityType { get; set; } = default!;
    public Guid EntityId { get; set; }
    public string FieldKey { get; set; } = default!;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string ChangedBy { get; set; } = default!;
    public DateTimeOffset ChangedAt { get; set; }
    public string ChangeType { get; set; } = default!;
}
