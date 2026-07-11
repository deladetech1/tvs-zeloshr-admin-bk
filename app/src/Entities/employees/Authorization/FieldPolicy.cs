namespace ZelosHR.Api.Entities.Employees.Authorization;

/// <summary>
/// Single source of truth for employee self-service field edit tiers.
/// Paths use dot notation matching the JSON update payload.
/// Anything not listed is admin-only (deny-by-default).
/// </summary>
public static class FieldPolicy
{
    public static readonly IReadOnlyDictionary<string, FieldAccess> EmployeeAccess =
        new Dictionary<string, FieldAccess>(StringComparer.OrdinalIgnoreCase)
        {
            ["identity.phone"] = FieldAccess.Free,
            ["identity.personal_email"] = FieldAccess.Free,
            ["identity.linked_in_url"] = FieldAccess.Free,
            ["identity.profile_url"] = FieldAccess.Free,
            ["identity.residential_address"] = FieldAccess.Free,
            ["identity.next_of_kin_name"] = FieldAccess.Free,
            ["identity.next_of_kin_phone"] = FieldAccess.Free,
            ["identity.relationship_to_next_of_kin"] = FieldAccess.Free,
            ["identity.emergency"] = FieldAccess.Free,
            ["identity.custom_fields"] = FieldAccess.Free,

            ["identity.full_name"] = FieldAccess.Approval,
            ["identity.date_of_birth"] = FieldAccess.Approval,
            ["identity.gender"] = FieldAccess.Approval,
            ["identity.country"] = FieldAccess.Approval,
            ["identity.marital_status"] = FieldAccess.Approval,
            ["identity.identifications"] = FieldAccess.Approval,

            ["compensation.ssnit_insurance_number"] = FieldAccess.Approval,
            ["compensation.payment"] = FieldAccess.Approval,

            ["education"] = FieldAccess.Approval,
            ["certifications"] = FieldAccess.Approval,

            ["skills"] = FieldAccess.Free,
            ["experiences"] = FieldAccess.Free,
            ["referrals"] = FieldAccess.Free,

            ["medical.blood_group"] = FieldAccess.Free,
            ["medical.has_medical_condition"] = FieldAccess.Free,
            ["medical.medical_conditions"] = FieldAccess.Free,
            ["medical.allergies"] = FieldAccess.Free,
            ["medical.takes_regular_medication"] = FieldAccess.Free,
            ["medical.medications"] = FieldAccess.Free,
            ["medical.emergency_medical_notes"] = FieldAccess.Free,

            ["medical.disability_status"] = FieldAccess.Approval,
            ["medical.requires_accommodation"] = FieldAccess.Approval,
            ["medical.accommodation_details"] = FieldAccess.Approval,
        };

    public static FieldAccess ForEmployee(string fieldPath) =>
        EmployeeAccess.TryGetValue(fieldPath, out var access) ? access : FieldAccess.None;

    public static IReadOnlyList<FieldPolicyEntryDto> ListForEmployeeUi() =>
        EmployeeAccess
            .OrderBy(e => e.Key, StringComparer.OrdinalIgnoreCase)
            .Select(e => new FieldPolicyEntryDto
            {
                Path = e.Key,
                Access = e.Value.ToString().ToLowerInvariant(),
            })
            .ToList();
}

public sealed class FieldPolicyEntryDto
{
    public required string Path { get; init; }
    public required string Access { get; init; }
}
