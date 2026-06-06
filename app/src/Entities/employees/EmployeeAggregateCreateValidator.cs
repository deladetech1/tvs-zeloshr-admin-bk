namespace ZelosHR.Api.Entities.Employees;

internal static class EmployeeAggregateCreateValidator
{
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

        var eduErrors = EmployeeEducationSection.ValidateForCreate(request.Education);
        if (eduErrors is not null)
        {
            foreach (var (key, value) in eduErrors)
                errors[key] = value;
        }

        if (request.Certifications.Count > MaxCertifications)
            errors["certifications"] = $"At most {MaxCertifications} certification records allowed.";

        return errors.Count == 0 ? null : errors;
    }
}
