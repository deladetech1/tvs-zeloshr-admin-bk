namespace ZelosHR.Api.Entities.Employees;

internal static class EmployeeAggregateCreateValidator
{
    private const int MaxEducation = 20;
    private const int MaxCertifications = 50;
    private const int MaxIdentifications = 10;
    private const int MaxEmergency = 10;
    private const int MaxPayment = 5;
    private const int MaxMedicalConditions = 20;
    private const int MaxAllergies = 20;
    private const int MaxMedications = 20;
    private const int MaxSkills = 50;
    private const int MaxExperiences = 20;
    private const int MaxReferrals = 10;

    internal static Dictionary<string, string>? Validate(CreateEmployeeAggregateRequest request)
    {
        var errors = new Dictionary<string, string>();

        if (string.IsNullOrWhiteSpace(request.Identity.FullName))
            errors["identity.full_name"] = "Full name is required.";

        if (string.IsNullOrWhiteSpace(request.Identity.Phone))
            errors["identity.phone"] = "Phone is required.";

        var willFinalise = !request.IsDraft && !string.IsNullOrWhiteSpace(request.Identity.WorkEmail);
        if (willFinalise && string.IsNullOrWhiteSpace(request.Employment?.EmploymentStatus))
            errors["employment.employment_status"] =
                "employment_status is required when creating a finalised employee.";

        if (request.Education.Count > MaxEducation)
            errors["education"] = $"At most {MaxEducation} education records allowed.";

        for (var i = 0; i < request.Education.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(request.Education[i].Institution))
                errors[$"education[{i}].institution"] = "Institution is required.";
        }

        if (request.Certifications.Count > MaxCertifications)
            errors["certifications"] = $"At most {MaxCertifications} certification records allowed.";

        for (var i = 0; i < request.Certifications.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(request.Certifications[i].Name))
                errors[$"certifications[{i}].name"] = "Name is required.";
        }

        var identifications = request.Identity.Identifications ?? [];
        if (identifications.Count > MaxIdentifications)
            errors["identity.identifications"] = $"At most {MaxIdentifications} identification records allowed.";

        var duplicateIdErrors = identifications.Count > 0
            ? EmployeeSubResourceUpsertRules.ValidateDuplicateIdCardTypeIds(identifications)
            : null;
        if (duplicateIdErrors is not null)
        {
            foreach (var (key, message) in duplicateIdErrors)
                errors[key] = message;
        }

        for (var i = 0; i < identifications.Count; i++)
        {
            if (identifications[i].IdCardTypeId == Guid.Empty)
                errors[$"identity.identifications[{i}].id_card_type_id"] = "id_card_type_id is required.";

            if (string.IsNullOrWhiteSpace(identifications[i].IdCardTypeNumber))
                errors[$"identity.identifications[{i}].id_card_type_number"] = "id_card_type_number is required.";
        }

        var emergency = request.Identity.Emergency ?? [];
        if (emergency.Count > MaxEmergency)
            errors["identity.emergency"] = $"At most {MaxEmergency} emergency contacts allowed.";

        var payment = request.Compensation?.Payment ?? [];
        if (payment.Count > MaxPayment)
            errors["compensation.payment"] = $"At most {MaxPayment} payment methods allowed.";

        if (request.Medical?.MedicalConditions is { Count: > MaxMedicalConditions })
            errors["medical.medical_conditions"] = $"At most {MaxMedicalConditions} medical conditions allowed.";

        if (request.Medical?.Allergies is { Count: > MaxAllergies })
            errors["medical.allergies"] = $"At most {MaxAllergies} allergies allowed.";

        if (request.Medical?.Medications is { Count: > MaxMedications })
            errors["medical.medications"] = $"At most {MaxMedications} medications allowed.";

        if (request.Skills.Count > MaxSkills)
            errors["skills"] = $"At most {MaxSkills} skills allowed.";

        if (request.Experiences.Count > MaxExperiences)
            errors["experiences"] = $"At most {MaxExperiences} experiences allowed.";

        if (request.Referrals.Count > MaxReferrals)
            errors["referrals"] = $"At most {MaxReferrals} referrals allowed.";

        return errors.Count == 0 ? null : errors;
    }
}
