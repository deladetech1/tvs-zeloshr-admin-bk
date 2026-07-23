using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using ZelosHR.Api.Entities.EmployeeIdFormat;
using ZelosHR.Api.Entities.Shared;

namespace ZelosHR.Api.Configs;

/// <summary>Employee Settings — employee ID format OpenAPI examples.</summary>
public sealed class SwaggerEmployeeIdFormatOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var method = context.ApiDescription.HttpMethod ?? "";
        var path = context.ApiDescription.RelativePath ?? "";
        if (!path.StartsWith("api/v1/employee-settings/id-format", StringComparison.OrdinalIgnoreCase))
            return;

        switch (path)
        {
            case "api/v1/employee-settings/id-format/get" when method.Equals("GET", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 200, SwaggerExamples.EmployeeIdFormatGetResponse());
                operation.Summary ??= "Get employee ID format settings";
                operation.Description = SwaggerOptionFormat.Append(operation.Description,
                    """
                    Always returns 200 for a valid org context. Auto-creates default settings on first GET.
                    Includes next_id_preview for the settings UI. Applied to all new employees when auto_generate is true.
                    """);
                return;

            case "api/v1/employee-settings/id-format/add" when method.Equals("POST", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 400, SwaggerExamples.EnvelopeFor(typeof(Respons<EmployeeIdFormatReadDto>), 400));
                operation.Summary ??= "Create employee ID format settings";
                return;

            case "api/v1/employee-settings/id-format/update" when method.Equals("PUT", StringComparison.OrdinalIgnoreCase):
                SetJsonResponseExample(operation, 400, SwaggerExamples.EnvelopeFor(typeof(Respons<EmployeeIdFormatReadDto>), 400));
                SetJsonResponseExample(operation, 404, SwaggerExamples.EnvelopeFor(typeof(Respons<EmployeeIdFormatReadDto>), 404));
                operation.Summary ??= "Update employee ID format settings";
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
