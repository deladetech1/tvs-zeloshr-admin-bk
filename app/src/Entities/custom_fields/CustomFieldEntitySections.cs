using ZelosHR.Api.Entities.Employees;

namespace ZelosHR.Api.Entities.CustomFields;

/// <summary>Section options per <see cref="CustomFieldEntityTypes"/> for custom field definition forms.</summary>
public static class CustomFieldEntitySections
{
    private static readonly IReadOnlyList<CustomFieldSectionOptionDto> EmployeeSections =
    [
        new() { Value = EmployeeCustomFieldSections.Identity, Label = "Identity" },
        new() { Value = EmployeeCustomFieldSections.Employment, Label = "Employment" },
        new() { Value = EmployeeCustomFieldSections.Compensation, Label = "Compensation" },
        new() { Value = EmployeeCustomFieldSections.Education, Label = "Education" },
        new() { Value = EmployeeCustomFieldSections.Certification, Label = "Certification" },
    ];

    private static readonly IReadOnlyDictionary<string, IReadOnlyList<CustomFieldSectionOptionDto>> ByEntityType =
        new Dictionary<string, IReadOnlyList<CustomFieldSectionOptionDto>>(StringComparer.OrdinalIgnoreCase)
        {
            [CustomFieldEntityTypes.Employee] = EmployeeSections,
        };

    public static IReadOnlyList<CustomFieldSectionOptionDto> ForEntityType(string entityType) =>
        ByEntityType.TryGetValue(entityType, out var sections) ? sections : [];

    public static string? ResolveCanonicalEntityType(string entityType)
    {
        if (string.IsNullOrWhiteSpace(entityType))
            return null;

        return CustomFieldEntityTypes.All.FirstOrDefault(
            t => t.Equals(entityType.Trim(), StringComparison.OrdinalIgnoreCase));
    }
}
