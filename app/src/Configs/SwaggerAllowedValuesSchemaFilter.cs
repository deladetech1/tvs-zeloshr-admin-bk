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

        ApplyAllowedValues(schema, attr, usePipeJoinedExample: true);
    }

    internal static void ApplyAllowedValues(
        IOpenApiSchema schema,
        SwaggerAllowedValuesAttribute attr,
        bool usePipeJoinedExample = true)
    {
        if (schema is not OpenApiSchema mutable)
            return;

        var values = attr.Resolve();
        if (values.Count == 0)
            return;

        mutable.Type = JsonSchemaType.String;
        mutable.Enum = values.Select(v => (JsonNode)JsonValue.Create(v)!).ToList();
        mutable.Example = JsonValue.Create(
            usePipeJoinedExample ? SwaggerOptionFormat.Join(values) : values[0]);

        if (!string.IsNullOrWhiteSpace(attr.Description))
            mutable.Description = SwaggerOptionFormat.Append(mutable.Description, attr.Description);

        mutable.Description = SwaggerOptionFormat.Append(mutable.Description, SwaggerOptionFormat.Allowed(values));
    }
}
