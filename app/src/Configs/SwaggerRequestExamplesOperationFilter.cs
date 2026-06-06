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
                    "Creates employee and links cp_users when work_email is set. Attach files via document_ids (from POST /file/post/multiple). Read response returns documents[] (DocumentReadDto with presigned URLs)."),
                ["draft_minimal"] = Example(
                    SwaggerExamples.CreateEmployeeDraft(),
                    "Draft — minimal",
                    "Only identity.full_name and identity.phone are required. All other sections and fields are optional on create."),
            };
        }

        if (method.Equals("PUT", StringComparison.OrdinalIgnoreCase)
            && path.Equals("api/v1/employees/update", StringComparison.OrdinalIgnoreCase))
        {
            return new Dictionary<string, IOpenApiExample>
            {
                ["partial_identity"] = Example(
                    SwaggerExamples.UpdateEmployeePartialIdentity(),
                    "Partial — identity only",
                    """
                    Send only the section/fields you change. identity fields are merged — you do not need full_name on every save unless changing it.
                    """),
                ["education_cert_upsert"] = Example(
                    SwaggerExamples.UpdateEmployeeEducationCertUpsert(),
                    "Bulk edit — education + certifications with id (sync false)",
                    """
                    Default sync=false: patch mode. Only rows in the body are upserted; all other rows stay.
                    Round-trip id from GET to update; omit id to add.
                    """),
                ["patch_one_certification"] = Example(
                    SwaggerExamples.UpdateEmployeePatchOneCertification(),
                    "Patch one certification (sync false)",
                    """
                    Employee has 2+ certifications. Send one item with id — only that row updates; others unchanged.
                    sync_certifications omitted or false.
                    """),
                ["sync_replace_certifications"] = Example(
                    SwaggerExamples.UpdateEmployeeSyncCertificationsReplace(),
                    "sync_certifications — replace full list",
                    """
                    sync_certifications: true means the certifications[] array IS the full desired set.
                    Any existing certification not in the array is deleted. Use to collapse duplicates or reset the section.
                    Send sync_certifications: true + certifications: [] to delete all certifications.
                    """),
                ["sync_replace_education"] = Example(
                    SwaggerExamples.UpdateEmployeeSyncEducationReplace(),
                    "sync_education — replace full list",
                    """
                    sync_education: true means the education[] array IS the full desired set.
                    Any existing education row not in the array is deleted after upsert.
                    """),
                ["add_sub_rows"] = Example(
                    SwaggerExamples.UpdateEmployeeAddSubRows(),
                    "Add education/certification rows",
                    "New rows: omit id on each item. institution (education) and name (certifications) are required."),
                ["sync_and_delete"] = Example(
                    SwaggerExamples.UpdateEmployeeSyncAndDelete(),
                    "sync_education + delete_certification_ids",
                    """
                    sync_education: true + full education[] replaces the education section.
                    delete_certification_ids removes specific certification rows without sending certifications[].
                    """),
                ["full_profile_update"] = Example(
                    SwaggerExamples.UpdateEmployeeFull(),
                    "Full profile update",
                    """
                    All sections optional — send only what you need. Same row shapes as POST /add for education/certifications,
                    but include id from GET on existing rows. document_ids appends; delete_document_ids removes registry IDs.
                    """),
            };
        }

        if (method.Equals("POST", StringComparison.OrdinalIgnoreCase)
            && path.Equals("api/v1/custom-fields/add", StringComparison.OrdinalIgnoreCase))
        {
            return new Dictionary<string, IOpenApiExample>
            {
                ["create_custom_field_definition"] = Example(
                    SwaggerExamples.CreateCustomFieldAddBody(),
                    "Create custom field definition",
                    SwaggerExamples.CreateCustomFieldAddExampleDescription()),
            };
        }

        if (method.Equals("PUT", StringComparison.OrdinalIgnoreCase)
            && path.Equals("api/v1/custom-fields/update", StringComparison.OrdinalIgnoreCase))
        {
            return new Dictionary<string, IOpenApiExample>
            {
                ["update_label_and_required"] = Example(
                    SwaggerExamples.UpdateCustomFieldBody(),
                    "Update label and required flag",
                    "Send only fields to change. custom_field_id is required on the query string."),
            };
        }

        if (method.Equals("POST", StringComparison.OrdinalIgnoreCase)
            && path.Equals("api/v1/org-structure/departments/add", StringComparison.OrdinalIgnoreCase))
        {
            return new Dictionary<string, IOpenApiExample>
            {
                ["root_department"] = Example(
                    SwaggerExamples.CreateDepartmentRoot(),
                    "Root department",
                    "Top-level department with optional head_of_department_id (employee UUID)."),
                ["child_department"] = Example(
                    SwaggerExamples.CreateDepartmentChild(),
                    "Child department",
                    "Nested under parent_department_id from an existing department."),
            };
        }

        if (method.Equals("PUT", StringComparison.OrdinalIgnoreCase)
            && path.Equals("api/v1/org-structure/departments/update", StringComparison.OrdinalIgnoreCase))
        {
            return new Dictionary<string, IOpenApiExample>
            {
                ["rename_and_reassign_head"] = Example(
                    SwaggerExamples.UpdateDepartmentBody(),
                    "Rename and assign head",
                    "Send only fields to change. department_id is required on the query string."),
            };
        }

        if (method.Equals("POST", StringComparison.OrdinalIgnoreCase)
            && path.Equals("api/v1/org-structure/branches/add", StringComparison.OrdinalIgnoreCase))
        {
            return new Dictionary<string, IOpenApiExample>
            {
                ["branch"] = Example(
                    SwaggerExamples.CreateBranchBody(),
                    "New branch",
                    "Creates a branch location for employee assignment."),
            };
        }

        if (method.Equals("PUT", StringComparison.OrdinalIgnoreCase)
            && path.Equals("api/v1/org-structure/branches/update", StringComparison.OrdinalIgnoreCase))
        {
            return new Dictionary<string, IOpenApiExample>
            {
                ["rename"] = Example(
                    SwaggerExamples.UpdateBranchBody(),
                    "Rename branch",
                    "branch_id is required on the query string."),
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
