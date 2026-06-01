using ZelosHR.Api.Entities.CustomFields;

namespace ZelosHR.Api.Entities.Employees;

internal static class EmployeeCustomFieldMapper
{
    public static Dictionary<string, string?> MergeIntoFlat(
        Dictionary<string, string?> currentFlat,
        IReadOnlyList<CustomFieldDefinitionDto> definitions,
        Dictionary<string, string?>? identityFields,
        Dictionary<string, string?>? employmentFields,
        Dictionary<string, string?>? compensationFields,
        IEnumerable<Dictionary<string, string?>?> educationFieldSets,
        IEnumerable<Dictionary<string, string?>?> certificationFieldSets)
    {
        var merged = new Dictionary<string, string?>(currentFlat, StringComparer.OrdinalIgnoreCase);

        ApplySection(merged, definitions, EmployeeCustomFieldSections.Identity, identityFields);
        ApplySection(merged, definitions, EmployeeCustomFieldSections.Employment, employmentFields);
        ApplySection(merged, definitions, EmployeeCustomFieldSections.Compensation, compensationFields);

        var educationMerged = MergeFieldSets(educationFieldSets);
        if (educationMerged is not null)
            ApplySection(merged, definitions, EmployeeCustomFieldSections.Education, educationMerged);

        var certificationMerged = MergeFieldSets(certificationFieldSets);
        if (certificationMerged is not null)
            ApplySection(merged, definitions, EmployeeCustomFieldSections.Certification, certificationMerged);

        return merged;
    }

    public static EmployeeCustomFieldSectionsDto SplitFromFlat(
        Dictionary<string, string?> flat,
        IReadOnlyList<CustomFieldDefinitionDto> definitions)
    {
        var sections = EmployeeCustomFieldSections.All.ToDictionary(
            s => s,
            _ => new Dictionary<string, string?>(),
            StringComparer.OrdinalIgnoreCase);

        foreach (var def in definitions)
        {
            var section = NormalizeSection(def.SectionName);
            if (!flat.TryGetValue(def.FieldKey, out var value))
                continue;

            sections[section][def.FieldKey] = value;
        }

        return new EmployeeCustomFieldSectionsDto
        {
            Identity = sections[EmployeeCustomFieldSections.Identity],
            Employment = sections[EmployeeCustomFieldSections.Employment],
            Compensation = sections[EmployeeCustomFieldSections.Compensation],
            Education = sections[EmployeeCustomFieldSections.Education],
            Certification = sections[EmployeeCustomFieldSections.Certification],
        };
    }

    private static void ApplySection(
        Dictionary<string, string?> merged,
        IReadOnlyList<CustomFieldDefinitionDto> definitions,
        string section,
        Dictionary<string, string?>? incoming)
    {
        if (incoming is null)
            return;

        var keysForSection = definitions
            .Where(d => string.Equals(NormalizeSection(d.SectionName), section, StringComparison.OrdinalIgnoreCase))
            .Select(d => d.FieldKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var key in keysForSection)
            merged.Remove(key);

        foreach (var (key, value) in incoming)
        {
            if (!keysForSection.Contains(key))
                continue;

            merged[key] = value;
        }
    }

    private static Dictionary<string, string?>? MergeFieldSets(IEnumerable<Dictionary<string, string?>?> sets)
    {
        Dictionary<string, string?>? merged = null;
        foreach (var set in sets)
        {
            if (set is not { Count: > 0 })
                continue;

            merged ??= new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            foreach (var (key, value) in set)
                merged[key] = value;
        }

        return merged;
    }

    private static string NormalizeSection(string? sectionName)
    {
        if (string.IsNullOrWhiteSpace(sectionName))
            return EmployeeCustomFieldSections.Identity;

        var trimmed = sectionName.Trim();
        if (EmployeeCustomFieldSections.LegacyAliases.TryGetValue(trimmed, out var legacy))
            return legacy;

        return EmployeeCustomFieldSections.All.FirstOrDefault(
            s => string.Equals(s, trimmed, StringComparison.OrdinalIgnoreCase))
            ?? trimmed.ToLowerInvariant();
    }
}

internal sealed class EmployeeCustomFieldSectionsDto
{
    public Dictionary<string, string?> Identity { get; init; } = new();
    public Dictionary<string, string?> Employment { get; init; } = new();
    public Dictionary<string, string?> Compensation { get; init; } = new();
    public Dictionary<string, string?> Education { get; init; } = new();
    public Dictionary<string, string?> Certification { get; init; } = new();
}
