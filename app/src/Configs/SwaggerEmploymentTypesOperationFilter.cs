using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using ZelosHR.Api.Entities.EmploymentTypes;
using ZelosHR.Api.Entities.Shared;

namespace ZelosHR.Api.Configs;

/// <summary>Company Settings — employment types OpenAPI examples and parameter hints.</summary>
public sealed class SwaggerEmploymentTypesOperationFilter : IOperationFilter
{
    private const string PipeExampleNote =
        "Pipe-separated values in examples list allowed shapes — send **one** value on real API calls.";

    private const string EmployeeTypeNote =
        """
        **Employee read vs write:**
        • **Write** (POST /employees/add, PUT /employees/update): `employment.employment_type_id` (UUID from this list).
        • **Read** (GET /employees/get): nested `employment.employment_type` object only — `id` matches the write FK; flat `employment_type_id` is omitted.
        """;

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var method = context.ApiDescription.HttpMethod ?? "";
        var path = context.ApiDescription.RelativePath ?? "";
        if (!path.StartsWith("api/v1/employment-types", StringComparison.OrdinalIgnoreCase))
            return;

        operation.Description = SwaggerOptionFormat.Append(operation.Description, PipeExampleNote);
        operation.Description = SwaggerOptionFormat.Append(operation.Description, EmployeeTypeNote);

        switch (path)
        {
            case "api/v1/employment-types/list" when method.Equals("GET", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.EmploymentTypeListResponse());
                operation.Summary ??= "Employment types (Company Settings table)";
                operation.Description = SwaggerOptionFormat.Append(operation.Description,
                    $"""
                    Ghana system defaults (Full-time, Part-time, Contractor, Casual, Intern) seed on first list per org.
                    Map UI columns: name · description · type ({SwaggerExampleHints.EmploymentTypeKind}) · employee_count · is_active · audit fields.
                    System defaults (`type=default`): cannot rename or delete. Custom types (`type=custom`): full CRUD.
                    """);
                AppendParameterDescription(operation, "search", "Optional filter on name or description (min 2 characters).");
                AppendParameterDescription(operation, "is_active", $"Filter by active flag. Allowed: {SwaggerExampleHints.BooleanPipe}. Omit for all.");
                AppendParameterDescription(operation, "sort_by", $"Allowed: {SwaggerExampleHints.EmploymentTypeSortBy}. Default name.");
                AppendParameterDescription(operation, "sort_order", "Allowed: asc | desc. Default asc.");
                AppendParameterDescription(operation, "page", "Page number (default 1).");
                AppendParameterDescription(operation, "size", "Page size (default 20).");
                return;

            case "api/v1/employment-types/get" when method.Equals("GET", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.EmploymentTypeGetResponse());
                SetJsonResponseExample(operation, 404, SwaggerExamples.EnvelopeFor(typeof(Respons<EmploymentTypeListItemDto>), 404));
                operation.Summary ??= "Get employment type";
                operation.Description = SwaggerOptionFormat.Append(operation.Description,
                    "Single row for view/edit drawer. Use employment_type_id on employee write; read returns nested employment_type.id.");
                AppendParameterDescription(operation, "employment_type_id", "Employment type UUID from list.");
                return;

            case "api/v1/employment-types/add" when method.Equals("POST", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.EmploymentTypeGetCustomResponse());
                SetJsonResponseExample(operation, 400, SwaggerExamples.EnvelopeFor(typeof(Respons<EmploymentTypeListItemDto>), 400));
                operation.Summary ??= "Add custom employment type";
                operation.Description = SwaggerOptionFormat.Append(operation.Description,
                    "Creates a custom type (`type=custom`). System defaults are seeded automatically — do not POST them.");
                return;

            case "api/v1/employment-types/update" when method.Equals("PUT", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.EmploymentTypeGetResponse());
                SetJsonResponseExample(operation, 400, SwaggerExamples.EmploymentTypeRenameBlockedResponse());
                SetJsonResponseExample(operation, 404, SwaggerExamples.EnvelopeFor(typeof(Respons<EmploymentTypeListItemDto>), 404));
                operation.Summary ??= "Update employment type";
                operation.Description = SwaggerOptionFormat.Append(operation.Description,
                    $"""
                    Partial body — send only fields to change.
                    • **Custom** (`type=custom`): name · description · is_active ({SwaggerExampleHints.BooleanPipe}) — all editable.
                    • **System default** (`type=default`): description and is_active only — **name change returns 400**.
                    See request examples: custom_full · system_description_only · deactivate_custom.
                    """);
                AppendParameterDescription(operation, "employment_type_id", "Employment type UUID to update.");
                return;

            case "api/v1/employment-types/delete" when method.Equals("DELETE", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.EmploymentTypeDeleteSuccessResponse());
                SetJsonResponseExample(operation, 404, SwaggerExamples.EnvelopeFor(typeof(Respons<object>), 404));
                SetJsonResponseExample(operation, 409, SwaggerExamples.EmploymentTypeDeleteBlockedResponse());
                operation.Summary ??= "Delete custom employment type";
                operation.Description = SwaggerOptionFormat.Append(operation.Description,
                    """
                    Custom types only. **400** when system default. **409** when employee_count > 0.
                    Prefer deactivating (`is_active: false` on PUT) when history must be kept.
                    """);
                AppendParameterDescription(operation, "employment_type_id", "Custom employment type UUID.");
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
