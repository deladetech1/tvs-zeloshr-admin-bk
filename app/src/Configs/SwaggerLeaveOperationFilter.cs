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
                    "Admin KPI cards: pending_requests · pending_final_approvals · on_leave_today · leaving_this_week · low_balance_alert · total_requests.");
                return;

            case "api/v1/leave/dashboard" when method.Equals("GET", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.LeaveDashboardResponse());
                operation.Summary ??= "Leave Management dashboard";
                operation.Description = SwaggerOptionFormat.Append(operation.Description,
                    "Dashboard widgets: summary · on_leave_today[] · pending_approvals[] (final stage) · leaving_this_week[]. Rows include nested employee, leave_type, waiting_hours.");
                return;

            case "api/v1/leave/requests/list" when method.Equals("GET", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.LeaveListResponse());
                operation.Summary ??= "Leave Management / Approvals list";
                operation.Description = SwaggerOptionFormat.Append(operation.Description,
                    """
                    Paginated admin list. Response `data`: summary · items[].
                    Each row includes nested `employee`, `leave_type`, `prior_approvers`, `waiting_hours`, and `employee.profile_url` (DocumentReadDto) when pending.
                    Use `approval_stage=pending_final` for the final approver queue (LM+HoD cleared).
                    Filters: department_id · from_date/to_date (overlap) · leave_type_id · employee_id · search (name, code, job title).
                    Balances: GET /leave/balances/list.
                    """);
                AppendParameterDescription(operation, "status", $"Filter by status. Allowed: {SwaggerExampleHints.LeaveRequestStatus}, all.");
                AppendParameterDescription(operation, "approval_stage", $"Workflow stage filter. Allowed: {SwaggerExampleHints.LeaveApprovalStage}, all.");
                AppendParameterDescription(operation, "leave_type_id", "Optional leave type UUID filter.");
                AppendParameterDescription(operation, "employee_id", "Optional employee UUID filter.");
                AppendParameterDescription(operation, "department_id", "Optional department UUID filter.");
                AppendParameterDescription(operation, "from_date", "Include requests ending on or after this date.");
                AppendParameterDescription(operation, "to_date", "Include requests starting on or before this date.");
                return;

            case "api/v1/leave/requests/get" when method.Equals("GET", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.LeaveRequestGetResponse());
                SetJsonResponseExample(operation, 404, SwaggerExamples.EnvelopeFor(typeof(Respons<LeaveRequestDetailDto>), 404));
                operation.Summary ??= "Get leave request detail";
                operation.Description = SwaggerOptionFormat.Append(operation.Description,
                    """
                    Detail modal payload: nested `employee` · `leave_type` · working_days · public_holidays_in_range · balance_impact · approval_trail.
                    Use nested refs for display — do not render raw UUIDs in the UI.
                    """);
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
                SetJsonResponseExample(operation, 409, SwaggerExamples.EnvelopeFor(typeof(Respons<LeaveRequestDetailDto>), 409));
                operation.Summary ??= "Advance leave approval";
                operation.Description = SwaggerOptionFormat.Append(operation.Description,
                    """
                    Three-stage workflow: line_manager → head_of_department → final.
                    Each call advances one stage when the authenticated user's employee matches the expected approver.
                    Final stage sets status Approved and decrements balance. `approver_id` is the platform user id.
                    """);
                AppendParameterDescription(operation, "leave_request_id", "Pending leave request UUID.");
                return;

            case "api/v1/leave/requests/reject" when method.Equals("POST", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.LeaveRequestRejectedResponse());
                SetJsonResponseExample(operation, 409, SwaggerExamples.EnvelopeFor(typeof(Respons<LeaveRequestDetailDto>), 409));
                operation.Summary ??= "Reject leave request";
                AppendParameterDescription(operation, "leave_request_id", "Pending leave request UUID.");
                return;

            case "api/v1/leave/requests/delete" when method.Equals("DELETE", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.LeaveDeleteRequestResponse());
                SetJsonResponseExample(operation, 404, SwaggerExamples.EnvelopeFor(typeof(Respons<LeaveRequestDetailDto>), 404));
                operation.Summary ??= "Delete pending leave request";
                AppendParameterDescription(operation, "leave_request_id", "Only Pending requests can be deleted.");
                return;

            case "api/v1/leave/my/summary" when method.Equals("GET", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.LeaveMySummaryResponse());
                SetJsonResponseExample(operation, 404, SwaggerExamples.EnvelopeFor(typeof(Respons<LeaveMySummaryDto>), 404));
                operation.Summary ??= "My Leave summary";
                operation.Description = SwaggerOptionFormat.Append(operation.Description,
                    """
                    Employee self-service for logged-in platform user: total_remaining_days · pending_requests · approved_this_year · balances[].
                    Each balance includes nested `employee` (full_name) and `leave_type` (name) — use those for display, not raw UUIDs.
                    """);
                return;

            case "api/v1/leave/my/requests/list" when method.Equals("GET", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.LeaveMyRequestListResponse());
                operation.Summary ??= "My Leave requests";
                operation.Description = SwaggerOptionFormat.Append(operation.Description,
                    "Paginated requests for the logged-in employee. Rows include nested `employee` and `leave_type`. Use GET /leave/my/summary for balances and counts.");
                AppendParameterDescription(operation, "status", $"Optional status filter. Allowed: {SwaggerExampleHints.LeaveRequestStatus}, all.");
                return;

            case "api/v1/leave/my/balances/list" when method.Equals("GET", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.LeaveBalanceListResponse());
                SetJsonResponseExample(operation, 404, SwaggerExamples.EnvelopeFor(typeof(Respons<LeaveBalanceListDto>), 404));
                operation.Summary ??= "My Leave balances";
                operation.Description = SwaggerOptionFormat.Append(operation.Description,
                    "Entitlement rows for the logged-in employee. Each item includes nested `employee` and `leave_type` display refs.");
                return;

            case "api/v1/leave/my/requests/add" when method.Equals("POST", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.LeaveRequestGetResponse());
                operation.Summary ??= "Submit My Leave request";
                operation.Description = SwaggerOptionFormat.Append(operation.Description,
                    "Creates a request for the employee linked to the logged-in platform user. Body uses `leave_type_id` only — no `employee_id`.");
                return;

            case "api/v1/leave/balances/list" when method.Equals("GET", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.LeaveBalanceListResponse());
                operation.Summary ??= "List leave balances";
                operation.Description = SwaggerOptionFormat.Append(operation.Description,
                    "Admin entitlement list. Each row includes nested `employee` (full_name, job_title, profile_url) and `leave_type` (name).");
                AppendParameterDescription(operation, "employee_id", "Optional employee UUID filter.");
                AppendParameterDescription(operation, "leave_type_id", "Optional leave type UUID filter.");
                return;

            case "api/v1/leave/balances/get" when method.Equals("GET", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.LeaveBalanceGetResponse());
                SetJsonResponseExample(operation, 404, SwaggerExamples.EnvelopeFor(typeof(Respons<LeaveBalanceListItemDto>), 404));
                operation.Summary ??= "Get leave balance";
                operation.Description = SwaggerOptionFormat.Append(operation.Description,
                    "Single balance row with nested `employee` and `leave_type` display refs.");
                AppendParameterDescription(operation, "leave_balance_id", "Balance row UUID.");
                return;

            case "api/v1/leave/balances/add" when method.Equals("POST", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.LeaveBalanceGetResponse());
                operation.Summary ??= "Create leave balance (admin)";
                operation.Description = SwaggerOptionFormat.Append(operation.Description,
                    "Creates an entitlement row. Response includes nested `employee` and `leave_type` for display.");
                return;

            case "api/v1/leave/balances/update" when method.Equals("PUT", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.LeaveBalanceGetResponse());
                operation.Summary ??= "Update leave balance (admin)";
                operation.Description = SwaggerOptionFormat.Append(operation.Description,
                    "Partial update of entitled_days and/or used_days. Response includes nested display refs.");
                AppendParameterDescription(operation, "leave_balance_id", "Balance row UUID.");
                return;

            case "api/v1/leave/types/list" when method.Equals("GET", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.LeaveTypeListResponse());
                operation.Summary ??= "Leave types (Settings)";
                operation.Description = SwaggerOptionFormat.Append(operation.Description,
                    "Configurable leave types. Filter by ISO country_code (GH, KE, …) or omit for all. `active_only` defaults true.");
                AppendParameterDescription(operation, "country_code", "Optional ISO 3166-1 alpha-2 filter (GH, KE, NG, …).");
                AppendParameterDescription(operation, "active_only", $"Return only active types. Allowed: {SwaggerExampleHints.BooleanPipe}.");
                return;

            case "api/v1/leave/types/get" when method.Equals("GET", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.LeaveTypeGetResponse());
                SetJsonResponseExample(operation, 404, SwaggerExamples.EnvelopeFor(typeof(Respons<LeaveTypeListItemDto>), 404));
                operation.Summary ??= "Get leave type";
                AppendParameterDescription(operation, "leave_type_id", "Leave type UUID.");
                return;

            case "api/v1/leave/types/add" when method.Equals("POST", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.LeaveTypeGetResponse());
                operation.Summary ??= "Create leave type (admin)";
                return;

            case "api/v1/leave/types/update" when method.Equals("PUT", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.LeaveTypeGetResponse());
                operation.Summary ??= "Update leave type (admin)";
                AppendParameterDescription(operation, "leave_type_id", "Leave type UUID.");
                return;

            case "api/v1/leave/types/delete" when method.Equals("DELETE", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.LeaveDeleteTypeResponse());
                SetJsonResponseExample(operation, 404, SwaggerExamples.EnvelopeFor(typeof(Respons<LeaveTypeListItemDto>), 404));
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
                SetJsonResponseExample(operation, 200, SwaggerExamples.LeaveHolidayGetResponse());
                SetJsonResponseExample(operation, 404, SwaggerExamples.EnvelopeFor(typeof(Respons<PublicHolidayListItemDto>), 404));
                operation.Summary ??= "Get public holiday";
                AppendParameterDescription(operation, "holiday_id", "Holiday UUID.");
                return;

            case "api/v1/leave/holidays/add" when method.Equals("POST", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.LeaveHolidayGetResponse());
                operation.Summary ??= "Create public holiday (admin)";
                return;

            case "api/v1/leave/holidays/update" when method.Equals("PUT", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.LeaveHolidayGetResponse());
                operation.Summary ??= "Update public holiday (admin)";
                AppendParameterDescription(operation, "holiday_id", "Holiday UUID.");
                return;

            case "api/v1/leave/holidays/delete" when method.Equals("DELETE", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.LeaveDeleteHolidayResponse());
                SetJsonResponseExample(operation, 404, SwaggerExamples.EnvelopeFor(typeof(Respons<PublicHolidayListItemDto>), 404));
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
