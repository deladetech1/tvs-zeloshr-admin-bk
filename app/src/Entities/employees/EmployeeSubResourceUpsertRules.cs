namespace ZelosHR.Api.Entities.Employees;

/// <summary>Id-based upsert rules for education/certification rows on <c>PUT /employees/update</c>.</summary>
internal static class EmployeeSubResourceUpsertRules
{
    internal static bool HasPersistedId(Guid? id) => id is { } value && value != Guid.Empty;

    /// <summary>Update only when <paramref name="id"/> matches a row already on the employee (from GET).</summary>
    internal static bool ShouldUpdateExisting(Guid? id, IReadOnlySet<Guid> existingIds) =>
        HasPersistedId(id) && existingIds.Contains(id!.Value);

    internal static Dictionary<string, string>? ValidateDuplicateIds(
        IReadOnlyList<EmployeeEducationUpsertDto> items)
    {
        var errors = new Dictionary<string, string>(StringComparer.Ordinal);
        var seen = new HashSet<Guid>();
        for (var i = 0; i < items.Count; i++)
        {
            if (!HasPersistedId(items[i].Id))
                continue;

            var id = items[i].Id!.Value;
            if (!seen.Add(id))
                errors[$"education[{i}].id"] = "Duplicate education id in the same request.";
        }

        return errors.Count > 0 ? errors : null;
    }

    internal static Dictionary<string, string>? ValidateDuplicateIds(
        IReadOnlyList<EmployeeCertificationUpsertDto> items)
    {
        var errors = new Dictionary<string, string>(StringComparer.Ordinal);
        var seen = new HashSet<Guid>();
        for (var i = 0; i < items.Count; i++)
        {
            if (!HasPersistedId(items[i].Id))
                continue;

            var id = items[i].Id!.Value;
            if (!seen.Add(id))
                errors[$"certifications[{i}].id"] = "Duplicate certification id in the same request.";
        }

        return errors.Count > 0 ? errors : null;
    }

    internal static Dictionary<string, string>? ValidateDuplicateIds(
        IReadOnlyList<EmployeeIdentificationUpsertDto> items)
    {
        var errors = new Dictionary<string, string>(StringComparer.Ordinal);
        var seen = new HashSet<Guid>();
        for (var i = 0; i < items.Count; i++)
        {
            if (!HasPersistedId(items[i].Id))
                continue;

            var id = items[i].Id!.Value;
            if (!seen.Add(id))
                errors[$"identity.identifications[{i}].id"] = "Duplicate identification id in the same request.";
        }

        return errors.Count > 0 ? errors : null;
    }

    internal static Dictionary<string, string>? ValidateDuplicateIdTypeIds(
        IReadOnlyList<EmployeeIdentificationUpsertDto> items)
    {
        var errors = new Dictionary<string, string>(StringComparer.Ordinal);
        var seen = new HashSet<Guid>();
        for (var i = 0; i < items.Count; i++)
        {
            if (items[i].IdTypeId == Guid.Empty)
                continue;

            if (!seen.Add(items[i].IdTypeId))
                errors[$"identity.identifications[{i}].id_type_id"] =
                    "Duplicate id_type_id in the same request.";
        }

        return errors.Count > 0 ? errors : null;
    }
}
