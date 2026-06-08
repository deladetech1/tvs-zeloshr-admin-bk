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
                **Optional — leave empty to auto-generate** blob path(s) inside the **zeloshr** container.

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

        if (name.Equals("query", StringComparison.OrdinalIgnoreCase))
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

        if (name.Equals("search", StringComparison.OrdinalIgnoreCase)
            && context.ParameterInfo?.Member.DeclaringType?.FullName?.Contains(
                "OrgStructure",
                StringComparison.Ordinal) == true)
        {
            schema.Example = "eng";
            parameter.Description = "Optional name filter (minimum 3 characters, case-insensitive).";
            return;
        }

        if (context.ParameterInfo?.Member.DeclaringType?.FullName?.Contains(
                "AuditLogs",
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

        if (name.Equals(PlatformQueryParams.AuditLogId, StringComparison.OrdinalIgnoreCase)
            || name.Equals("auditLogId", StringComparison.OrdinalIgnoreCase))
        {
            schema.Example = SwaggerExamples.SampleAuditLogId.ToString();
            parameter.Description = "Audit log UUID from GET /api/v1/audit-logs/list.";
        }
    }
}
