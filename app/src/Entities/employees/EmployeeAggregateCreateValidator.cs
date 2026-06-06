namespace ZelosHR.Api.Entities.Employees;

internal static class EmployeeAggregateCreateValidator
{
    private const int MaxEducation = 20;
    private const int MaxCertifications = 50;

    internal static Dictionary<string, string>? Validate(CreateEmployeeAggregateRequest request)
    {
        var errors = new Dictionary<string, string>();

        if (string.IsNullOrWhiteSpace(request.Identity.FullName))
            errors["identity.full_name"] = "Full name is required.";

        if (string.IsNullOrWhiteSpace(request.Identity.Phone))
            errors["identity.phone"] = "Phone is required.";

        if (!string.IsNullOrWhiteSpace(request.Status)
            && !string.Equals(request.Status, "finalised", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(request.Status, "draft", StringComparison.OrdinalIgnoreCase))
        {
            errors["status"] = "Status must be 'draft' or 'finalised'.";
        }

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

        return errors.Count == 0 ? null : errors;
    }
}
