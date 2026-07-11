namespace ZelosHR.Api.Entities.Employees;

/// <summary>Id-based upsert rules for education/certification rows on <c>PUT /employees/update</c>.</summary>
internal static class EmployeeSubResourceUpsertRules
{
    internal static bool HasPersistedId(Guid? id) => id is { } value && value != Guid.Empty;

    /// <summary>Update only when <paramref name="id"/> matches a row already on the employee (from GET).</summary>
    internal static bool ShouldUpdateExisting(Guid? id, IReadOnlySet<Guid> existingIds) =>
        HasPersistedId(id) && existingIds.Contains(id!.Value);

    internal static Dictionary<string, string>? ValidateDuplicateIds(
        IReadOnlyList<EmployeeEducationUpsertDto> items) =>
        ValidateDuplicateIds(items, "education", i => i.Id);

    internal static Dictionary<string, string>? ValidateDuplicateIds(
        IReadOnlyList<EmployeeCertificationUpsertDto> items) =>
        ValidateDuplicateIds(items, "certifications", i => i.Id);

    internal static Dictionary<string, string>? ValidateDuplicateIds(
        IReadOnlyList<EmployeeIdentificationUpsertDto> items) =>
        ValidateDuplicateIds(items, "identity.identifications", i => i.Id);

    internal static Dictionary<string, string>? ValidateDuplicateIds(
        IReadOnlyList<EmployeeEmergencyContactUpsertDto> items) =>
        ValidateDuplicateIds(items, "identity.emergency", i => i.Id);

    internal static Dictionary<string, string>? ValidateDuplicateIds(
        IReadOnlyList<EmployeePaymentMethodUpsertDto> items) =>
        ValidateDuplicateIds(items, "compensation.payment", i => i.Id);

    internal static Dictionary<string, string>? ValidateDuplicateIds(
        IReadOnlyList<EmployeeMedicalConditionUpsertDto> items) =>
        ValidateDuplicateIds(items, "medical.medical_conditions", i => i.Id);

    internal static Dictionary<string, string>? ValidateDuplicateIds(
        IReadOnlyList<EmployeeAllergyUpsertDto> items) =>
        ValidateDuplicateIds(items, "medical.allergies", i => i.Id);

    internal static Dictionary<string, string>? ValidateDuplicateIds(
        IReadOnlyList<EmployeeMedicationUpsertDto> items) =>
        ValidateDuplicateIds(items, "medical.medications", i => i.Id);

    internal static Dictionary<string, string>? ValidateDuplicateIds(
        IReadOnlyList<EmployeeSkillUpsertDto> items) =>
        ValidateDuplicateIds(items, "skills", i => i.Id);

    internal static Dictionary<string, string>? ValidateDuplicateIds(
        IReadOnlyList<EmployeeExperienceUpsertDto> items) =>
        ValidateDuplicateIds(items, "experiences", i => i.Id);

    internal static Dictionary<string, string>? ValidateDuplicateIds(
        IReadOnlyList<EmployeeReferralUpsertDto> items) =>
        ValidateDuplicateIds(items, "referrals", i => i.Id);

    internal static Dictionary<string, string>? ValidateDuplicateIds<T>(
        IReadOnlyList<T> items,
        string fieldPrefix,
        Func<T, Guid?> getId)
    {
        var errors = new Dictionary<string, string>(StringComparer.Ordinal);
        var seen = new HashSet<Guid>();
        for (var i = 0; i < items.Count; i++)
        {
            if (!HasPersistedId(getId(items[i])))
                continue;

            var id = getId(items[i])!.Value;
            if (!seen.Add(id))
                errors[$"{fieldPrefix}[{i}].id"] = $"Duplicate id in the same request.";
        }

        return errors.Count > 0 ? errors : null;
    }

    internal static Dictionary<string, string>? ValidateDuplicateIdCardTypeIds(
        IReadOnlyList<EmployeeIdentificationUpsertDto> items)
    {
        var errors = new Dictionary<string, string>(StringComparer.Ordinal);
        var seen = new HashSet<Guid>();
        for (var i = 0; i < items.Count; i++)
        {
            if (items[i].IdCardTypeId == Guid.Empty)
                continue;

            if (!seen.Add(items[i].IdCardTypeId))
                errors[$"identity.identifications[{i}].id_card_type_id"] =
                    "Duplicate id_card_type_id in the same request.";
        }

        return errors.Count > 0 ? errors : null;
    }
}
