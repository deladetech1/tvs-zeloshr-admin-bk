using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using ZelosHR.Api.Entities.Leave;
using ZelosHR.Api.Entities.Shared;

namespace ZelosHR.Api.Configs;

/// <summary>Leave management OpenAPI examples, parameter hints, and response schemas.</summary>
public sealed class SwaggerLeaveOperationFilter : IOperationFilter
{
    private const string PipeExampleNote =
        "Pipe-separated values in examples list allowed shapes — send **one** value on real API calls.";

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var method = context.ApiDescription.HttpMethod ?? "";
        var path = context.ApiDescription.RelativePath ?? "";
        if (!path.StartsWith("api/v1/leave", StringComparison.OrdinalIgnoreCase))
            return;

        operation.Description = SwaggerOptionFormat.Append(operation.Description, PipeExampleNote);

        switch (path)
        {
            case "api/v1/leave/statistics" when method.Equals("GET", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.LeaveStatisticsResponse());
                operation.Summary ??= "Leave dashboard statistics";
                operation.Description = SwaggerOptionFormat.Append(operation.Description,
                    "Admin KPI cards: pending_requests · approved_this_month · on_leave_today · total_requests.");
                return;

            case "api/v1/leave/requests/list" when method.Equals("GET", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.LeaveListResponse());
                operation.Summary ??= "Leave Management list";
                operation.Description = SwaggerOptionFormat.Append(operation.Description,
                    """
                    Paginated admin list. Response `data`: summary · requests[] · balances[].
                    Each request includes `remaining_days` when a balance row exists for employee + leave_type.
                    Query `status`: Pending|Approved|Rejected|Cancelled or `all`. `search` min 3 chars on employee name.
                    """);
                AppendParameterDescription(operation, "status", $"Filter by status. Allowed: {SwaggerExampleHints.LeaveRequestStatus}, all.");
                AppendParameterDescription(operation, "leave_type", $"Leave type name (exact match). Examples: {SwaggerExampleHints.LeaveTypeName}.");
                AppendParameterDescription(operation, "employee_id", "Optional employee UUID filter.");
                return;

            case "api/v1/leave/requests/get" when method.Equals("GET", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.LeaveRequestGetResponse());
                SetJsonResponseExample(operation, 404, SwaggerExamples.EnvelopeFor(typeof(Respons<LeaveRequestListItemDto>), 404));
                operation.Summary ??= "Get leave request";
                AppendParameterDescription(operation, "leave_request_id", "Leave request UUID from list or create.");
                return;

            case "api/v1/leave/requests/add" when method.Equals("POST", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.LeaveRequestGetResponse());
                SetJsonResponseExample(operation, 400, SwaggerExamples.ValidationErrorEnvelope(new JsonObject
                {
                    ["days_requested"] = "Insufficient leave balance. Remaining: 3 day(s).",
                }));
                operation.Summary ??= "Create leave request (admin)";
                return;

            case "api/v1/leave/requests/update" when method.Equals("PUT", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.LeaveRequestGetResponse());
                operation.Summary ??= "Update leave request";
                AppendParameterDescription(operation, "leave_request_id", "Leave request UUID.");
                return;

            case "api/v1/leave/requests/approve" when method.Equals("POST", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.LeaveRequestApprovedResponse());
                SetJsonResponseExample(operation, 409, SwaggerExamples.EnvelopeFor(typeof(Respons<LeaveRequestListItemDto>), 409));
                operation.Summary ??= "Approve leave request";
                operation.Description = SwaggerOptionFormat.Append(operation.Description,
                    "Pending only. Decrements matching balance `remaining_days` when a balance row exists.");
                AppendParameterDescription(operation, "leave_request_id", "Pending leave request UUID.");
                return;

            case "api/v1/leave/requests/reject" when method.Equals("POST", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.LeaveRequestRejectedResponse());
                SetJsonResponseExample(operation, 409, SwaggerExamples.EnvelopeFor(typeof(Respons<LeaveRequestListItemDto>), 409));
                operation.Summary ??= "Reject leave request";
                AppendParameterDescription(operation, "leave_request_id", "Pending leave request UUID.");
                return;

            case "api/v1/leave/requests/delete" when method.Equals("DELETE", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.LeaveDeleteRequestResponse());
                SetJsonResponseExample(operation, 404, SwaggerExamples.EnvelopeFor(typeof(Respons<LeaveRequestListItemDto>), 404));
                operation.Summary ??= "Delete pending leave request";
                AppendParameterDescription(operation, "leave_request_id", "Only Pending requests can be deleted.");
                return;

            case "api/v1/leave/my/summary" when method.Equals("GET", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.LeaveMySummaryResponse());
                SetJsonResponseExample(operation, 404, SwaggerExamples.EnvelopeFor(typeof(Respons<LeaveRequestListItemDto>), 404));
                operation.Summary ??= "My Leave summary";
                operation.Description = SwaggerOptionFormat.Append(operation.Description,
                    "Employee self-service for logged-in platform user: total_remaining_days · pending_requests · approved_this_year · balances[].");
                return;

            case "api/v1/leave/my/requests/list" when method.Equals("GET", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.LeaveListResponse());
                operation.Summary ??= "My Leave requests";
                AppendParameterDescription(operation, "status", $"Optional status filter. Allowed: {SwaggerExampleHints.LeaveRequestStatus}, all.");
                return;

            case "api/v1/leave/my/balances/list" when method.Equals("GET", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.EnvelopeFor(typeof(Respons<LeaveBalanceListDto>), 200));
                operation.Summary ??= "My Leave balances";
                return;

            case "api/v1/leave/my/requests/add" when method.Equals("POST", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.LeaveRequestGetResponse());
                operation.Summary ??= "Submit My Leave request";
                operation.Description = SwaggerOptionFormat.Append(operation.Description,
                    "Creates a request for the employee linked to the logged-in platform user. `employee_id` in body is ignored.");
                return;

            case "api/v1/leave/balances/list" when method.Equals("GET", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.EnvelopeFor(typeof(Respons<LeaveBalanceListDto>), 200));
                operation.Summary ??= "List leave balances";
                AppendParameterDescription(operation, "employee_id", "Optional employee UUID filter.");
                AppendParameterDescription(operation, "leave_type", "Optional leave type name filter.");
                return;

            case "api/v1/leave/balances/get" when method.Equals("GET", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.EnvelopeFor(typeof(Respons<LeaveBalanceListItemDto>), 200));
                SetJsonResponseExample(operation, 404, SwaggerExamples.EnvelopeFor(typeof(Respons<LeaveRequestListItemDto>), 404));
                operation.Summary ??= "Get leave balance";
                AppendParameterDescription(operation, "leave_balance_id", "Balance row UUID.");
                return;

            case "api/v1/leave/balances/add" when method.Equals("POST", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.EnvelopeFor(typeof(Respons<LeaveBalanceListItemDto>), 200));
                operation.Summary ??= "Create leave balance (admin)";
                return;

            case "api/v1/leave/balances/update" when method.Equals("PUT", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.EnvelopeFor(typeof(Respons<LeaveBalanceListItemDto>), 200));
                operation.Summary ??= "Update leave balance (admin)";
                AppendParameterDescription(operation, "leave_balance_id", "Balance row UUID.");
                return;

            case "api/v1/leave/types/list" when method.Equals("GET", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.EnvelopeFor(typeof(Respons<LeaveTypeListDto>), 200));
                operation.Summary ??= "Leave types (Settings)";
                operation.Description = SwaggerOptionFormat.Append(operation.Description,
                    "Configurable leave types. Filter by ISO country_code (GH, KE, …) or omit for all. `active_only` defaults true.");
                AppendParameterDescription(operation, "country_code", "Optional ISO 3166-1 alpha-2 filter (GH, KE, NG, …).");
                AppendParameterDescription(operation, "active_only", $"Return only active types. Allowed: {SwaggerExampleHints.BooleanPipe}.");
                return;

            case "api/v1/leave/types/get" when method.Equals("GET", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.EnvelopeFor(typeof(Respons<LeaveTypeListItemDto>), 200));
                SetJsonResponseExample(operation, 404, SwaggerExamples.EnvelopeFor(typeof(Respons<LeaveRequestListItemDto>), 404));
                operation.Summary ??= "Get leave type";
                AppendParameterDescription(operation, "leave_type_id", "Leave type UUID.");
                return;

            case "api/v1/leave/types/add" when method.Equals("POST", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.EnvelopeFor(typeof(Respons<LeaveTypeListItemDto>), 200));
                operation.Summary ??= "Create leave type (admin)";
                return;

            case "api/v1/leave/types/update" when method.Equals("PUT", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.EnvelopeFor(typeof(Respons<LeaveTypeListItemDto>), 200));
                operation.Summary ??= "Update leave type (admin)";
                AppendParameterDescription(operation, "leave_type_id", "Leave type UUID.");
                return;

            case "api/v1/leave/types/delete" when method.Equals("DELETE", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.LeaveDeleteTypeResponse());
                SetJsonResponseExample(operation, 404, SwaggerExamples.EnvelopeFor(typeof(Respons<LeaveRequestListItemDto>), 404));
                operation.Summary ??= "Delete or deactivate leave type (admin)";
                AppendParameterDescription(operation, "leave_type_id", "Soft-deactivates when type is referenced by requests/balances.");
                return;

            case "api/v1/leave/holidays/list" when method.Equals("GET", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.LeaveHolidayListResponse());
                operation.Summary ??= "Public holidays";
                operation.Description = SwaggerOptionFormat.Append(operation.Description,
                    "Country-based holidays for leave planning. Filter by country_code, year, and optional branch_id (includes org-wide rows where branch_id is null).");
                AppendParameterDescription(operation, "country_code", "ISO country code (GH, KE, NG, …).");
                AppendParameterDescription(operation, "year", "Calendar year; includes recurring holidays from any year.");
                AppendParameterDescription(operation, "branch_id", "Optional branch UUID — returns org-wide + branch-specific holidays.");
                return;

            case "api/v1/leave/holidays/get" when method.Equals("GET", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.EnvelopeFor(typeof(Respons<PublicHolidayListItemDto>), 200));
                SetJsonResponseExample(operation, 404, SwaggerExamples.EnvelopeFor(typeof(Respons<LeaveRequestListItemDto>), 404));
                operation.Summary ??= "Get public holiday";
                AppendParameterDescription(operation, "holiday_id", "Holiday UUID.");
                return;

            case "api/v1/leave/holidays/add" when method.Equals("POST", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.EnvelopeFor(typeof(Respons<PublicHolidayListItemDto>), 200));
                operation.Summary ??= "Create public holiday (admin)";
                return;

            case "api/v1/leave/holidays/update" when method.Equals("PUT", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.EnvelopeFor(typeof(Respons<PublicHolidayListItemDto>), 200));
                operation.Summary ??= "Update public holiday (admin)";
                AppendParameterDescription(operation, "holiday_id", "Holiday UUID.");
                return;

            case "api/v1/leave/holidays/delete" when method.Equals("DELETE", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.LeaveDeleteHolidayResponse());
                SetJsonResponseExample(operation, 404, SwaggerExamples.EnvelopeFor(typeof(Respons<LeaveRequestListItemDto>), 404));
                operation.Summary ??= "Remove public holiday (admin)";
                AppendParameterDescription(operation, "holiday_id", "Soft-deactivates the holiday row.");
                return;
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
}
