using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using ZelosHR.Api.Entities.EmployeeIdFormat;
using ZelosHR.Api.Entities.Shared;

namespace ZelosHR.Api.Configs;

/// <summary>Company Settings — employee ID format OpenAPI examples.</summary>
public sealed class SwaggerEmployeeIdFormatOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var method = context.ApiDescription.HttpMethod ?? "";
        var path = context.ApiDescription.RelativePath ?? "";
        if (!path.StartsWith("api/v1/company/id-format", StringComparison.OrdinalIgnoreCase))
            return;

        switch (path)
        {
            case "api/v1/company/id-format/list" when method.Equals("GET", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.EmployeeIdFormatListResponse());
                operation.Summary ??= "List employee ID format settings";
                operation.Description = SwaggerOptionFormat.Append(operation.Description,
                    """
                    Returns zero or one row for the current org. Does not auto-create defaults.
                    """);
                return;

            case "api/v1/company/id-format/get" when method.Equals("GET", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.EmployeeIdFormatGetResponse());
                SetJsonResponseExample(operation, 404, SwaggerExamples.EnvelopeFor(typeof(Respons<EmployeeIdFormatReadDto>), 404));
                operation.Summary ??= "Get employee ID format settings";
                operation.Description = SwaggerOptionFormat.Append(operation.Description,
                    """
                    404 when not configured — use POST /add to create. Includes next_id_preview when settings exist.
                    Applied to all new employees when auto_generate is true.
                    """);
                return;

            case "api/v1/company/id-format/add" when method.Equals("POST", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 400, SwaggerExamples.EnvelopeFor(typeof(Respons<EmployeeIdFormatReadDto>), 400));
                operation.Summary ??= "Create employee ID format settings";
                return;

            case "api/v1/company/id-format/update" when method.Equals("PUT", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 400, SwaggerExamples.EnvelopeFor(typeof(Respons<EmployeeIdFormatReadDto>), 400));
                SetJsonResponseExample(operation, 404, SwaggerExamples.EnvelopeFor(typeof(Respons<EmployeeIdFormatReadDto>), 404));
                operation.Summary ??= "Update employee ID format settings";
                return;

            case "api/v1/company/id-format/delete" when method.Equals("DELETE", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 404, SwaggerExamples.EnvelopeFor(typeof(Respons<object>), 404));
                operation.Summary ??= "Delete employee ID format settings";
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
}
