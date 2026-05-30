using System.Reflection;
using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using ZelosHR.Api.Entities.Shared;

namespace ZelosHR.Api.Configs;

/// <summary>
/// Supplies realistic examples for <see cref="Respons{T}"/> envelopes and related shared types
/// so Swagger does not show generic string/0/additionalProp placeholders.
/// </summary>
public sealed class SwaggerEnvelopeSchemaFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema is not OpenApiSchema mutable)
            return;

        if (SwaggerExamples.IsResponsType(context.Type))
        {
            mutable.Example = SwaggerExamples.EnvelopeFor(context.Type);
            return;
        }

        if (context.Type == typeof(PaginationMeta))
        {
            mutable.Example = SwaggerExamples.SamplePagination();
            return;
        }

        if (context.MemberInfo is PropertyInfo property)
        {
            if (property.DeclaringType is not null && SwaggerExamples.IsResponsType(property.DeclaringType))
            {
                ApplyResponsPropertyExample(mutable, property);
                return;
            }

            if (property.DeclaringType == typeof(PaginationMeta))
                ApplyPaginationPropertyExample(mutable, property);
        }
    }

    private static void ApplyResponsPropertyExample(OpenApiSchema schema, PropertyInfo property)
    {
        switch (property.Name)
        {
            case nameof(Respons<object>.Detail):
                schema.Example = JsonValue.Create("OK");
                return;
            case nameof(Respons<object>.Success):
                schema.Example = JsonValue.Create(true);
                return;
            case nameof(Respons<object>.StatusCode):
                schema.Example = JsonValue.Create(200);
                return;
            case nameof(Respons<object>.Message):
                schema.Example = JsonValue.Create("OK");
                return;
            case nameof(Respons<object>.Error):
                schema.Example = null;
                return;
            case nameof(Respons<object>.FieldErrors):
                schema.Example = SwaggerExamples.SampleFieldErrors();
                return;
            case nameof(Respons<object>.Pagination):
                schema.Example = null;
                return;
            case nameof(Respons<object>.Errors):
                schema.Example = new JsonArray();
                return;
        }
    }

    private static void ApplyPaginationPropertyExample(OpenApiSchema schema, PropertyInfo property)
    {
        schema.Example = property.Name switch
        {
            nameof(PaginationMeta.Page) => JsonValue.Create(1),
            nameof(PaginationMeta.Size) or nameof(PaginationMeta.PageSize) => JsonValue.Create(20),
            nameof(PaginationMeta.Total) or nameof(PaginationMeta.TotalCount) => JsonValue.Create(42),
            nameof(PaginationMeta.HasNext) => JsonValue.Create(true),
            nameof(PaginationMeta.TotalPages) => JsonValue.Create(3),
            _ => schema.Example,
        };
    }
}
