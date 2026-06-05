using ZelosHR.Api.Entities.Employees;

namespace ZelosHR.Api.Entities.CustomFields;

/// <summary>Maps custom field definitions and resolves platform user ids to display names on read.</summary>
public static class CustomFieldDefinitionMapper
{
    internal static CustomFieldDefinitionDto EnrichAuthors(
        CustomFieldDefinitionDto source,
        IReadOnlyDictionary<string, CpUserDto> users) =>
        new()
        {
            Id = source.Id,
            EntityType = source.EntityType,
            FieldKey = source.FieldKey,
            Label = source.Label,
            Description = source.Description,
            FieldType = source.FieldType,
            IsRequired = source.IsRequired,
            IsSensitive = source.IsSensitive,
            IsFilterable = source.IsFilterable,
            IsSearchable = source.IsSearchable,
            DisplayOrder = source.DisplayOrder,
            SectionName = source.SectionName,
            SectionOrder = source.SectionOrder,
            Options = source.Options,
            ValidationRules = source.ValidationRules,
            DefaultValue = source.DefaultValue,
            Placeholder = source.Placeholder,
            IsActive = source.IsActive,
            IsDeleted = source.IsDeleted,
            CreatedAt = source.CreatedAt,
            UpdatedAt = source.UpdatedAt,
            CreatedById = source.CreatedById,
            UpdatedById = source.UpdatedById,
            CreatedBy = ResolveDisplayName(source.CreatedById, users),
            UpdatedBy = ResolveDisplayName(source.UpdatedById, users),
        };

    internal static IReadOnlyList<CustomFieldDefinitionDto> EnrichAuthors(
        IReadOnlyList<CustomFieldDefinitionDto> items,
        IReadOnlyDictionary<string, CpUserDto> users) =>
        items.Select(i => EnrichAuthors(i, users)).ToList();

    internal static CustomFieldAuditLogDto EnrichChangedBy(
        CustomFieldAuditLogDto source,
        IReadOnlyDictionary<string, CpUserDto> users) =>
        new()
        {
            Id = source.Id,
            EntityType = source.EntityType,
            EntityId = source.EntityId,
            FieldKey = source.FieldKey,
            OldValue = source.OldValue,
            NewValue = source.NewValue,
            ChangedById = source.ChangedById,
            ChangedBy = ResolveDisplayName(source.ChangedById, users) ?? source.ChangedById,
            ChangedAt = source.ChangedAt,
            ChangeType = source.ChangeType,
        };

    internal static IReadOnlyList<CustomFieldAuditLogDto> EnrichChangedBy(
        IReadOnlyList<CustomFieldAuditLogDto> items,
        IReadOnlyDictionary<string, CpUserDto> users) =>
        items.Select(i => EnrichChangedBy(i, users)).ToList();

    internal static IEnumerable<string> CollectUserIds(IEnumerable<CustomFieldDefinitionDto> items) =>
        items.SelectMany(i => new[] { i.CreatedById, i.UpdatedById })
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!);

    internal static IEnumerable<string> CollectUserIds(IEnumerable<CustomFieldAuditLogDto> items) =>
        items.Select(i => i.ChangedById)
            .Where(id => !string.IsNullOrWhiteSpace(id));

    internal static string? ResolveDisplayName(
        string? userId,
        IReadOnlyDictionary<string, CpUserDto> users)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return null;

        return users.TryGetValue(userId, out var user) ? user.FullName : "Unknown user";
    }
}
