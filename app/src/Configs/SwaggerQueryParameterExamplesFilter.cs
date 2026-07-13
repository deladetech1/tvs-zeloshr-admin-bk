using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using ZelosHR.Api.Entities.CustomFields;
using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Entities.OrgStructure;
using ZelosHR.Api.Shared.Constants;

namespace ZelosHR.Api.Configs;

/// <summary>Adds realistic examples and descriptions to common query parameters.</summary>
public sealed class SwaggerQueryParameterExamplesFilter : IParameterFilter
{
    public void Apply(IOpenApiParameter parameter, ParameterFilterContext context)
    {
        if (parameter.Schema is not OpenApiSchema schema)
            return;

        var name = context.ParameterInfo?.Name ?? parameter.Name ?? "";

        if (name.Equals("blob_paths", StringComparison.OrdinalIgnoreCase))
        {
            schema.Example = null;
            parameter.Description = """
                **Optional — leave empty to auto-generate** blob path(s) inside the **employee-documents** container.

                Auto path pattern:
                `{tenant_id}/{org_id}/{bus_id}/employees/documents/{unique}-{filename}`

                Or supply paths explicitly (comma-separated only when uploading **multiple** files):
                - **One file** → entire `blob_paths` value is one path (commas in filenames are OK)
                - **Multiple files** → one comma-separated path per file (paths must not contain commas)
                - **One path, multiple files** → same path reused for every file
                Storage account is server config — clients send paths only, not container name.
                """;
            return;
        }

        if (name.Equals("blob_path", StringComparison.OrdinalIgnoreCase))
        {
            schema.Example = SwaggerExamples.SampleBlobPathSingle;
            parameter.Description = """
                Optional new blob path when replacing file content (`PUT /file/put`).
                Omit to overwrite the existing path from upload.
                """;
            return;
        }

        if (name.Equals(PlatformQueryParams.DocumentId, StringComparison.OrdinalIgnoreCase)
            || name.Equals("documentId", StringComparison.OrdinalIgnoreCase))
        {
            schema.Example = SwaggerExamples.SampleDocumentId1;
            parameter.Description = """
                Document registry ID from `POST /file/post/multiple` response (`data[].id`).
                This is **not** the blob path — use `document_id`, not `blob_path`, for update/delete.
                """;
            return;
        }

        if (name.Equals("descriptions", StringComparison.OrdinalIgnoreCase))
        {
            schema.Example = "Employment contract,National ID scan";
            parameter.Description = """
                Optional comma-separated labels stored on each registry row.
                Can be fewer entries than files — remaining files get no description.
                """;
            return;
        }

        if (name.Equals("description", StringComparison.OrdinalIgnoreCase))
        {
            schema.Example = "Employment contract (revised)";
            parameter.Description = "Optional updated label on `PUT /file/put`.";
            return;
        }

        if (name.Equals("document_ids", StringComparison.OrdinalIgnoreCase))
        {
            schema.Example = $"{SwaggerExamples.SampleDocumentId1},{SwaggerExamples.SampleDocumentId2}";
            parameter.Description = """
                **Required.** Comma-separated document registry IDs.

                Sources:
                - `POST /file/post/multiple` → `data[].id`
                - Employee GET → `documents[].doc_id` from `GET /employees/get?employee_id=`

                Returns presigned download URLs valid for **24 hours** (includes `file_name` on this endpoint).

                Example: `doc_contract_a1b2c3,doc_national_id_d4e5f6`
                """;
            return;
        }

        if (name.Equals("query", StringComparison.OrdinalIgnoreCase)
            && string.Equals(context.ParameterInfo?.Member.Name, "ImportSearch", StringComparison.Ordinal))
        {
            schema.Example = "ada";
            parameter.Description = SwaggerOptionFormat.Append(
                parameter.Description,
                "Search cp_users by name or email (excludes users already linked to an employee). Used on GET /employees/import/search.");
            return;
        }

        if (name.Equals(PlatformQueryParams.EmployeeId, StringComparison.OrdinalIgnoreCase)
            || name.Equals("employeeId", StringComparison.OrdinalIgnoreCase))
        {
            schema.Example = SwaggerExamples.SampleEmployeeId.ToString();
            parameter.Description = """
                Employee UUID from POST /employees/add or GET /employees/list.
                GET single employee: GET /api/v1/employees/get?employee_id={uuid}
                Update: PUT /api/v1/employees/update?employee_id={uuid}
                """;
            return;
        }

        if (name.Equals(PlatformQueryParams.CustomFieldId, StringComparison.OrdinalIgnoreCase)
            || name.Equals("customFieldId", StringComparison.OrdinalIgnoreCase))
        {
            schema.Example = SwaggerExamples.SampleCustomFieldId.ToString();
            parameter.Description = """
                Custom field definition UUID from POST /custom-fields/add or GET /custom-fields/list.
                Used on GET /custom-fields/get, PUT /custom-fields/update, and DELETE /custom-fields/delete.
                """;
            return;
        }

        if (name.Equals(PlatformQueryParams.CurrencyId, StringComparison.OrdinalIgnoreCase)
            || name.Equals("currencyId", StringComparison.OrdinalIgnoreCase))
        {
            schema.Example = SwaggerExamples.SampleCurrencyId;
            parameter.Description = """
                Currency ID from GET /api/v1/currencies/list (e.g. cur_ghs_default).
                Used as compensation.currency_id on employee create/update — not a currency code string.
                """;
            return;
        }

        if (name.Equals("entityType", StringComparison.OrdinalIgnoreCase)
            || name.Equals("entity_type", StringComparison.OrdinalIgnoreCase))
        {
            schema.Example = SwaggerExampleHints.EntityType;
            var isSectionsEndpoint = string.Equals(
                context.ParameterInfo?.Member.Name,
                "Sections",
                StringComparison.OrdinalIgnoreCase);
            parameter.Description = isSectionsEndpoint
                ? SwaggerOptionFormat.Append(
                    parameter.Description,
                    "Required. Entity type from GET /custom-fields/entity-types. Returns valid section_name options for that type.")
                : SwaggerOptionFormat.Append(
                    parameter.Description,
                    "Entity type for custom field definitions. Use `employee` for HR profile fields.");
            return;
        }

        if (name.Equals("sectionName", StringComparison.OrdinalIgnoreCase))
        {
            schema.Example = SwaggerExampleHints.SectionName;
            parameter.Description = SwaggerOptionFormat.Append(
                parameter.Description,
                $"Filter definitions by employee form section. Allowed: {SwaggerExampleHints.SectionName}.");
            return;
        }

        if (name.Equals("sortBy", StringComparison.OrdinalIgnoreCase)
            || name.Equals("sort_by", StringComparison.OrdinalIgnoreCase))
        {
            schema.Example = SwaggerExampleHints.OrgDepartmentSortBy;
            parameter.Description = SwaggerOptionFormat.Append(
                parameter.Description,
                $"Allowed: {SwaggerExampleHints.OrgDepartmentSortBy}.");
            return;
        }

        if (name.Equals("sortOrder", StringComparison.OrdinalIgnoreCase)
            || name.Equals("sort_order", StringComparison.OrdinalIgnoreCase))
        {
            schema.Example = SwaggerExampleHints.OrgSortOrder;
            parameter.Description = SwaggerOptionFormat.Append(
                parameter.Description,
                $"Allowed: {SwaggerExampleHints.OrgSortOrder}.");
            return;
        }

        if (name.Equals("includeArchived", StringComparison.OrdinalIgnoreCase)
            || name.Equals("include_archived", StringComparison.OrdinalIgnoreCase))
        {
            schema.Example = SwaggerExampleHints.OrgIncludeArchived;
            parameter.Description = SwaggerOptionFormat.Append(
                parameter.Description,
                $"Allowed: {SwaggerExampleHints.OrgIncludeArchived}.");
            return;
        }

        if (name.Equals(PlatformQueryParams.DepartmentId, StringComparison.OrdinalIgnoreCase)
            || name.Equals("departmentId", StringComparison.OrdinalIgnoreCase))
        {
            schema.Example = SwaggerExamples.SampleDepartmentId.ToString();
            parameter.Description = """
                Department UUID from POST /org-structure/departments/add or GET /org-structure/departments/list.
                """;
            return;
        }

        if (name.Equals(PlatformQueryParams.BranchId, StringComparison.OrdinalIgnoreCase)
            || name.Equals("branchId", StringComparison.OrdinalIgnoreCase))
        {
            schema.Example = SwaggerExamples.SampleBranchId.ToString();
            parameter.Description = """
                Branch UUID from POST /org-structure/branches/add or GET /org-structure/branches/list.
                """;
            return;
        }

        if (name.Equals(PlatformQueryParams.EmploymentTypeId, StringComparison.OrdinalIgnoreCase))
        {
            schema.Example = SwaggerExamples.SampleEmploymentTypeId.ToString();
            parameter.Description = SwaggerOptionFormat.Append(
                parameter.Description,
                "Employment type UUID from GET /employment-types/list or GET /employment-types/get.");
            return;
        }

        if (name.Equals("search", StringComparison.OrdinalIgnoreCase)
            && context.ParameterInfo?.Member.DeclaringType?.FullName?.Contains(
                "EmploymentTypeListQuery",
                StringComparison.Ordinal) == true)
        {
            schema.Example = "full";
            parameter.Description = "Optional filter on name or description (minimum 2 characters).";
            return;
        }

        if (name.Equals("sort_by", StringComparison.OrdinalIgnoreCase)
            && context.ParameterInfo?.Member.DeclaringType?.FullName?.Contains(
                "EmploymentTypeListQuery",
                StringComparison.Ordinal) == true)
        {
            schema.Example = "name";
            parameter.Description = SwaggerOptionFormat.Append(
                parameter.Description,
                $"Allowed: {SwaggerExampleHints.EmploymentTypeSortBy}.");
            return;
        }

        if (name.Equals("is_active", StringComparison.OrdinalIgnoreCase)
            && context.ParameterInfo?.Member.DeclaringType?.FullName?.Contains(
                "EmploymentTypeListQuery",
                StringComparison.Ordinal) == true)
        {
            schema.Example = SwaggerExampleHints.BooleanPipe;
            parameter.Description = SwaggerOptionFormat.Append(
                parameter.Description,
                $"Filter active/inactive rows. Allowed: {SwaggerExampleHints.BooleanPipe}. Omit for all.");
            return;
        }

        if (name.Equals("search", StringComparison.OrdinalIgnoreCase)
            && context.ParameterInfo?.Member.DeclaringType?.FullName?.Contains(
                "OrgStructure",
                StringComparison.Ordinal) == true)
        {
            schema.Example = "eng";
            parameter.Description = "Optional name filter (minimum 3 characters, case-insensitive).";
            return;
        }

        if (name.Equals("start_date", StringComparison.OrdinalIgnoreCase)
            || name.Equals("startDate", StringComparison.OrdinalIgnoreCase))
        {
            schema.Example = "2025-01-01";
            parameter.Description = SwaggerOptionFormat.Append(
                parameter.Description,
                "Employment start on or after this date (YYYY-MM-DD). Uses start_date or employment_start_date on the employee record.");
            return;
        }

        if (name.Equals("end_date", StringComparison.OrdinalIgnoreCase)
            || name.Equals("endDate", StringComparison.OrdinalIgnoreCase))
        {
            schema.Example = "2025-12-31";
            parameter.Description = SwaggerOptionFormat.Append(
                parameter.Description,
                "Employment start on or before this date (YYYY-MM-DD). Must be on or after start_date when both are set.");
            return;
        }

        if (name.Equals("status", StringComparison.OrdinalIgnoreCase)
            && context.ParameterInfo?.Member.DeclaringType?.FullName?.Contains(
                "EmployeeListQuery", StringComparison.Ordinal) == true)
        {
            schema.Example = EmployeeEngagementValues.Active;
            parameter.Description = SwaggerOptionFormat.Append(
                parameter.Description,
                $"Smart workforce filter. Allowed: {SwaggerExampleHints.ListStatusFilter}. Ignored when employment_status is set.");
            return;
        }

        if (name.Equals("status", StringComparison.OrdinalIgnoreCase)
            && context.ParameterInfo?.Member.DeclaringType?.FullName?.Contains(
                "EmployeeExportQuery", StringComparison.Ordinal) == true)
        {
            schema.Example = EmployeeEngagementValues.Active;
            parameter.Description = SwaggerOptionFormat.Append(
                parameter.Description,
                $"Smart workforce filter. Allowed: {SwaggerExampleHints.ListStatusFilter}.");
            return;
        }

        if (name.Equals("employment_status", StringComparison.OrdinalIgnoreCase)
            || name.Equals("employmentStatus", StringComparison.OrdinalIgnoreCase))
        {
            schema.Example = EmploymentStatusValues.Active;
            parameter.Description = SwaggerOptionFormat.Append(
                parameter.Description,
                $"Exact match on stored employment_status. Allowed: {SwaggerExampleHints.EmploymentStatus}. Use status= for smart filtering.");
            return;
        }

        if (context.ParameterInfo?.Member.DeclaringType?.FullName?.Contains(
                "AuditLogPurgeQuery",
                StringComparison.Ordinal) == true)
        {
            if (name.Equals("retention_window", StringComparison.OrdinalIgnoreCase))
            {
                schema.Example = 90;
                parameter.Description = SwaggerOptionFormat.Append(
                    parameter.Description,
                    "Days before cutoff. Allowed: 90, 180, 365. Default 90.");
            }

            return;
        }

        if (IsLeaveParameter(context))
        {
            if (name.Equals(PlatformQueryParams.LeaveRequestId, StringComparison.OrdinalIgnoreCase)
                || name.Equals("leaveRequestId", StringComparison.OrdinalIgnoreCase))
            {
                schema.Example = SwaggerExamples.SampleLeaveRequestId.ToString();
                parameter.Description = "Leave request UUID from list, create, or dashboard.";
                return;
            }

            if (name.Equals(PlatformQueryParams.LeaveBalanceId, StringComparison.OrdinalIgnoreCase)
                || name.Equals("leaveBalanceId", StringComparison.OrdinalIgnoreCase))
            {
                schema.Example = SwaggerExamples.SampleLeaveBalanceId.ToString();
                parameter.Description = "Leave balance UUID from GET /leave/balances/list.";
                return;
            }

            if (name.Equals(PlatformQueryParams.LeaveTypeId, StringComparison.OrdinalIgnoreCase)
                || name.Equals("leaveTypeId", StringComparison.OrdinalIgnoreCase))
            {
                schema.Example = SwaggerExamples.SampleLeaveTypeId.ToString();
                parameter.Description = "Leave type UUID from GET /leave/types/list.";
                return;
            }

            if (name.Equals(PlatformQueryParams.HolidayId, StringComparison.OrdinalIgnoreCase)
                || name.Equals("holidayId", StringComparison.OrdinalIgnoreCase))
            {
                schema.Example = SwaggerExamples.SampleHolidayId.ToString();
                parameter.Description = "Public holiday UUID from GET /leave/holidays/list.";
                return;
            }

            if (name.Equals("approval_stage", StringComparison.OrdinalIgnoreCase))
            {
                schema.Example = "pending_final";
                parameter.Description = SwaggerOptionFormat.Append(
                    parameter.Description,
                    $"Workflow stage filter. Allowed: {SwaggerExampleHints.LeaveApprovalStage}, all.");
                return;
            }

            if (name.Equals("from_date", StringComparison.OrdinalIgnoreCase))
            {
                schema.Example = "2026-06-01";
                parameter.Description = SwaggerOptionFormat.Append(
                    parameter.Description,
                    "Leave period: include requests ending on or after this date (YYYY-MM-DD).");
                return;
            }

            if (name.Equals("to_date", StringComparison.OrdinalIgnoreCase))
            {
                schema.Example = "2026-12-31";
                parameter.Description = SwaggerOptionFormat.Append(
                    parameter.Description,
                    "Leave period: include requests starting on or before this date (YYYY-MM-DD).");
                return;
            }

            if (name.Equals("submitted_from_date", StringComparison.OrdinalIgnoreCase))
            {
                schema.Example = "2026-06-01";
                parameter.Description = SwaggerOptionFormat.Append(
                    parameter.Description,
                    "Submitted-at range: on or after start of day UTC (YYYY-MM-DD).");
                return;
            }

            if (name.Equals("submitted_to_date", StringComparison.OrdinalIgnoreCase))
            {
                schema.Example = "2026-06-30";
                parameter.Description = SwaggerOptionFormat.Append(
                    parameter.Description,
                    "Submitted-at range: on or before end of day UTC (YYYY-MM-DD).");
                return;
            }

            if (name.Equals("employee_code", StringComparison.OrdinalIgnoreCase))
            {
                schema.Example = "ZEL-0042";
                parameter.Description = "Partial match on employee code.";
                return;
            }

            if (name.Equals("status", StringComparison.OrdinalIgnoreCase))
            {
                schema.Example = "Pending";
                parameter.Description = SwaggerOptionFormat.Append(
                    parameter.Description,
                    $"Filter by status. Allowed: {SwaggerExampleHints.LeaveRequestStatus}, all.");
                return;
            }

            if (name.Equals("search", StringComparison.OrdinalIgnoreCase))
            {
                schema.Example = "kwame";
                parameter.Description = "Free text (min 2 chars): employee name, employee code, job title, or leave type name.";
                return;
            }

            if (name.Equals(PlatformQueryParams.Country, StringComparison.OrdinalIgnoreCase)
                || name.Equals("country", StringComparison.OrdinalIgnoreCase))
            {
                schema.Example = SwaggerExamples.SampleCountryName;
                parameter.Description =
                    "Country name from GET /countries/list (e.g. Ghana). Matches frontend PublicHolidayParams.country.";
                return;
            }

            if (name.Equals(PlatformQueryParams.CountryId, StringComparison.OrdinalIgnoreCase)
                || name.Equals("country_id", StringComparison.OrdinalIgnoreCase))
            {
                schema.Example = SwaggerExamples.SampleCountryId;
                parameter.Description = "Country id from GET /countries/get (countries module only).";
                return;
            }

            if (name.Equals("country_code", StringComparison.OrdinalIgnoreCase))
            {
                schema.Example = "GH";
                parameter.Description = "ISO 3166-1 alpha-2 country id (e.g. GH).";
                return;
            }

            if (name.Equals("year", StringComparison.OrdinalIgnoreCase))
            {
                schema.Example = 2026;
                parameter.Description = "Calendar year (e.g. 2026). Filters holidays for that year and sets occurrence_date on recurring rows. Omit for no filter.";
                return;
            }

            if (name.Equals("active_only", StringComparison.OrdinalIgnoreCase))
            {
                schema.Example = SwaggerExampleHints.BooleanPipe;
                parameter.Description = SwaggerOptionFormat.Append(
                    parameter.Description,
                    $"Return only active leave types. Allowed: {SwaggerExampleHints.BooleanPipe}.");
                return;
            }

            if (name.Equals("status", StringComparison.OrdinalIgnoreCase)
                && context.ParameterInfo?.Member.DeclaringType == typeof(ChangeRequestListQuery))
            {
                schema.Example = ChangeRequestStatuses.Pending;
                parameter.Description = SwaggerOptionFormat.Append(
                    parameter.Description,
                    $"Filter by workflow status. Allowed: {SwaggerExampleHints.ChangeRequestStatus}. Omit for all.");
                return;
            }

            if (name.Equals("status", StringComparison.OrdinalIgnoreCase)
                && context.ApiDescription.RelativePath?.Contains("me/change-requests", StringComparison.OrdinalIgnoreCase) == true)
            {
                schema.Example = ChangeRequestStatuses.Pending;
                parameter.Description = SwaggerOptionFormat.Append(
                    parameter.Description,
                    $"Filter by workflow status. Allowed: {SwaggerExampleHints.ChangeRequestStatus}. Omit for all.");
                return;
            }

            if (name.Equals("employee_id", StringComparison.OrdinalIgnoreCase)
                && context.ParameterInfo?.Member.DeclaringType == typeof(ChangeRequestListQuery))
            {
                schema.Example = SwaggerExamples.SampleEmployeeId.ToString();
                parameter.Description = SwaggerOptionFormat.Append(
                    parameter.Description,
                    "Optional employee UUID to scope the HR review queue.");
                return;
            }

            if (name.Equals("changeRequestId", StringComparison.OrdinalIgnoreCase)
                && context.ApiDescription.RelativePath?.Contains("change-requests", StringComparison.OrdinalIgnoreCase) == true)
            {
                schema.Example = SwaggerExamples.SampleChangeRequestId.ToString();
                parameter.Description = SwaggerOptionFormat.Append(
                    parameter.Description,
                    "Change request UUID from GET /change-requests or GET /employees/me/change-requests.");
                return;
            }

            if (name.Equals("tab", StringComparison.OrdinalIgnoreCase))
            {
                schema.Example = SwaggerExampleHints.LeaveApprovalListTab;
                parameter.Description = SwaggerOptionFormat.Append(
                    parameter.Description,
                    $"Active tab. Allowed: {SwaggerExampleHints.LeaveApprovalListTab}.");
                return;
            }

            if (name.Equals("sort_by", StringComparison.OrdinalIgnoreCase))
            {
                schema.Example = "name";
                parameter.Description = SwaggerOptionFormat.Append(
                    parameter.Description,
                    $"Sort column. Allowed: {SwaggerExampleHints.LeaveApprovalListSortBy}.");
                return;
            }

            if (name.Equals("sort_order", StringComparison.OrdinalIgnoreCase))
            {
                schema.Example = SwaggerExampleHints.LeaveApprovalListSortOrder;
                parameter.Description = SwaggerOptionFormat.Append(
                    parameter.Description,
                    $"Sort direction. Allowed: {SwaggerExampleHints.LeaveApprovalListSortOrder}.");
                return;
            }

            return;
        }

        if (context.ParameterInfo?.Member.DeclaringType?.FullName?.Contains(
                ".Entities.Users.",
                StringComparison.Ordinal) == true)
        {
            if (name.Equals("delete_status", StringComparison.OrdinalIgnoreCase))
            {
                schema.Example = "NOT_DELETED";
                return;
            }

            if (name.Equals("gender", StringComparison.OrdinalIgnoreCase))
            {
                schema.Example = "MALE";
                return;
            }

            if (name.Equals("email", StringComparison.OrdinalIgnoreCase))
            {
                schema.Example = "admin@demo.trovesuite.com";
                return;
            }

            if (name.Equals("fullname", StringComparison.OrdinalIgnoreCase))
            {
                schema.Example = "Demo Admin";
                return;
            }

            return;
        }

        if (context.ParameterInfo?.Member.DeclaringType?.FullName?.Contains(
                ".AuditLogs.",
                StringComparison.Ordinal) != true
            && context.ParameterInfo?.Member.DeclaringType?.FullName?.Contains(
                "AuditLog",
                StringComparison.Ordinal) != true)
            return;

        if (name.Equals("severity", StringComparison.OrdinalIgnoreCase))
        {
            schema.Example = SwaggerExampleHints.AuditSeverity;
            parameter.Description = SwaggerOptionFormat.Append(
                parameter.Description,
                $"Allowed: {SwaggerExampleHints.AuditSeverity} or `{SwaggerExampleHints.AuditFilterAll}`.");
            return;
        }

        if (name.Equals("action", StringComparison.OrdinalIgnoreCase))
        {
            schema.Example = "Personal information updated";
            parameter.Description = SwaggerOptionFormat.Append(
                parameter.Description,
                $"Action title substring, or `{SwaggerExampleHints.AuditFilterAll}`.");
            return;
        }

        if (name.Equals("actor", StringComparison.OrdinalIgnoreCase))
        {
            schema.Example = "u1000001-0000-0000-0000-000000000001";
            parameter.Description = SwaggerOptionFormat.Append(
                parameter.Description,
                "Platform user id or actor display name substring.");
            return;
        }

        if (name.Equals("start_date", StringComparison.OrdinalIgnoreCase))
        {
            schema.Example = "2026-01-01";
            parameter.Description = SwaggerOptionFormat.Append(
                parameter.Description,
                "Activity occurred on or after this date (YYYY-MM-DD, UTC day boundary).");
            return;
        }

        if (name.Equals("end_date", StringComparison.OrdinalIgnoreCase))
        {
            schema.Example = "2026-06-30";
            parameter.Description = SwaggerOptionFormat.Append(
                parameter.Description,
                "Activity occurred on or before this date (YYYY-MM-DD, UTC day boundary).");
            return;
        }

        if (name.Equals(PlatformQueryParams.AuditLogId, StringComparison.OrdinalIgnoreCase)
            || name.Equals("auditLogId", StringComparison.OrdinalIgnoreCase))
        {
            schema.Example = SwaggerExamples.SampleAuditLogId.ToString();
            parameter.Description = "Audit log UUID from GET /api/v1/audit-logs/list.";
        }
    }

    private static bool IsLeaveParameter(ParameterFilterContext context)
    {
        var declaringType = context.ParameterInfo?.Member.DeclaringType?.FullName;
        return declaringType?.Contains(".Entities.Leave.", StringComparison.Ordinal) == true;
    }
}
