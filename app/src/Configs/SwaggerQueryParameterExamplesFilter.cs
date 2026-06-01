using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using ZelosHR.Api.Entities.CustomFields;
using ZelosHR.Api.Entities.Employees;
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
            schema.Example = $"{SwaggerExamples.SampleBlobPathMulti1},{SwaggerExamples.SampleBlobPathMulti2}";
            parameter.Description = """
                **Required.** Comma-separated blob path(s) inside the Azure container (you choose the path).

                Pattern: `{tenant_id}/{org_id}/{bus_id}/employees/{filename}`

                Rules:
                - One path → applied to every uploaded file
                - N paths → must equal number of files (one path per file)

                Example (two files): `tenant_demo/org_demo/bus_demo/employees/contract.pdf,tenant_demo/org_demo/bus_demo/employees/id.jpg`

                Server picks storage account + container — not sent by the client.
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
                - Employee record → `document_ids` array from `GET /employees/id`

                Returns presigned download URLs valid for **24 hours**.

                Example: `doc_contract_a1b2c3,doc_national_id_d4e5f6`
                """;
            return;
        }

        if (name.Equals(PlatformQueryParams.EmployeeId, StringComparison.OrdinalIgnoreCase)
            || name.Equals("employeeId", StringComparison.OrdinalIgnoreCase))
        {
            schema.Example = SwaggerExamples.SampleEmployeeId.ToString();
            parameter.Description = "Employee UUID from POST /employees/add or GET /employees/list.";
            return;
        }

        if (name.Equals("entityType", StringComparison.OrdinalIgnoreCase))
        {
            schema.Example = CustomFieldEntityTypes.Employee;
            parameter.Description = SwaggerOptionFormat.Append(
                parameter.Description,
                "Entity type for custom field definitions. Use `employee` for HR profile fields.");
            return;
        }

        if (name.Equals("sectionName", StringComparison.OrdinalIgnoreCase))
        {
            schema.Example = EmployeeCustomFieldSections.Compensation;
            parameter.Description = SwaggerOptionFormat.Append(
                parameter.Description,
                "Filter definitions by employee form section.");
            return;
        }
    }
}
