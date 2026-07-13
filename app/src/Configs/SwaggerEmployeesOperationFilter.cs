using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Entities.Shared;

namespace ZelosHR.Api.Configs;

/// <summary>Path-specific employee OpenAPI examples (responses, query params, import body).</summary>
public sealed class SwaggerEmployeesOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var method = context.ApiDescription.HttpMethod ?? "";
        var path = context.ApiDescription.RelativePath ?? "";

        if (method.Equals("GET", StringComparison.OrdinalIgnoreCase) && path.Equals("api/v1/employees/get", StringComparison.OrdinalIgnoreCase))
        {
            SetJsonResponseExample(operation, 200, SwaggerExamples.EmployeeAggregateReadResponse());
            SetJsonResponseExample(operation, 404, SwaggerExamples.NotFoundEnvelopeForEmployee());
            AppendParameterDescription(operation, "employee_id",
                "Required. Employee UUID from POST /employees/add or GET /employees/list → items[].employee_id.");
            operation.Description = AppendDescription(operation.Description,
                """
                Response `documents[]` and `identity.profile_url`: MyStoreGuard DocumentReadDto (`doc_id`, `name`, `presigned_url`, `description`).
                On update send document id strings (or round-trip the read objects).

                **employment.employment_type (read):** nested object `{ id, name, description, type }` — flat `employment_type_id` is omitted.
                `id` matches `employment_type_id` from GET /employment-types/list used on write.
                """);
            return;
        }

        if (method.Equals("GET", StringComparison.OrdinalIgnoreCase) && path.Equals("api/v1/employees/statistics", StringComparison.OrdinalIgnoreCase))
        {
            SetJsonResponseExample(operation, 200, SwaggerExamples.EmployeeDirectoryStatisticsResponse());
            operation.Description = AppendDescription(operation.Description,
                "KPI cards for the employee directory: total_employees, active_employees, on_probation, on_contract.");
            return;
        }

        if (method.Equals("GET", StringComparison.OrdinalIgnoreCase) && path.Equals("api/v1/employees/list", StringComparison.OrdinalIgnoreCase))
        {
            SetJsonResponseExample(operation, 200, SwaggerExamples.EnvelopeFor(typeof(Respons<EmployeeListDto>), 200));
            operation.Description = AppendDescription(operation.Description,
                """
                Status filtering — two modes (do not combine):
                • employment_status — exact match on stored column (Draft, Pre-hire, Active, Probation, On Leave, …)
                • status — smart filter using simple commands: active, probation, on_leave, pre_hire, draft, suspended, terminated, resigned, inactive

                Examples:
                • GET /employees/list?status=active — all actively employed (includes probation and on leave)
                • GET /employees/list?status=probation — active employees on probation
                • GET /employees/list?employment_status=Active — exact match only (excludes Probation rows)

                Each item returns employment_status (stored value), plus engagement and work_states[] for UI badges.

                Filter by employment start with start_date / end_date (YYYY-MM-DD). Same filters apply to GET /employees/export.
                """);
            AppendParameterDescription(operation, "start_date",
                "Employment start on or after this date (uses start_date or employment_start_date on the employee record).");
            AppendParameterDescription(operation, "end_date",
                "Employment start on or before this date (uses start_date or employment_start_date on the employee record).");
            AppendParameterDescription(operation, "status",
                $"Smart workforce filter. Allowed: {SwaggerExampleHints.ListStatusFilter}. Ignored when employment_status is set.");
            AppendParameterDescription(operation, "employment_status",
                $"Exact match on employment_status column. Allowed: {SwaggerExampleHints.EmploymentStatus}.");
            return;
        }

        if (method.Equals("GET", StringComparison.OrdinalIgnoreCase) && path.Equals("api/v1/employees/import/search", StringComparison.OrdinalIgnoreCase))
        {
            SetJsonResponseExample(operation, 200, SwaggerExamples.ImportSearchResponse());
            AppendParameterDescription(operation, "query",
                "Search text matched against cp_users full name or email (min 1 character). Returns up to 20 users not already linked to an employee.");
            operation.Description = AppendDescription(operation.Description,
                """
                Trovesuite platform user picker for HR import. Reads `core_platform.cp_users` for the current tenant.
                For a full paginated user directory with filters, use `GET /api/v1/users/get-users` (Users tag).
                """);
            return;
        }

        if (method.Equals("POST", StringComparison.OrdinalIgnoreCase) && path.Equals("api/v1/employees/import", StringComparison.OrdinalIgnoreCase))
        {
            SetJsonRequestExample(operation, SwaggerExamples.ImportEmployeesRequestBody());
            SetJsonResponseExample(operation, 200, SwaggerExamples.ImportEmployeesAllSucceededResponse());
            SetJsonResponseExample(operation, 207, SwaggerExamples.ImportEmployeesPartialResponse());
            SetJsonResponseExample(operation, 422, SwaggerExamples.ImportEmployeesAllFailedResponse());
            operation.Description = AppendDescription(operation.Description,
                """
                Responses: **200** all rows succeeded; **207** partial; **422** none succeeded.
                Per-row outcomes in `data.items[]` (`success`, `error`).
                """);
            return;
        }

        if (method.Equals("GET", StringComparison.OrdinalIgnoreCase) && path.Equals("api/v1/employees/bulk/template", StringComparison.OrdinalIgnoreCase))
        {
            operation.Description = SwaggerOptionFormat.Append(operation.Description,
                $"Returns {EmployeeBulkImportCsv.FileName} with columns: {string.Join(", ", EmployeeBulkImportCsv.Headers)}.");
            return;
        }

        if (method.Equals("GET", StringComparison.OrdinalIgnoreCase) && path.Equals("api/v1/employees/export", StringComparison.OrdinalIgnoreCase))
        {
            operation.Summary ??= "Export employees CSV";
            operation.Description = SwaggerOptionFormat.Append(operation.Description,
                $"Returns CSV with columns: {string.Join(", ", EmployeeCsvExport.Headers)}. "
                + "Filter by employment start using start_date and end_date (YYYY-MM-DD). "
                + "Optional filters match GET /employees/list (search, status, employment_status, department_id, branch_id, employment_type, work_location, include_inactive). "
                + "Use status=active|probation|on_leave for smart filtering, or employment_status for exact column match.");
            AppendParameterDescription(operation, "status",
                $"Smart workforce filter. Allowed: {SwaggerExampleHints.ListStatusFilter}.");
            AppendParameterDescription(operation, "employment_status",
                $"Exact match on employment_status. Allowed: {SwaggerExampleHints.EmploymentStatus}.");
            AppendParameterDescription(operation, "start_date",
                "Employment start on or after this date (uses start_date or employment_start_date on the employee record).");
            AppendParameterDescription(operation, "end_date",
                "Employment start on or before this date (uses start_date or employment_start_date on the employee record).");
            return;
        }

        if (method.Equals("DELETE", StringComparison.OrdinalIgnoreCase) && path.Equals("api/v1/employees/delete", StringComparison.OrdinalIgnoreCase))
        {
            SetJsonResponseExample(operation, 200, SwaggerExamples.DeleteEmployeeSuccessResponse());
            SetJsonResponseExample(operation, 404, SwaggerExamples.NotFoundEnvelopeForEmployee());
            AppendParameterDescription(operation, "employee_id", "Employee UUID to soft-delete.");
            return;
        }

        if (method.Equals("PUT", StringComparison.OrdinalIgnoreCase) && path.Equals("api/v1/employees/update", StringComparison.OrdinalIgnoreCase))
        {
            SetJsonResponseExample(operation, 200, SwaggerExamples.EmployeeAggregateReadResponse());
            SetJsonResponseExample(operation, 404, SwaggerExamples.NotFoundEnvelopeForEmployee());
            AppendParameterDescription(operation, "employee_id",
                "Required. Employee UUID from POST /employees/add or GET /employees/get?employee_id=.");
            operation.Description = AppendDescription(operation.Description,
                """
                Partial aggregate update — send only changed sections.

                education[] / certifications[] — two modes:
                • sync false (default): PATCH — send rows to add/update; rows you omit are unchanged.
                • sync true: REPLACE — array is the full desired set; existing rows not listed are deleted.

                Row ids: include <c>id</c> from GET only to update an existing row. Omit <c>id</c> (or send a client UUID not yet saved) to add a new education/certification row while editing.

                delete_education_ids / delete_certification_ids remove rows by UUID without sending arrays.
                Include id from GET on education/certification items to update; omit id to add.

                **employment:** write `employment_type_id` (UUID from GET /employment-types/list). Response uses nested `employment.employment_type.id` only.
                """);
            return;
        }

        if (method.Equals("PUT", StringComparison.OrdinalIgnoreCase) && path.Equals("api/v1/employees/me/update", StringComparison.OrdinalIgnoreCase))
        {
            SetJsonResponseExample(operation, 200, SwaggerExamples.EmployeeSelfUpdateResultResponse());
            SetJsonResponseExample(operation, 400, SwaggerExamples.ValidationErrorEnvelope(
                new JsonObject { ["body"] = "Include at least one field to update." }));
            SetJsonResponseExample(operation, 404, SwaggerExamples.NotFoundEnvelopeForEmployee());
            operation.Description = AppendDescription(operation.Description,
                """
                Employee self-service partial update — same JSON shape as PUT /employees/update.
                Fields are split by GET /employees/field-policy:
                • free — applied immediately (listed in response.applied[])
                • approval — queued in zhr_employee_change_requests (listed in response.pending[])
                • omitted paths (employment, salary, work_email, document_ids, …) — rejected in response.rejected[]

                Requires the authenticated cp_users row to be linked to a zhr_employees.user_id.
                """);
            return;
        }

        if (method.Equals("GET", StringComparison.OrdinalIgnoreCase) && path.Equals("api/v1/employees/field-policy", StringComparison.OrdinalIgnoreCase))
        {
            SetJsonResponseExample(operation, 200, SwaggerExamples.FieldPolicyListResponse());
            operation.Description = AppendDescription(operation.Description,
                """
                Returns the employee self-service field policy matrix (path + access tier).

                | access | Behaviour on PUT /employees/me/update |
                |--------|----------------------------------------|
                | free | Applied immediately |
                | approval | Creates a pending change request for HR review |

                Paths not listed are admin-only and appear in response.rejected[].
                """);
            return;
        }

        if (method.Equals("GET", StringComparison.OrdinalIgnoreCase) && path.Equals("api/v1/employees/me/change-requests", StringComparison.OrdinalIgnoreCase))
        {
            SetJsonResponseExample(operation, 200, SwaggerExamples.ChangeRequestListResponse());
            SetJsonResponseExample(operation, 404, SwaggerExamples.NotFoundEnvelopeForEmployee());
            AppendParameterDescription(operation, "status",
                $"Optional filter. Allowed: {SwaggerExampleHints.ChangeRequestStatus}.");
            operation.Description = AppendDescription(operation.Description,
                """
                Lists change requests for the employee linked to the current JWT user (zhr_employees.user_id).
                Returns 404 when the logged-in user has no employee profile (typical for HR admin accounts).
                """);
            return;
        }

        if (path.StartsWith("api/v1/change-requests", StringComparison.OrdinalIgnoreCase))
        {
            if (method.Equals("GET", StringComparison.OrdinalIgnoreCase))
            {
                SetJsonResponseExample(operation, 200, SwaggerExamples.ChangeRequestListResponse());
                AppendParameterDescription(operation, "status",
                    $"Optional filter. Allowed: {SwaggerExampleHints.ChangeRequestStatus}.");
                AppendParameterDescription(operation, "employee_id",
                    "Optional employee UUID to scope the HR review queue.");
                AppendParameterDescription(operation, "page", "Page number (default 1).");
                AppendParameterDescription(operation, "size", "Page size (default 20, max 100).");
                operation.Description = AppendDescription(operation.Description,
                    """
                    HR review queue for employee self-service changes.

                    Each item includes field_path, old_value, new_value, status, requester/reviewer audit names,
                    and standard resource audit fields (created_at, updated_at, created_by*, updated_by*).

                    Filter examples:
                    • GET /change-requests?status=pending
                    • GET /change-requests?employee_id={uuid}&status=pending&page=1&size=20
                    """);
            }

            if (method.Equals("POST", StringComparison.OrdinalIgnoreCase) && path.Contains("/approve", StringComparison.Ordinal))
            {
                SetJsonResponseExample(operation, 200, SwaggerExamples.ChangeRequestApproveEmployeeResponse());
                SetJsonResponseExample(operation, 404, SwaggerExamples.NotFoundEnvelopeForEmployee());
                SetJsonResponseExample(operation, 409, SwaggerExamples.ChangeRequestConflictResponse());
                operation.Description = AppendDescription(operation.Description,
                    """
                    Approves a pending change request and replays new_value through PUT /employees/update for the target employee.
                    Returns the updated employee aggregate on success.
                    409 when the request is no longer pending (already approved, rejected, or superseded).
                    """);
            }

            if (method.Equals("POST", StringComparison.OrdinalIgnoreCase) && path.Contains("/reject", StringComparison.Ordinal))
            {
                SetJsonResponseExample(operation, 200, SwaggerExamples.ChangeRequestRejectedResponse());
                SetJsonResponseExample(operation, 404, SwaggerExamples.NotFoundEnvelopeForEmployee());
                SetJsonResponseExample(operation, 409, SwaggerExamples.ChangeRequestConflictResponse());
                operation.Description = AppendDescription(operation.Description,
                    "Rejects a pending change request. Optional review_note body is stored and returned on the change request row.");
            }

            return;
        }

        if (method.Equals("POST", StringComparison.OrdinalIgnoreCase) && path.Equals("api/v1/employees/bulk", StringComparison.OrdinalIgnoreCase))
        {
            SetJsonResponseExample(operation, 200, SwaggerExamples.BatchImportAllSucceededEnvelope());
            SetJsonResponseExample(operation, 207, SwaggerExamples.BatchImportPartialEnvelope());
            SetJsonResponseExample(operation, 422, SwaggerExamples.BatchImportAllFailedEnvelope());
            AppendParameterDescription(operation, "status",
                $"Import status for all CSV rows. Allowed: {SwaggerExampleHints.Status}.");
            operation.Description = AppendDescription(operation.Description,
                """
                Responses: **200** all rows succeeded (`success: true`); **207** partial (`success: false`, see `data.rows`);
                **422** none succeeded (`success: false`). Per-row errors stay in `data.rows[].error`.
                """);
        }
    }

    private static void SetJsonResponseExample(OpenApiOperation operation, int statusCode, JsonObject? example)
    {
        if (example is null)
            return;

        var key = statusCode.ToString();
        if (operation.Responses.TryGetValue(key, out var existing)
            && existing.Content is not null
            && existing.Content.TryGetValue("application/json", out var media))
        {
            SwaggerMediaExamples.SetSingleExample(media, example);
            return;
        }

        var newMedia = new OpenApiMediaType();
        SwaggerMediaExamples.SetSingleExample(newMedia, example);
        operation.Responses[key] = new OpenApiResponse
        {
            Description = existing?.Description ?? DescribeStatusCode(statusCode),
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["application/json"] = newMedia,
            },
        };
    }

    private static string DescribeStatusCode(int statusCode) => statusCode switch
    {
        207 => "Batch completed with some row failures (envelope success false; see data.rows).",
        422 => "Batch completed with no successful rows (envelope success false; see data.rows).",
        _ => "Response",
    };

    private static void SetJsonRequestExample(OpenApiOperation operation, JsonObject example)
    {
        if (operation.RequestBody?.Content is null
            || !operation.RequestBody.Content.TryGetValue("application/json", out var media))
            return;

        SwaggerMediaExamples.SetSingleExample(media, example);
    }

    private static void AppendParameterDescription(OpenApiOperation operation, string name, string addition)
    {
        if (operation.Parameters is null)
            return;

        foreach (var parameter in operation.Parameters)
        {
            if (!string.Equals(parameter.Name, name, StringComparison.OrdinalIgnoreCase))
                continue;

            parameter.Description = SwaggerOptionFormat.Append(parameter.Description, addition);
            break;
        }
    }

    private static string? AppendDescription(string? existing, string addition) =>
        SwaggerOptionFormat.Append(existing, addition);
}
