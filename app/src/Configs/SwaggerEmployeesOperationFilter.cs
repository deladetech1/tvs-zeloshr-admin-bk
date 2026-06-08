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
                "Response `documents[]` and `identity.profile_url`: MyStoreGuard DocumentReadDto (`doc_id`, `name`, `presigned_url`, `description`). "
                + "On update send document id strings (or round-trip the read objects).");
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
                """);
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
                "Search text matched against cp_users full name or email. Returns users not already linked to an employee.");
            return;
        }

        if (method.Equals("POST", StringComparison.OrdinalIgnoreCase) && path.Equals("api/v1/employees/import", StringComparison.OrdinalIgnoreCase))
        {
            SetJsonRequestExample(operation, SwaggerExamples.ImportEmployeesRequestBody());
            SetJsonResponseExample(operation, 200, SwaggerExamples.ImportEmployeesResponse());
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
                """);
            return;
        }

        if (method.Equals("POST", StringComparison.OrdinalIgnoreCase) && path.Equals("api/v1/employees/bulk", StringComparison.OrdinalIgnoreCase))
        {
            SetJsonResponseExample(operation, 200, SwaggerExamples.EnvelopeFor(typeof(Respons<EmployeeBulkImportResult>), 200));
            AppendParameterDescription(operation, "status",
                $"Import status for all CSV rows. Allowed: {SwaggerExampleHints.Status}.");
        }
    }

    private static void SetJsonResponseExample(OpenApiOperation operation, int statusCode, JsonObject? example)
    {
        if (example is null)
            return;
        var key = statusCode.ToString();
        if (!operation.Responses.TryGetValue(key, out var response) || response.Content is null)
            return;

        if (!response.Content.TryGetValue("application/json", out var media))
            return;

        SwaggerMediaExamples.SetSingleExample(media, example);
    }

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
