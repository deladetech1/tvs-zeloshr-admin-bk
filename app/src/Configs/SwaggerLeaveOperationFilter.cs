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
                    """
                    Flat employee lists for Leave Management landing page widgets (three example employees per widget).
                    KPI cards: summary.on_leave_today · summary.pending_approvals · summary.leaving_this_week · summary.low_balance_alert.
                    on_leave_today[]: employee_name · profile_url · leave_type · returns_on.
                    pending_approvals[]: employee_name · leave_type · leave_days · waiting OR days_since_last_approval (oldest first).
                    leaving_this_week[]: starts_on · employee_name · leave_type · leave_days.
                    Alias: GET /leave/summary.
                    """);
                return;

            case "api/v1/leave/approvals/list" when method.Equals("GET", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.LeaveApprovalListResponse());
                operation.Summary ??= "Leave Approvals table";
                operation.Description = SwaggerOptionFormat.Append(operation.Description,
                    $"""
                    Flat employee rows (response example shows three employees): employee_id · employee_name · title · profile_url · leave_type · leave_from · leave_to · leave_days · waiting · approved_by[] · audit fields.
                    `pending_count` is the final-stage queue badge count.
                    Tab: `tab={SwaggerExampleHints.LeaveApprovalListTab}`. Sort: `sort_by={SwaggerExampleHints.LeaveApprovalListSortBy}` · `sort_order={SwaggerExampleHints.LeaveApprovalListSortOrder}`.
                    Filters: search · department_id · leave_type_id · from_date/to_date · page · size.
                    Actions: GET /requests/get · POST /requests/approve · POST /requests/reject.
                    """);
                AppendParameterDescription(operation, "tab", $"Leave Approvals tab. Allowed: {SwaggerExampleHints.LeaveApprovalListTab}.");
                AppendParameterDescription(operation, "sort_by", $"Sort column. Allowed: {SwaggerExampleHints.LeaveApprovalListSortBy}.");
                AppendParameterDescription(operation, "sort_order", $"Sort direction. Allowed: {SwaggerExampleHints.LeaveApprovalListSortOrder}.");
                AppendParameterDescription(operation, "search", "Free text (min 2 chars): employee name, employee code, or job title.");
                AppendParameterDescription(operation, "leave_type_id", "Optional leave type UUID filter.");
                AppendParameterDescription(operation, "department_id", "Optional department UUID filter.");
                AppendParameterDescription(operation, "from_date", "Leave period: include requests ending on or after this date.");
                AppendParameterDescription(operation, "to_date", "Leave period: include requests starting on or before this date.");
                return;

            case "api/v1/leave/requests/list" when method.Equals("GET", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.LeaveListResponse());
                operation.Summary ??= "List leave requests (admin)";
                operation.Description = SwaggerOptionFormat.Append(operation.Description,
                    """
                    Paginated admin list (response example shows three employees) — each row is a leave request with nested employee · leave_type · audit fields.
                    Leave Approvals screen uses GET /approvals/list instead.
                    Filters: status · approval_stage · search · department_id · leave_type_id · from_date/to_date · page · size.
                    Actions: GET /requests/get · POST /requests/approve · POST /requests/reject.
                    """);
                AppendParameterDescription(operation, "status", $"Filter by status. Allowed: {SwaggerExampleHints.LeaveRequestStatus}, all.");
                AppendParameterDescription(operation, "approval_stage", $"Workflow stage filter. Allowed: {SwaggerExampleHints.LeaveApprovalStage}, all.");
                AppendParameterDescription(operation, "search", "Free text (min 2 chars): employee name, employee code, job title, or leave type name.");
                AppendParameterDescription(operation, "employee_code", "Partial match on employee code (e.g. ZEL-0042).");
                AppendParameterDescription(operation, "leave_request_id", "Exact leave request UUID — deep-link one row.");
                AppendParameterDescription(operation, "leave_type_id", "Optional leave type UUID filter.");
                AppendParameterDescription(operation, "employee_id", "Optional employee UUID filter.");
                AppendParameterDescription(operation, "department_id", "Optional department UUID filter.");
                AppendParameterDescription(operation, "branch_id", "Optional branch UUID filter.");
                AppendParameterDescription(operation, "from_date", "Leave period: include requests ending on or after this date.");
                AppendParameterDescription(operation, "to_date", "Leave period: include requests starting on or before this date.");
                AppendParameterDescription(operation, "submitted_from_date", "Submitted-at range: on or after start of day (UTC).");
                AppendParameterDescription(operation, "submitted_to_date", "Submitted-at range: on or before end of day (UTC).");
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
                    Final stage sets status Approved and decrements balance. Response `approver.approver_id` is the platform user id.
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

            case "api/v1/leave/summary" when method.Equals("GET", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.LeaveDashboardResponse());
                operation.Summary ??= "Leave Management summary";
                operation.Description = SwaggerOptionFormat.Append(operation.Description,
                    """
                    Leave Management landing page — same flat employee-list shape as GET /leave/dashboard.
                    KPI cards: summary.on_leave_today · summary.pending_approvals · summary.leaving_this_week · summary.low_balance_alert.
                    Widget rows are flat employee lists (not nested leave request objects).
                    """);
                return;

            case "api/v1/leave/my/summary" when method.Equals("GET", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.LeavePersonalSummaryResponse());
                SetJsonResponseExample(operation, 400, SwaggerExamples.EnvelopeFor(typeof(Respons<LeavePersonalSummaryDto>), 400));
                SetJsonResponseExample(operation, 404, SwaggerExamples.EnvelopeFor(typeof(Respons<LeavePersonalSummaryDto>), 404));
                operation.Summary ??= "My Leave summary";
                operation.Description = SwaggerOptionFormat.Append(operation.Description,
                    """
                    Personal leave for an employee: total_remaining_days · pending_requests · approved_this_year · balances[].
                    Requires `employee_id` query param (UUID from GET /employees/list).
                    """);
                AppendParameterDescription(operation, "employee_id", "Required. Employee UUID whose personal leave summary to load.");
                return;

            case "api/v1/leave/my/requests/list" when method.Equals("GET", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.LeaveMyRequestListResponse());
                SetJsonResponseExample(operation, 400, SwaggerExamples.EnvelopeFor(typeof(Respons<LeaveMyRequestListDto>), 400));
                SetJsonResponseExample(operation, 404, SwaggerExamples.EnvelopeFor(typeof(Respons<LeaveMyRequestListDto>), 404));
                operation.Summary ??= "My Leave requests";
                operation.Description = SwaggerOptionFormat.Append(operation.Description,
                    "Paginated requests for an employee. Requires `employee_id`. Rows include nested `employee` and `leave_type`. Admin dashboard: GET /leave/summary.");
                AppendParameterDescription(operation, "employee_id", "Required. Employee UUID whose requests to list.");
                AppendParameterDescription(operation, "status", $"Optional status filter. Allowed: {SwaggerExampleHints.LeaveRequestStatus}, all.");
                return;

            case "api/v1/leave/my/balances/list" when method.Equals("GET", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.LeaveBalanceListResponse());
                SetJsonResponseExample(operation, 400, SwaggerExamples.EnvelopeFor(typeof(Respons<LeaveBalanceListDto>), 400));
                SetJsonResponseExample(operation, 404, SwaggerExamples.EnvelopeFor(typeof(Respons<LeaveBalanceListDto>), 404));
                operation.Summary ??= "My Leave balances";
                operation.Description = SwaggerOptionFormat.Append(operation.Description,
                    "Entitlement rows for an employee. Requires `employee_id`. Each item includes nested `employee` and `leave_type` display refs.");
                AppendParameterDescription(operation, "employee_id", "Required. Employee UUID whose balances to list.");
                return;

            case "api/v1/leave/my/requests/add" when method.Equals("POST", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.LeaveRequestGetResponse());
                SetJsonResponseExample(operation, 400, SwaggerExamples.EnvelopeFor(typeof(Respons<LeaveRequestDetailDto>), 400));
                SetJsonResponseExample(operation, 404, SwaggerExamples.EnvelopeFor(typeof(Respons<LeaveRequestDetailDto>), 404));
                operation.Summary ??= "Submit My Leave request";
                operation.Description = SwaggerOptionFormat.Append(operation.Description,
                    "Creates a request for the employee identified by `employee_id` query param. Body uses `leave_type_id` only — no `employee_id` in JSON.");
                AppendParameterDescription(operation, "employee_id", "Required. Employee UUID submitting the request.");
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
                operation.Summary ??= "Leave types (Settings table)";
                operation.Description = SwaggerOptionFormat.Append(operation.Description,
                    """
                    Paginated leave types for Settings table. Each item includes policy fields and standard audit fields
                    (created_at, updated_at, created_by_id, updated_by_id, created_by, updated_by).
                    Map UI columns: name · default_entitled_days (entitlement) · accrual_method ({SwaggerExampleHints.LeaveAccrualMethod}) · carry_over_allowed ({SwaggerExampleHints.BooleanPipe}) ·
                    applies_to_employment_types ({SwaggerExampleHints.LeaveTypeEmploymentType}) · is_paid ({SwaggerExampleHints.BooleanPipe}) · audit fields.
                    """);
                AppendParameterDescription(operation, "search", "Optional name search (case-insensitive).");
                AppendParameterDescription(operation, "active_only", $"Return only active types. Allowed: {SwaggerExampleHints.BooleanPipe}. Default true.");
                AppendParameterDescription(operation, "page", "Page number (default 1).");
                AppendParameterDescription(operation, "size", "Page size (default 20).");
                return;

            case "api/v1/leave/types/get" when method.Equals("GET", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.LeaveTypeGetResponse());
                SetJsonResponseExample(operation, 404, SwaggerExamples.EnvelopeFor(typeof(Respons<LeaveTypeListItemDto>), 404));
                operation.Summary ??= "View leave type";
                operation.Description = SwaggerOptionFormat.Append(operation.Description,
                    "Single leave type for View action. Includes full policy fields and audit metadata.");
                AppendParameterDescription(operation, "leave_type_id", "Leave type UUID.");
                return;

            case "api/v1/leave/types/add" when method.Equals("POST", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.LeaveTypeGetResponse());
                operation.Summary ??= "Create leave type (admin)";
                operation.Description = SwaggerOptionFormat.Append(operation.Description,
                    $"""
                    Configure leave type policy — matches Add leave type modal:
                    name · default_entitled_days · is_paid ({SwaggerExampleHints.BooleanPipe}) · accrual_method ({SwaggerExampleHints.LeaveAccrualMethod}) · carry_over_allowed ({SwaggerExampleHints.BooleanPipe}) ·
                    applies_to_employment_types ({SwaggerExampleHints.LeaveTypeEmploymentType} — check All by sending all three) ·
                    min_notice_working_days · max_consecutive_days · requires_supporting_document ({SwaggerExampleHints.BooleanPipe}).
                    """);
                return;

            case "api/v1/leave/types/update" when method.Equals("PUT", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.LeaveTypeGetResponse());
                SetJsonResponseExample(operation, 400, SwaggerExamples.EnvelopeFor(typeof(Respons<LeaveTypeListItemDto>), 400));
                SetJsonResponseExample(operation, 404, SwaggerExamples.EnvelopeFor(typeof(Respons<LeaveTypeListItemDto>), 404));
                operation.Summary ??= "Edit leave type (admin)";
                operation.Description = SwaggerOptionFormat.Append(operation.Description,
                    "Edit action — same request body as POST /types/add (pipe-separated variants in schema). Pass leave_type_id query param.");
                AppendParameterDescription(operation, "leave_type_id", "Leave type UUID to update.");
                return;

            case "api/v1/leave/types/archive" when method.Equals("POST", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.LeaveArchiveTypeResponse());
                SetJsonResponseExample(operation, 404, SwaggerExamples.EnvelopeFor(typeof(Respons<LeaveTypeListItemDto>), 404));
                operation.Summary ??= "Archive leave type";
                operation.Description = SwaggerOptionFormat.Append(operation.Description,
                    "Archive action — sets is_active=false. Type remains for historical requests/balances.");
                AppendParameterDescription(operation, "leave_type_id", "Leave type UUID to archive.");
                return;

            case "api/v1/leave/types/delete" when method.Equals("DELETE", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.LeaveDeleteTypeResponse());
                SetJsonResponseExample(operation, 404, SwaggerExamples.EnvelopeFor(typeof(Respons<object>), 404));
                SetJsonResponseExample(operation, 409, SwaggerExamples.EnvelopeFor(typeof(Respons<object>), 409));
                operation.Summary ??= "Delete leave type";
                operation.Description = SwaggerOptionFormat.Append(operation.Description,
                    "Delete action — permanently removes when not referenced. Returns 409 when in use; archive instead.");
                AppendParameterDescription(operation, "leave_type_id", "Leave type UUID to delete.");
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
