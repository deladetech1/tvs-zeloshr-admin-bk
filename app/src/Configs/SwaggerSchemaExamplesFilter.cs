using System.Reflection;
using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using ZelosHR.Api.Entities.Branches;
using ZelosHR.Api.Entities.Currencies;
using ZelosHR.Api.Entities.CustomFields;
using ZelosHR.Api.Entities.Departments;
using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Entities.Files;
using ZelosHR.Api.Entities.OrgStructure;
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
            nameof(EmployeeAggregateEducationDto) => SwaggerExamples.EducationSection(),
            nameof(CreateEmployeeAggregateRequest) => SwaggerExamples.CreateEmployeeFinalised(),
            nameof(UpdateEmployeeAggregateRequest) => SwaggerExamples.UpdateEmployeeFull(),
            nameof(CreateCustomFieldDefinitionDto) => SwaggerExamples.CreateCustomFieldAddBody(),
            nameof(ImportEmployeesRequest) => SwaggerExamples.ImportEmployeesRequestBody(),
            nameof(EmployeeAggregateReadDto) => SwaggerExamples.EmployeeAggregateReadData(),
            nameof(DocumentReadDto) => SwaggerExamples.EmployeeDocumentItem(),
            nameof(EmployeeDirectorySummaryDto) => SwaggerExamples.EmployeeDirectorySummaryData(),
            nameof(GetCurrencySimpleReadDto) => SwaggerExamples.CurrencyItem(),
            nameof(FileDeleteReadDto) => SwaggerExamples.FileDeleteData(),
            nameof(FileResponseReadDto) => SwaggerExamples.FileResponseData(),
            nameof(FileUploadMultipleReadDto) => new JsonObject { ["id"] = SwaggerExamples.SampleDocumentId1 },
            nameof(CreateDepartmentRequestDto) => SwaggerExamples.CreateDepartmentRoot(),
            nameof(UpdateDepartmentRequestDto) => SwaggerExamples.UpdateDepartmentBody(),
            nameof(OrgChartDto) => SwaggerExamples.OrgChartDataForSchema(),
            nameof(BranchListItemDto) => SwaggerExamples.BranchListItemExample(),
            nameof(DepartmentListItemDto) => SwaggerExamples.DepartmentListItemExample(),
            nameof(OrgChartNodeDto) => SwaggerExamples.OrgChartNodeExample(),
            _ => schema.Example,
        };

        schema.Description = context.Type.Name switch
        {
            nameof(CreateEmployeeAggregateRequest) => AppendDescription(schema.Description,
                "One-shot employee create. See operation examples (finalised vs draft). Upload files first via POST /api/v1/file/post/multiple."),
            nameof(UpdateEmployeeAggregateRequest) => AppendDescription(schema.Description,
                "Partial or full profile update — same shape as POST /add. Pass employee_id on the query string, not in the body."),
            nameof(EmployeeDirectorySummaryDto) => AppendDescription(schema.Description,
                "Directory KPI cards: total headcount, active, on probation, on contract."),
            nameof(CreateCustomFieldDefinitionDto) => AppendDescription(schema.Description,
                SwaggerExamples.CreateCustomFieldAddExampleDescription()),
            nameof(GetCurrencySimpleReadDto) => AppendDescription(schema.Description,
                "Tenant currency from core_platform.cp_currencies. List via GET /api/v1/currencies/list."),
            nameof(EmployeeAggregateCompensationDto) => AppendDescription(schema.Description,
                $"currency_id from GET /api/v1/currencies/list. pay_frequency: {SwaggerExampleHints.PayFrequency}."),
            nameof(EmployeeAggregateEducationDto) => AppendDescription(schema.Description,
                "Single education block (not an array). Partial PUT like employment — institution required when first adding the section."),
            nameof(EmployeeAggregateReadDto) => AppendDescription(schema.Description,
                "Read-only employee aggregate. documents[] on read: MyStoreGuard DocumentReadDto (doc_id, name, presigned_url, description). Write via document_ids string array."),
            nameof(DocumentReadDto) => AppendDescription(schema.Description,
                "MyStoreGuard embedded document on entity read. Write via document_ids (registry IDs from POST /file/post/multiple)."),
            nameof(FileUploadMultipleReadDto) => AppendDescription(schema.Description,
                "Registry ID from upload. Attach on employee create/update as document_ids string."),
            nameof(FileResponseReadDto) => AppendDescription(schema.Description,
                "Presigned URL expires after 24 hours. Re-call GET /file/list when expired."),
            nameof(FileDeleteReadDto) => AppendDescription(schema.Description,
                "Echo of storage location after delete."),
            nameof(CreateDepartmentRequestDto) => AppendDescription(schema.Description,
                "Create department. Optional parent_department_id and head_of_department_id (employee UUID)."),
            nameof(OrgChartDto) => AppendDescription(schema.Description,
                "Nested department tree. Each node: id · name · node_type (department) · parent_id · head_of_department · employee_count · children."),
            nameof(BranchListItemDto) => AppendDescription(schema.Description,
                $"branch_id (UUID) · name · employee_count · is_archived ({SwaggerExampleHints.OrgIncludeArchived})."),
            nameof(DepartmentListItemDto) => AppendDescription(schema.Description,
                $"department_id (UUID) · name · parent_department_id · head_of_department · employee_count · is_archived ({SwaggerExampleHints.OrgIncludeArchived}) · hierarchy_level."),
            nameof(OrgChartNodeDto) => AppendDescription(schema.Description,
                $"Chart node. node_type: {SwaggerExampleHints.OrgNodeType}. parent_id null on roots."),
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

    private static bool IsCustomFieldDefinitionProperty(PropertyInfo property) =>
        property.DeclaringType?.Namespace?.Contains("CustomFields", StringComparison.Ordinal) == true
        || property.DeclaringType?.Name.Contains("CustomField", StringComparison.Ordinal) == true;

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
        if (property.GetCustomAttribute<SwaggerAllowedValuesAttribute>() is not null)
            return;

        var name = property.Name;
        var type = property.PropertyType;

        if (name.Equals("ProfileUrl", StringComparison.OrdinalIgnoreCase))
        {
            if (property.DeclaringType == typeof(EmployeeAggregateIdentityReadDto)
                || property.DeclaringType == typeof(EmployeeListItemDto))
            {
                schema.Example = SwaggerExamples.EmployeeDocumentItem(
                    SwaggerExamples.SampleDocumentId1, "Employee profile photo");
                schema.Description = AppendDescription(schema.Description,
                    "Profile photo on read: id, presigned_url (~24h), description.");
                return;
            }

            if (property.DeclaringType == typeof(EmployeeAggregateIdentityDto))
            {
                schema.Example = JsonValue.Create(SwaggerExamples.SampleDocumentId1);
                schema.Description = AppendDescription(schema.Description,
                    "Write: document id string from POST /file/post/multiple. Read (GET): DocumentReadDto with doc_id, name, presigned_url, description.");
                return;
            }
        }

        if (name.Equals("DocId", StringComparison.OrdinalIgnoreCase)
            && property.DeclaringType == typeof(DocumentReadDto))
        {
            schema.Example = JsonValue.Create(SwaggerExamples.SampleDocumentId1);
            return;
        }

        if (name.Equals("Id", StringComparison.OrdinalIgnoreCase)
            && property.DeclaringType is { } declaring
            && (declaring == typeof(FileUploadMultipleReadDto)
                || declaring == typeof(FileResponseReadDto)))
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
            case nameof(EmployeeAggregateEducationDto.Institution):
                schema.Example = JsonValue.Create("University of Ghana");
                return;
            case nameof(EmployeeAggregateEducationDto.Degree):
                schema.Example = JsonValue.Create("BSc Computer Science");
                return;
            case nameof(EmployeeAggregateEducationDto.FieldOfStudy):
                schema.Example = JsonValue.Create("Computer Science");
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
                    "Required when gross_salary is set (unless tenant default applies). List options: GET /api/v1/currencies/list.");
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
                    "Computed on save: Monthly × 12 | Bi-weekly × 26.");
                return;
            case "Options" when IsCustomFieldDefinitionProperty(property):
                schema.Example = JsonValue.Create(SwaggerExampleHints.SelectOptionsPipe);
                schema.Description = AppendDescription(schema.Description,
                    "Required for field_type select|multiselect. Wire: JSON array string. Example choices: option_a|option_b|option_c — pick one per slot on real requests.");
                return;
            case nameof(CreateCustomFieldDefinitionDto.FieldKey):
                schema.Example = JsonValue.Create("bonus_eligible");
                schema.Description = AppendDescription(schema.Description,
                    "Stable API key used in employee section custom_fields objects.");
                return;
            case nameof(CreateCustomFieldDefinitionDto.Label):
                schema.Example = JsonValue.Create("Bonus eligible");
                return;
            case "BranchId" when property.DeclaringType == typeof(BranchListItemDto):
                schema.Example = JsonValue.Create(SwaggerExamples.SampleBranchId.ToString());
                schema.Description = AppendDescription(schema.Description,
                    "UUID from POST /org-structure/branches/add or GET /org-structure/branches.");
                return;
            case "IsArchived" when property.DeclaringType == typeof(BranchListItemDto)
                                  || property.DeclaringType == typeof(DepartmentListItemDto):
                schema.Example = JsonValue.Create(SwaggerExampleHints.BooleanPipe);
                schema.Description = AppendDescription(schema.Description,
                    $"Allowed: {SwaggerExampleHints.OrgIncludeArchived}.");
                return;
            case "DepartmentId" when property.DeclaringType == typeof(DepartmentListItemDto):
                schema.Example = JsonValue.Create(SwaggerExamples.SampleDepartmentId.ToString());
                return;
            case "NodeType" when property.DeclaringType == typeof(OrgChartNodeDto):
                schema.Example = JsonValue.Create(SwaggerExampleHints.OrgNodeType);
                schema.Description = AppendDescription(schema.Description,
                    $"Allowed: {SwaggerExampleHints.OrgNodeType}.");
                return;
            case "EmployeeCount":
                schema.Example = JsonValue.Create(24);
                schema.Description = AppendDescription(schema.Description,
                    "Active employees assigned to this department or branch.");
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

        if (name.Equals("Documents", StringComparison.OrdinalIgnoreCase)
            && property.DeclaringType == typeof(EmployeeAggregateReadDto))
        {
            schema.Example = SwaggerExamples.EmployeeDocumentsArray();
            schema.Description = AppendDescription(schema.Description,
                "Read only. MyStoreGuard DocumentReadDto per item (doc_id, name, presigned_url, description). On create/update send string IDs in document_ids.");
            return;
        }

        if (name.Equals("DocumentIds", StringComparison.OrdinalIgnoreCase))
        {
            if (property.DeclaringType == typeof(CreateEmployeeAggregateRequest)
                || property.DeclaringType == typeof(UpdateEmployeeAggregateRequest))
            {
                schema.Example = new JsonArray(SwaggerExamples.SampleDocumentId1, SwaggerExamples.SampleDocumentId2);
                schema.Description = AppendDescription(schema.Description,
                    "Append registry IDs from POST /file/post/multiple (string array on write).");
                return;
            }
        }

        if (name.Equals("DeleteDocumentIds", StringComparison.OrdinalIgnoreCase))
        {
            schema.Example = new JsonArray(SwaggerExamples.SampleDocumentId2);
            schema.Description = AppendDescription(schema.Description,
                "Remove registry IDs from the employee record.");
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
            var n when n.Contains("Identity", StringComparison.OrdinalIgnoreCase) => EmployeeCustomFieldSections.Identity,
            var n when n.Contains("Employment", StringComparison.OrdinalIgnoreCase) => EmployeeCustomFieldSections.Employment,
            var n when n.Contains("Compensation", StringComparison.OrdinalIgnoreCase) => EmployeeCustomFieldSections.Compensation,
            var n when n.Contains("Education", StringComparison.OrdinalIgnoreCase) => EmployeeCustomFieldSections.Education,
            var n when n.Contains("Certification", StringComparison.OrdinalIgnoreCase) => EmployeeCustomFieldSections.Certification,
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

    internal static string? AppendDescription(string? existing, string addition) =>
        SwaggerOptionFormat.Append(existing, addition);
}
