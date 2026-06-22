using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using ZelosHR.Api.Entities.IdCardTypes;
using ZelosHR.Api.Entities.Shared;

namespace ZelosHR.Api.Configs;

/// <summary>Company Settings — ID card types OpenAPI examples and parameter hints.</summary>
public sealed class SwaggerIdCardTypesOperationFilter : IOperationFilter
{
    private const string PipeExampleNote =
        "Pipe-separated values in examples list allowed shapes — send **one** value on real API calls.";

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var method = context.ApiDescription.HttpMethod ?? "";
        var path = context.ApiDescription.RelativePath ?? "";
        if (!path.StartsWith("api/v1/id-card-types", StringComparison.OrdinalIgnoreCase))
            return;

        operation.Description = SwaggerOptionFormat.Append(operation.Description, PipeExampleNote);

        switch (path)
        {
            case "api/v1/id-card-types/list" when method.Equals("GET", StringComparison.OrdinalIgnoreCase):
                operation.Summary ??= "ID card types (Company Settings table)";
                operation.Description = SwaggerOptionFormat.Append(operation.Description,
                    """
                    Ghana system defaults (National ID, Voter's ID, Driver's License, National Health Insurance) seed on first list per org.
                    Map UI columns: name · description · type (default|custom) · is_active · audit fields.
                    System defaults (`type=default`): cannot rename or delete. Custom types (`type=custom`): full CRUD.
                    """);
                AppendParameterDescription(operation, "search", "Optional filter on name or description (min 2 characters).");
                AppendParameterDescription(operation, "is_active", "Filter by active flag. Allowed: true | false. Omit for all.");
                AppendParameterDescription(operation, "sort_by", "Allowed: name | type | status | created_at. Default name.");
                AppendParameterDescription(operation, "sort_order", "Allowed: asc | desc. Default asc.");
                AppendParameterDescription(operation, "page", "Page number (default 1).");
                AppendParameterDescription(operation, "size", "Page size (default 20).");
                return;

            case "api/v1/id-card-types/get" when method.Equals("GET", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 404, SwaggerExamples.EnvelopeFor(typeof(Respons<IdCardTypeListItemDto>), 404));
                operation.Summary ??= "Get ID card type";
                operation.Description = SwaggerOptionFormat.Append(operation.Description,
                    "Single row for view/edit drawer.");
                AppendParameterDescription(operation, "id_card_type_id", "ID card type UUID from list.");
                return;

            case "api/v1/id-card-types/add" when method.Equals("POST", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 400, SwaggerExamples.EnvelopeFor(typeof(Respons<IdCardTypeListItemDto>), 400));
                operation.Summary ??= "Add custom ID card type";
                operation.Description = SwaggerOptionFormat.Append(operation.Description,
                    "Creates a custom type (`type=custom`). System defaults are seeded automatically — do not POST them.");
                return;

            case "api/v1/id-card-types/update" when method.Equals("PUT", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 400, SwaggerExamples.EnvelopeFor(typeof(Respons<IdCardTypeListItemDto>), 400));
                SetJsonResponseExample(operation, 404, SwaggerExamples.EnvelopeFor(typeof(Respons<IdCardTypeListItemDto>), 404));
                operation.Summary ??= "Update ID card type";
                operation.Description = SwaggerOptionFormat.Append(operation.Description,
                    """
                    Partial body — send only fields to change.
                    • **Custom** (`type=custom`): name · description · is_active — all editable.
                    • **System default** (`type=default`): description and is_active only — **name change returns 400**.
                    """);
                AppendParameterDescription(operation, "id_card_type_id", "ID card type UUID to update.");
                return;

            case "api/v1/id-card-types/delete" when method.Equals("DELETE", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 404, SwaggerExamples.EnvelopeFor(typeof(Respons<object>), 404));
                operation.Summary ??= "Delete custom ID card type";
                operation.Description = SwaggerOptionFormat.Append(operation.Description,
                    """
                    Custom types only. **400** when system default.
                    Prefer deactivating (`is_active: false` on PUT) when history must be kept.
                    """);
                AppendParameterDescription(operation, "id_card_type_id", "Custom ID card type UUID.");
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
