using System.Reflection;
using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ZelosHR.Api.Configs;

/// <summary>Applies <see cref="SwaggerAllowedValuesAttribute"/> as OpenAPI string enums on schemas.</summary>
public sealed class SwaggerAllowedValuesSchemaFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        var attr = context.MemberInfo?.GetCustomAttribute<SwaggerAllowedValuesAttribute>()
            ?? context.ParameterInfo?.GetCustomAttribute<SwaggerAllowedValuesAttribute>()
            ?? context.Type.GetCustomAttribute<SwaggerAllowedValuesAttribute>();

        if (attr is null)
            return;

        ApplyAllowedValues(schema, attr);
    }

    internal static void ApplyAllowedValues(IOpenApiSchema schema, SwaggerAllowedValuesAttribute attr)
    {
        if (schema is not OpenApiSchema mutable)
            return;

        var values = attr.Resolve();
        if (values.Count == 0)
            return;

        mutable.Type = JsonSchemaType.String;
        mutable.Enum = values.Select(v => (JsonNode)JsonValue.Create(v)!).ToList();
        mutable.Example = JsonValue.Create(values[0]);

        var allowed = string.Join(" | ", values);
        var allowedText = $"Allowed: {allowed}";

        if (!string.IsNullOrWhiteSpace(attr.Description))
        {
            mutable.Description = string.IsNullOrWhiteSpace(mutable.Description)
                ? $"{attr.Description} {allowedText}"
                : $"{mutable.Description} {attr.Description} {allowedText}";
        }
        else
        {
            mutable.Description = string.IsNullOrWhiteSpace(mutable.Description)
                ? allowedText
                : $"{mutable.Description} {allowedText}";
        }
    }
}
