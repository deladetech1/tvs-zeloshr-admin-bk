namespace ZelosHR.Api.Entities.Employees;

internal static class EmployeeAggregateCreateValidator
{
    private const int MaxEducation = 20;
    private const int MaxCertifications = 50;
    private const int MaxIdentifications = 10;

    internal static Dictionary<string, string>? Validate(CreateEmployeeAggregateRequest request)
    {
        var errors = new Dictionary<string, string>();

        if (string.IsNullOrWhiteSpace(request.Identity.FullName))
            errors["identity.full_name"] = "Full name is required.";

        if (string.IsNullOrWhiteSpace(request.Identity.Phone))
            errors["identity.phone"] = "Phone is required.";

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
            ? EmployeeSubResourceUpsertRules.ValidateDuplicateIdTypeIds(identifications)
            : null;
        if (duplicateIdErrors is not null)
        {
            foreach (var (key, message) in duplicateIdErrors)
                errors[key] = message;
        }

        for (var i = 0; i < identifications.Count; i++)
        {
            if (identifications[i].IdTypeId == Guid.Empty)
                errors[$"identity.identifications[{i}].id_type_id"] = "id_type_id is required.";

            if (string.IsNullOrWhiteSpace(identifications[i].IdNumber))
                errors[$"identity.identifications[{i}].id_number"] = "id_number is required.";
        }

        return errors.Count == 0 ? null : errors;
    }
}
