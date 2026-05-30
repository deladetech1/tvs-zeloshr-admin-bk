using System.Reflection;
using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using ZelosHR.Api.Entities.CustomFields;
using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Entities.Shared;

namespace ZelosHR.Api.Configs;

/// <summary>
/// Replaces generic Swagger placeholders with realistic property- and type-level examples.
/// </summary>
public sealed class SwaggerSchemaExamplesFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema is not OpenApiSchema mutable)
            return;

        ApplyTypeLevelExample(mutable, context);

        if (IsStringDictionary(context.Type))
        {
            var dictProperty = context.MemberInfo as PropertyInfo;
            if (dictProperty?.Name.Equals(nameof(Respons<object>.FieldErrors), StringComparison.OrdinalIgnoreCase) == true)
            {
                ApplyFieldErrorsDictionary(mutable);
                return;
            }

            if (IsCustomFieldsProperty(dictProperty))
            {
                ApplyCustomFieldsDictionary(mutable, dictProperty);
                return;
            }

            ApplyGenericStringDictionary(mutable);
            return;
        }

        if (context.MemberInfo is not PropertyInfo property)
            return;

        ApplyPropertyExample(mutable, property);
    }

    private static void ApplyTypeLevelExample(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.MemberInfo is not null)
            return;

        schema.Example = context.Type.Name switch
        {
            nameof(CreateEmployeeAggregateRequest) => SwaggerExamples.CreateEmployeeFinalised(),
            nameof(UpdateEmployeeAggregateRequest) => SwaggerExamples.UpdateEmployeePartial(),
            nameof(CreateCustomFieldDefinitionDto) => SwaggerExamples.CreateCustomFieldCompensation(),
            nameof(EmployeeAggregateReadDto) => SwaggerExamples.EmployeeAggregateReadData(),
            nameof(FileDeleteReadDto) => SwaggerExamples.FileDeleteData(),
            nameof(FileResponseReadDto) => SwaggerExamples.FileResponseData(),
            nameof(FileUploadMultipleReadDto) => new JsonObject { ["id"] = SwaggerExamples.SampleDocumentId1 },
            _ => schema.Example,
        };

        schema.Description = context.Type.Name switch
        {
            nameof(CreateEmployeeAggregateRequest) => AppendDescription(schema.Description,
                "One-shot employee create. See operation examples (finalised vs draft). Upload files first via POST /api/v1/file/post/multiple."),
            nameof(UpdateEmployeeAggregateRequest) => AppendDescription(schema.Description,
                "Partial update — include only fields to change plus required id. Set status to finalised to complete a draft."),
            nameof(CreateCustomFieldDefinitionDto) => AppendDescription(schema.Description,
                "Defines schema for a custom field. Values are sent later on employee create/update under the matching section custom_fields object."),
            nameof(EmployeeAggregateCompensationDto) => AppendDescription(schema.Description,
                "currency_id references core_platform.cp_currencies (seeded per tenant). gross_salary + Monthly/Bi-weekly drives annualized_cost on read."),
            nameof(FileUploadMultipleReadDto) => AppendDescription(schema.Description,
                "Registry ID from upload. Attach on employee create/update as document_ids string."),
            nameof(FileResponseReadDto) => AppendDescription(schema.Description,
                "Presigned URL expires after 24 hours. Re-call GET /file/list when expired."),
            nameof(FileDeleteReadDto) => AppendDescription(schema.Description,
                "blob_path is the client-supplied path from upload. container_name is server config (not sent on upload)."),
            _ => schema.Description,
        };
    }

    private static void ApplyFieldErrorsDictionary(OpenApiSchema schema)
    {
        schema.AdditionalPropertiesAllowed = true;
        schema.AdditionalProperties = new OpenApiSchema
        {
            Type = JsonSchemaType.String,
            Example = JsonValue.Create("This field is required."),
        };
        schema.Example = SwaggerExamples.SampleFieldErrors();
        schema.Description = SwaggerSchemaExamplesFilter.AppendDescription(schema.Description,
            "Present on 400 validation responses. Keys are snake_case field paths.");
    }

    private static void ApplyGenericStringDictionary(OpenApiSchema schema)
    {
        schema.AdditionalPropertiesAllowed = true;
        schema.AdditionalProperties = new OpenApiSchema
        {
            Type = JsonSchemaType.String,
            Example = JsonValue.Create("example-value"),
        };
        schema.Example = new JsonObject { ["example_key"] = "example-value" };
    }

    private static bool IsCustomFieldsProperty(PropertyInfo? property) =>
        property?.Name.Equals("CustomFields", StringComparison.OrdinalIgnoreCase) == true;

    private static void ApplyCustomFieldsDictionary(OpenApiSchema schema, PropertyInfo? property)
    {
        var section = ResolveCustomFieldSection(property);
        schema.AdditionalPropertiesAllowed = true;
        schema.AdditionalProperties = new OpenApiSchema
        {
            Type = JsonSchemaType.String | JsonSchemaType.Null,
            Example = JsonValue.Create("value"),
        };
        schema.Example = section is null
            ? new JsonObject()
            : SwaggerExamples.CustomFieldsForSection(section);
        schema.Description = section is null
            ? AppendDescription(schema.Description,
                "Key/value map. Keys must match field_key from GET /api/v1/custom-fields/schema?entityType=employee.")
            : SwaggerExamples.CustomFieldsHelpText(section);
    }

    private static void ApplyPropertyExample(OpenApiSchema schema, PropertyInfo property)
    {
        var name = property.Name;
        var type = property.PropertyType;

        if (name.Equals("Id", StringComparison.OrdinalIgnoreCase)
            && property.DeclaringType is { } declaring
            && (declaring == typeof(FileUploadMultipleReadDto) || declaring == typeof(FileResponseReadDto)))
        {
            schema.Example = JsonValue.Create(SwaggerExamples.SampleDocumentId1);
            return;
        }

        switch (name)
        {
            case nameof(EmployeeAggregateIdentityDto.FullName):
                schema.Example = JsonValue.Create("Ada Lovelace");
                return;
            case nameof(EmployeeAggregateIdentityDto.WorkEmail):
                schema.Example = JsonValue.Create("ada.lovelace@company.com");
                return;
            case nameof(EmployeeAggregateIdentityDto.PersonalEmail):
                schema.Example = JsonValue.Create("ada.personal@example.com");
                return;
            case nameof(EmployeeAggregateIdentityDto.Phone):
                schema.Example = JsonValue.Create("+233201234567");
                return;
            case nameof(EmployeeAggregateIdentityDto.Country):
                schema.Example = JsonValue.Create("Ghana");
                return;
            case nameof(EmployeeAggregateIdentityDto.IdNumber):
                schema.Example = JsonValue.Create("GHA-123456789-0");
                return;
            case nameof(EmployeeAggregateIdentityDto.LinkedInUrl):
                schema.Example = JsonValue.Create("https://linkedin.com/in/adalovelace");
                return;
            case nameof(EmployeeAggregateIdentityDto.ResidentialAddress):
                schema.Example = JsonValue.Create("12 Independence Ave, Accra");
                return;
            case nameof(EmployeeAggregateEmploymentDto.JobTitle):
                schema.Example = JsonValue.Create("Software Engineer");
                return;
            case nameof(EmployeeAggregateEmploymentDto.WorkLocation):
                schema.Example = JsonValue.Create("Accra HQ");
                return;
            case nameof(EmployeeAggregateEmploymentDto.PayGrade):
                schema.Example = JsonValue.Create("P4");
                return;
            case nameof(EmployeeAggregateEmploymentDto.WorkingHours):
                schema.Example = JsonValue.Create("40");
                return;
            case nameof(EmployeeAggregateEmploymentDto.NoticePeriod):
                schema.Example = JsonValue.Create("30 days");
                return;
            case nameof(EmployeeAggregateCompensationDto.GrossSalary):
                schema.Example = JsonValue.Create(8500.00m);
                return;
            case nameof(FileResponseReadDto.PresignedUrl):
                schema.Example = JsonValue.Create(SwaggerExamples.SamplePresignedUrl);
                return;
            case nameof(FileResponseReadDto.FileName):
                schema.Example = JsonValue.Create("contract.pdf");
                return;
            case nameof(FileResponseReadDto.Description):
                schema.Example = JsonValue.Create("Employment contract");
                return;
            case nameof(FileDeleteReadDto.BlobPath):
                schema.Example = JsonValue.Create(SwaggerExamples.SampleBlobPathSingle);
                return;
            case nameof(FileDeleteReadDto.ContainerName):
                schema.Example = JsonValue.Create(SwaggerExamples.SampleDocumentsContainer);
                return;
            case nameof(FileDeleteReadDto.Message):
                schema.Example = JsonValue.Create("File deleted successfully.");
                return;
            case nameof(EmployeeAggregateCompensationDto.CurrencyId):
                schema.Example = JsonValue.Create(SwaggerExamples.SampleCurrencyId);
                schema.Description = AppendDescription(schema.Description,
                    "Required when gross_salary is set (unless tenant default currency applies). List currencies from core_platform.cp_currencies for the tenant.");
                return;
            case nameof(EmployeeAggregateCompensationReadDto.CurrencyCode):
                schema.Example = JsonValue.Create("GHS");
                return;
            case nameof(EmployeeAggregateCompensationReadDto.CurrencyName):
                schema.Example = JsonValue.Create("Ghana Cedi");
                return;
            case nameof(EmployeeAggregateCompensationReadDto.CurrencySymbol):
                schema.Example = JsonValue.Create("₵");
                return;
            case nameof(EmployeeAggregateCompensationReadDto.AnnualizedCost):
                schema.Example = JsonValue.Create(102000.00m);
                schema.Description = AppendDescription(schema.Description,
                    "Computed on save: Monthly × 12, Bi-weekly × 26.");
                return;
            case nameof(CreateCustomFieldDefinitionDto.FieldKey):
                schema.Example = JsonValue.Create("bonus_eligible");
                schema.Description = AppendDescription(schema.Description,
                    "Stable API key used in employee section custom_fields objects.");
                return;
            case nameof(CreateCustomFieldDefinitionDto.Label):
                schema.Example = JsonValue.Create("Bonus eligible");
                return;
            case nameof(CreateCustomFieldDefinitionDto.SectionName):
                schema.Example = JsonValue.Create("compensation");
                schema.Description = AppendDescription(schema.Description,
                    "Allowed employee sections: identity | employment | compensation | education | certification");
                return;
            case nameof(CreateCustomFieldDefinitionDto.Options):
                schema.Example = JsonValue.Create("[\"yes\",\"no\"]");
                schema.Description = AppendDescription(schema.Description,
                    "JSON array string for select/multiselect field types.");
                return;
        }

        if (IsDateOnly(type))
        {
            schema.Example = JsonValue.Create(name.Contains("Birth", StringComparison.OrdinalIgnoreCase)
                ? "1990-05-15"
                : "2025-06-01");
            schema.Description = AppendDescription(schema.Description, "ISO date YYYY-MM-DD.");
            return;
        }

        if (IsGuid(type))
        {
            schema.Example = JsonValue.Create(ResolveGuidExample(name));
            return;
        }

        if (name.Equals("DocumentIds", StringComparison.OrdinalIgnoreCase)
            || name.Equals("DeleteDocumentIds", StringComparison.OrdinalIgnoreCase))
        {
            schema.Example = name.Contains("Delete", StringComparison.OrdinalIgnoreCase)
                ? new JsonArray(SwaggerExamples.SampleDocumentId2)
                : new JsonArray(SwaggerExamples.SampleDocumentId1, SwaggerExamples.SampleDocumentId2);
            schema.Description = AppendDescription(schema.Description,
                """
                Workflow: (1) POST /api/v1/file/post/multiple?blob_paths={tenant}/{org}/{bus}/employees/{filename}
                (2) use returned id strings here
                (3) GET /api/v1/file/list?document_ids=id1,id2 for presigned URLs (24h).
                """);
        }
    }

    private static string ResolveGuidExample(string propertyName) => propertyName switch
    {
        var n when n.Contains("Department", StringComparison.OrdinalIgnoreCase)
            => SwaggerExamples.SampleDepartmentId.ToString(),
        var n when n.Contains("Branch", StringComparison.OrdinalIgnoreCase)
            => SwaggerExamples.SampleBranchId.ToString(),
        var n when n.Contains("ReportsTo", StringComparison.OrdinalIgnoreCase)
            || n.Contains("Manager", StringComparison.OrdinalIgnoreCase)
            => SwaggerExamples.SampleReportsToId.ToString(),
        var n when n.Equals("Id", StringComparison.OrdinalIgnoreCase)
            => SwaggerExamples.SampleEmployeeId.ToString(),
        _ => "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    };

    private static string? ResolveCustomFieldSection(PropertyInfo? property)
    {
        if (property?.DeclaringType is null)
            return null;

        return property.DeclaringType.Name switch
        {
            var n when n.Contains("Identity", StringComparison.OrdinalIgnoreCase) => "identity",
            var n when n.Contains("Employment", StringComparison.OrdinalIgnoreCase) => "employment",
            var n when n.Contains("Compensation", StringComparison.OrdinalIgnoreCase) => "compensation",
            var n when n.Contains("Education", StringComparison.OrdinalIgnoreCase) => "education",
            var n when n.Contains("Certification", StringComparison.OrdinalIgnoreCase) => "certification",
            _ => null,
        };
    }

    private static bool IsStringDictionary(Type type)
    {
        if (!type.IsGenericType)
            return false;

        var def = type.GetGenericTypeDefinition();
        if (def != typeof(Dictionary<,>) && def != typeof(IDictionary<,>))
            return false;

        return type.GetGenericArguments()[0] == typeof(string);
    }

    private static bool IsDateOnly(Type type) =>
        (Nullable.GetUnderlyingType(type) ?? type) == typeof(DateOnly);

    private static bool IsGuid(Type type) =>
        (Nullable.GetUnderlyingType(type) ?? type) == typeof(Guid);

    internal static string? AppendDescription(string? existing, string addition)
    {
        if (string.IsNullOrWhiteSpace(addition))
            return existing;
        return string.IsNullOrWhiteSpace(existing) ? addition : $"{existing} {addition}";
    }
}
