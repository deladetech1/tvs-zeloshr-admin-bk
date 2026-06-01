using System.Text.Json.Nodes;
using Microsoft.OpenApi;

namespace ZelosHR.Api.Configs;

/// <summary>OpenAPI allows only one of <c>example</c> or <c>examples</c> per media type.</summary>
internal static class SwaggerMediaExamples
{
    internal static void SetSingleExample(OpenApiMediaType media, JsonObject example)
    {
        media.Examples = null;
        media.Example = example;
    }

    internal static void SetNamedExamples(OpenApiMediaType media, Dictionary<string, IOpenApiExample> examples)
    {
        media.Example = null;
        media.Examples = examples;
    }
}
