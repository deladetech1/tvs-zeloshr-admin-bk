using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using ZelosHR.Api.Entities.CustomFields;
using ZelosHR.Api.Entities.Employees;

namespace ZelosHR.Api.Configs;

/// <summary>Marks required properties in OpenAPI schemas (Swagger UI red asterisk + validation hints).</summary>
public sealed class SwaggerRequiredSchemaFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema is not OpenApiSchema mutable)
            return;

        if (mutable.Properties is null || mutable.Properties.Count == 0)
            return;

        var required = mutable.Required is not null
            ? new HashSet<string>(mutable.Required)
            : new HashSet<string>();

        foreach (var property in context.Type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetCustomAttribute<RequiredAttribute>() is not null)
                required.Add(ToJsonName(property.Name));
        }

        ApplyTypeRules(context.Type, required);

        if (required.Count > 0)
            mutable.Required = required;
    }

    private static void ApplyTypeRules(Type type, HashSet<string> required)
    {
        if (type == typeof(CreateEmployeeAggregateRequest))
        {
            required.Add("identity");
        }

        if (type == typeof(EmployeeAggregateIdentityDto))
        {
            required.Add("full_name");
            required.Add("phone");
        }

        if (type == typeof(EmployeeEducationUpsertDto))
        {
            required.Add("institution");
        }

        if (type == typeof(EmployeeCertificationUpsertDto)
            || type == typeof(EmployeeCertificationWriteDto))
        {
            required.Add("name");
        }

        if (type == typeof(CreateCustomFieldDefinitionDto))
        {
            required.Add("entity_type");
            required.Add("field_key");
            required.Add("label");
            required.Add("field_type");
        }
    }

    private static string ToJsonName(string propertyName) =>
        JsonNamingPolicy.SnakeCaseLower.ConvertName(propertyName);
}
