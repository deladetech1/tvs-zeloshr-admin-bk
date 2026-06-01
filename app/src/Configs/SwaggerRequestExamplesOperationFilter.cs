using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using ZelosHR.Api.Entities.Employees;

namespace ZelosHR.Api.Configs;

/// <summary>Attaches named request-body examples to key operations.</summary>
public sealed class SwaggerRequestExamplesOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation.RequestBody?.Content is null
            || !operation.RequestBody.Content.TryGetValue("application/json", out var media))
            return;

        var method = context.ApiDescription.HttpMethod ?? "";
        var path = context.ApiDescription.RelativePath ?? "";

        var examples = ResolveExamples(method, path);
        if (examples.Count == 0)
            return;

        SwaggerMediaExamples.SetNamedExamples(media, examples);
    }

    private static Dictionary<string, IOpenApiExample> ResolveExamples(string method, string path)
    {
        if (method.Equals("POST", StringComparison.OrdinalIgnoreCase)
            && path.Equals("api/v1/employees/add", StringComparison.OrdinalIgnoreCase))
        {
            return new Dictionary<string, IOpenApiExample>
            {
                ["finalised_full_profile"] = Example(
                    SwaggerExamples.CreateEmployeeFinalised(),
                    "Finalised — full profile",
                    "Creates employee and links cp_users when work_email is set. Includes compensation, education, certifications, documents, and custom field values."),
                ["draft_minimal"] = Example(
                    SwaggerExamples.CreateEmployeeDraft(),
                    "Draft — minimal",
                    "Saves a draft with name and basic employment/compensation only. Finalise later via PUT /employees/update with status finalised."),
            };
        }

        if (method.Equals("PUT", StringComparison.OrdinalIgnoreCase)
            && path.Equals("api/v1/employees/update", StringComparison.OrdinalIgnoreCase))
        {
            return new Dictionary<string, IOpenApiExample>
            {
                ["full_profile_update"] = Example(
                    SwaggerExamples.UpdateEmployeeFull(),
                    "Full profile update",
                    """
                    Same aggregate shape as POST /add plus required id (employee UUID).
                    Send the complete profile or only the sections/fields you want to change.
                    document_ids appends; delete_document_ids removes registry IDs.
                    """),
            };
        }

        if (method.Equals("POST", StringComparison.OrdinalIgnoreCase)
            && path.Equals("api/v1/custom-fields/add", StringComparison.OrdinalIgnoreCase))
        {
            return new Dictionary<string, IOpenApiExample>
            {
                ["identity_text"] = Example(
                    SwaggerExamples.CreateCustomFieldIdentity(),
                    "Identity text field",
                    $"Defines emergency_contact_name for section_name {EmployeeCustomFieldSections.Identity}. Values go in identity.custom_fields."),
                ["employment_text"] = Example(
                    SwaggerExamples.CreateCustomFieldEmployment(),
                    "Employment text field",
                    $"Defines desk_number for section_name {EmployeeCustomFieldSections.Employment}. Values go in employment.custom_fields."),
                ["compensation_select"] = Example(
                    SwaggerExamples.CreateCustomFieldCompensation(),
                    "Compensation select field",
                    $"Defines bonus_eligible for section_name {EmployeeCustomFieldSections.Compensation}. Values go in compensation.custom_fields."),
                ["education_text"] = Example(
                    SwaggerExamples.CreateCustomFieldEducation(),
                    "Education text field",
                    $"Defines honors for section_name {EmployeeCustomFieldSections.Education}. Values go in education[].custom_fields."),
                ["certification_text"] = Example(
                    SwaggerExamples.CreateCustomFieldCertification(),
                    "Certification text field",
                    $"Defines verified for section_name {EmployeeCustomFieldSections.Certification}. Values go in certifications[].custom_fields."),
            };
        }

        return [];
    }

    private static OpenApiExample Example(JsonObject value, string summary, string description) =>
        new()
        {
            Summary = summary,
            Description = description,
            Value = value,
        };
}
