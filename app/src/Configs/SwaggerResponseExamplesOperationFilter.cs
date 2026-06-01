using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ZelosHR.Api.Configs;

/// <summary>Attaches realistic full-envelope response examples for every documented JSON response.</summary>
public sealed class SwaggerResponseExamplesOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        foreach (var supported in context.ApiDescription.SupportedResponseTypes)
        {
            if (supported.Type is null)
                continue;

            if (supported.StatusCode is not (200 or 201 or 400 or 404))
                continue;

            var statusKey = supported.StatusCode.ToString();
            if (!operation.Responses.TryGetValue(statusKey, out var response) || response.Content is null)
                continue;

            if (!response.Content.TryGetValue("application/json", out var media))
                continue;

            if (media.Examples is { Count: > 0 } || media.Example is not null)
                continue;

            var example = SwaggerExamples.EnvelopeFor(supported.Type, supported.StatusCode);
            if (example is null)
                continue;

            SwaggerMediaExamples.SetSingleExample(media, example);
        }
    }
}
