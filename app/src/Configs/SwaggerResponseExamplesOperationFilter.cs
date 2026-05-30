using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ZelosHR.Api.Configs;

/// <summary>Attaches response examples for key read operations.</summary>
public sealed class SwaggerResponseExamplesOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var method = context.ApiDescription.HttpMethod ?? "";
        var path = context.ApiDescription.RelativePath ?? "";

        if (!method.Equals("GET", StringComparison.OrdinalIgnoreCase)
            || !path.Equals("api/v1/employees/id", StringComparison.OrdinalIgnoreCase))
            return;

        if (!operation.Responses.TryGetValue("200", out var response) || response.Content is null)
            return;

        if (!response.Content.TryGetValue("application/json", out var media))
            return;

        media.Example = SwaggerExamples.EmployeeAggregateReadResponse();
        media.Examples = new Dictionary<string, IOpenApiExample>
        {
            ["aggregate_read"] = new OpenApiExample
            {
                Summary = "Employee aggregate (envelope)",
                Description = "Full employee record with nested sections, joined currency metadata, and document_ids.",
                Value = SwaggerExamples.EmployeeAggregateReadResponse(),
            },
        };
    }
}
