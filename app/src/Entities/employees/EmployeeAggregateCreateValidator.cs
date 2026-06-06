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

        if (request.Certifications.Count > MaxCertifications)
            errors["certifications"] = $"At most {MaxCertifications} certification records allowed.";

        if (request.Employment is not null)
        {
            var branchRule = WorkArrangementRules.ValidateBranchForArrangement(
                request.Employment.WorkArrangement,
                request.Employment.BranchId);
            if (branchRule is not null)
            {
                foreach (var (key, message) in branchRule)
                    errors[key] = message;
            }
        }

        return errors.Count == 0 ? null : errors;
    }
}
