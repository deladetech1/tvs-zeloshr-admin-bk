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
                ["full_profile"] = Example(
                    SwaggerExamples.CreateEmployeeFinalised(),
                    "Full profile",
                    "Creates employee and links cp_users when work_email is set. Attach files via document_ids (from POST /file/post/multiple). Read response returns documents[] (DocumentReadDto with presigned URLs)."),
                ["minimal"] = Example(
                    SwaggerExamples.CreateEmployeeDraft(),
                    "Minimal",
                    "Only identity.full_name and identity.phone are required. Omit work_email to save without linking cp_users yet."),
            };
        }

        if (method.Equals("PUT", StringComparison.OrdinalIgnoreCase)
            && path.Equals("api/v1/employees/update", StringComparison.OrdinalIgnoreCase))
        {
            return new Dictionary<string, IOpenApiExample>
            {
                ["education_cert_upsert"] = Example(
                    SwaggerExamples.UpdateEmployeeEducationCertUpsert(),
                    "Bulk edit — education + certifications with id (sync false)",
                    """
                    Same row shape as GET /employees/get (without employee_id on items).
                    Default sync=false: patch mode — only sent rows are upserted.
                    Round-trip id from GET to update; omit id to add.
                    """),
                ["partial_identity"] = Example(
                    SwaggerExamples.UpdateEmployeePartialIdentity(),
                    "Partial — identity only",
                    """
                    Send only the section/fields you change. identity fields are merged — you do not need full_name on every save unless changing it.
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
                    All sections optional — send only what you need. education/certifications use the same row shape as GET (with id on existing rows).
                    document_ids appends; delete_document_ids removes registry IDs.
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
                    "Top-level department. Send head as head_of_department_id (UUID) or nested head_of_department.employee_id from list round-trip."),
                ["child_department"] = Example(
                    SwaggerExamples.CreateDepartmentChild(),
                    "Child department",
                    "Nested under parent_department_id from an existing department."),
                ["with_headcount"] = Example(
                    SwaggerExamples.CreateDepartmentRoot(),
                    "Department with headcount cap",
                    "Set headcount_capacity so the org-chart headcount bar can show current vs max (e.g. 8/10)."),
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

        if (method.Equals("POST", StringComparison.OrdinalIgnoreCase)
            && path.Equals("api/v1/leave/requests/add", StringComparison.OrdinalIgnoreCase))
        {
            return new Dictionary<string, IOpenApiExample>
            {
                ["annual_leave"] = Example(
                    SwaggerExamples.CreateLeaveRequestBody(),
                    "Annual leave request",
                    "Admin creates on behalf of employee. Validates balance when a balance row exists."),
            };
        }

        if (method.Equals("POST", StringComparison.OrdinalIgnoreCase)
            && path.Equals("api/v1/leave/my/requests/add", StringComparison.OrdinalIgnoreCase))
        {
            return new Dictionary<string, IOpenApiExample>
            {
                ["my_request"] = Example(
                    SwaggerExamples.CreateLeaveRequestMyBody(),
                    "My Leave submission",
                    "Scoped to the logged-in platform user's employee profile. Requires leave_type_id only."),
            };
        }

        if (method.Equals("PUT", StringComparison.OrdinalIgnoreCase)
            && path.Equals("api/v1/leave/requests/update", StringComparison.OrdinalIgnoreCase))
        {
            return new Dictionary<string, IOpenApiExample>
            {
                ["status_and_notes"] = Example(
                    SwaggerExamples.UpdateLeaveRequestBody(),
                    "Update status / notes",
                    "Send only fields to change. leave_request_id is required on the query string."),
            };
        }

        if (method.Equals("POST", StringComparison.OrdinalIgnoreCase)
            && path.Equals("api/v1/leave/requests/reject", StringComparison.OrdinalIgnoreCase))
        {
            return new Dictionary<string, IOpenApiExample>
            {
                ["reject"] = Example(
                    SwaggerExamples.RejectLeaveRequestBody(),
                    "Reject with reason",
                    "Optional notes explain rejection. Approver is the authenticated user — no body required for reject without notes."),
            };
        }

        if (method.Equals("POST", StringComparison.OrdinalIgnoreCase)
            && path.Equals("api/v1/leave/balances/add", StringComparison.OrdinalIgnoreCase))
        {
            return new Dictionary<string, IOpenApiExample>
            {
                ["new_balance"] = Example(
                    SwaggerExamples.CreateLeaveBalanceBody(),
                    "Assign entitlement",
                    "One balance row per employee + leave_type_id. used_days defaults to 0."),
            };
        }

        if (method.Equals("PUT", StringComparison.OrdinalIgnoreCase)
            && path.Equals("api/v1/leave/balances/update", StringComparison.OrdinalIgnoreCase))
        {
            return new Dictionary<string, IOpenApiExample>
            {
                ["adjust_entitlement"] = Example(
                    SwaggerExamples.UpdateLeaveBalanceBody(),
                    "Adjust entitled / used days",
                    "leave_balance_id required on query string. remaining_days is recalculated."),
            };
        }

        if (method.Equals("POST", StringComparison.OrdinalIgnoreCase)
            && path.Equals("api/v1/leave/types/add", StringComparison.OrdinalIgnoreCase))
        {
            return new Dictionary<string, IOpenApiExample>
            {
                ["country_specific"] = Example(
                    SwaggerExamples.CreateLeaveTypeBody(),
                    "Country-specific type",
                    "ISO country_code scopes type to GH, KE, etc."),
                ["global"] = Example(
                    SwaggerExamples.CreateLeaveTypeGlobalBody(),
                    "Global leave type",
                    "Omit country_code for org-wide type available in all countries."),
            };
        }

        if (method.Equals("PUT", StringComparison.OrdinalIgnoreCase)
            && path.Equals("api/v1/leave/types/update", StringComparison.OrdinalIgnoreCase))
        {
            return new Dictionary<string, IOpenApiExample>
            {
                ["adjust_entitlement"] = Example(
                    SwaggerExamples.UpdateLeaveTypeBody(),
                    "Update default entitlement",
                    "leave_type_id required on query string."),
            };
        }

        if (method.Equals("POST", StringComparison.OrdinalIgnoreCase)
            && path.Equals("api/v1/leave/holidays/add", StringComparison.OrdinalIgnoreCase))
        {
            return new Dictionary<string, IOpenApiExample>
            {
                ["national"] = Example(
                    SwaggerExamples.CreatePublicHolidayBody(),
                    "National recurring holiday",
                    "is_recurring=true matches the holiday every year on month/day."),
                ["branch_specific"] = Example(
                    SwaggerExamples.CreatePublicHolidayBranchBody(),
                    "Branch-only holiday",
                    "Set branch_id to limit holiday to one location; omit for org-wide."),
            };
        }

        if (method.Equals("PUT", StringComparison.OrdinalIgnoreCase)
            && path.Equals("api/v1/leave/holidays/update", StringComparison.OrdinalIgnoreCase))
        {
            return new Dictionary<string, IOpenApiExample>
            {
                ["rename_or_reschedule"] = Example(
                    SwaggerExamples.UpdatePublicHolidayBody(),
                    "Rename or move date",
                    "holiday_id required on query string."),
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
