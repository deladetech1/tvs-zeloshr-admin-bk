using System.Reflection;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ZelosHR.Api.Configs;

/// <summary>Applies <see cref="SwaggerAllowedValuesAttribute"/> on action parameters (e.g. <c>[FromQuery]</c>).</summary>
public sealed class SwaggerAllowedValuesParameterFilter : IParameterFilter
{
    public void Apply(IOpenApiParameter parameter, ParameterFilterContext context)
    {
        var attr = context.ParameterInfo?.GetCustomAttribute<SwaggerAllowedValuesAttribute>();
        if (attr is null || parameter.Schema is not OpenApiSchema schema)
            return;

        SwaggerAllowedValuesSchemaFilter.ApplyAllowedValues(schema, attr, usePipeJoinedExample: false);
    }
}
