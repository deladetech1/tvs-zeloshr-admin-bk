using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

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

        media.Examples = examples;
        if (examples.Count == 1)
            media.Example = examples.Values.First().Value;
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
                    "Saves a draft with name and basic employment/compensation only. Finalise later via PUT /employees/update with status: finalised."),
            };
        }

        if (method.Equals("PUT", StringComparison.OrdinalIgnoreCase)
            && path.Equals("api/v1/employees/update", StringComparison.OrdinalIgnoreCase))
        {
            return new Dictionary<string, IOpenApiExample>
            {
                ["partial_update"] = Example(
                    SwaggerExamples.UpdateEmployeePartial(),
                    "Partial update + finalise",
                    "Only send sections/fields to change. Include id (employee UUID). document_ids appends; delete_document_ids removes."),
            };
        }

        if (method.Equals("POST", StringComparison.OrdinalIgnoreCase)
            && path.Equals("api/v1/custom-fields/add", StringComparison.OrdinalIgnoreCase))
        {
            return new Dictionary<string, IOpenApiExample>
            {
                ["compensation_select"] = Example(
                    SwaggerExamples.CreateCustomFieldCompensation(),
                    "Compensation select field",
                    "Defines bonus_eligible for section_name compensation. Frontend reads schema then sends values in compensation.custom_fields on employee create/update."),
                ["identity_text"] = Example(
                    SwaggerExamples.CreateCustomFieldIdentity(),
                    "Identity text field",
                    "Defines emergency_contact_name for section_name identity."),
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
