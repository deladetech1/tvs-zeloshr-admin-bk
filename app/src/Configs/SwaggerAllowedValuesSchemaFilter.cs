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

        var isStringCollection = context.MemberInfo is PropertyInfo propertyInfo
                                 && IsStringCollection(propertyInfo.PropertyType);

        ApplyAllowedValues(schema, attr, usePipeJoinedExample: true, isStringCollection);
    }

    private static bool IsStringCollection(Type type)
    {
        if (type == typeof(string))
            return false;

        if (type.IsArray)
            return type.GetElementType() == typeof(string);

        if (!type.IsGenericType)
            return false;

        var genericDef = type.GetGenericTypeDefinition();
        if (genericDef != typeof(IReadOnlyList<>) && genericDef != typeof(IList<>)
            && genericDef != typeof(List<>) && genericDef != typeof(IEnumerable<>))
            return false;

        return type.GetGenericArguments()[0] == typeof(string);
    }

    internal static void ApplyAllowedValues(
        IOpenApiSchema schema,
        SwaggerAllowedValuesAttribute attr,
        bool usePipeJoinedExample = true,
        bool isStringCollection = false)
    {
        if (schema is not OpenApiSchema mutable)
            return;

        var values = attr.Resolve();
        if (values.Count == 0)
            return;

        var pipeExample = usePipeJoinedExample ? SwaggerOptionFormat.JoinPipe(values) : values[0];
        var allowedNote = SwaggerOptionFormat.Allowed(values);

        if (isStringCollection)
        {
            mutable.Type = JsonSchemaType.Array;
            mutable.Items = BuildStringEnumSchema(values, pipeExample, attr.Description, allowedNote);
            mutable.Example = new JsonArray(pipeExample);
            mutable.Description = SwaggerOptionFormat.Append(mutable.Description,
                $"Each element: {allowedNote}. Example shows all valid values in one slot.");
            return;
        }

        if (mutable.Type == JsonSchemaType.Array
            || (mutable.Items is not null && mutable.Type is null))
        {
            mutable.Type = JsonSchemaType.Array;
            mutable.Items = BuildStringEnumSchema(values, pipeExample, attr.Description, allowedNote);
            mutable.Example = new JsonArray(pipeExample);
            mutable.Description = SwaggerOptionFormat.Append(mutable.Description,
                $"Each element: {allowedNote}. Example shows all valid values in one slot.");
            return;
        }

        mutable.Type = JsonSchemaType.String;
        mutable.Enum = values.Select(v => (JsonNode)JsonValue.Create(v)!).ToList();
        mutable.Example = JsonValue.Create(pipeExample);

        if (!string.IsNullOrWhiteSpace(attr.Description))
            mutable.Description = SwaggerOptionFormat.Append(mutable.Description, attr.Description);

        mutable.Description = SwaggerOptionFormat.Append(mutable.Description, allowedNote);
    }

    private static OpenApiSchema BuildStringEnumSchema(
        IReadOnlyList<string> values,
        string pipeExample,
        string? description,
        string allowedNote)
    {
        var items = new OpenApiSchema
        {
            Type = JsonSchemaType.String,
            Enum = values.Select(v => (JsonNode)JsonValue.Create(v)!).ToList(),
            Example = JsonValue.Create(pipeExample),
        };
        if (!string.IsNullOrWhiteSpace(description))
            items.Description = SwaggerOptionFormat.Append(items.Description, description);
        items.Description = SwaggerOptionFormat.Append(items.Description, allowedNote);
        return items;
    }
}
