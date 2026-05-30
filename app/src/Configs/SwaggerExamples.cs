using System.Text.Json.Nodes;
using ZelosHR.Api.Entities.CustomFields;
using ZelosHR.Api.Entities.Departments;
using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Entities.Files;
using ZelosHR.Api.Entities.Shared;

namespace ZelosHR.Api.Configs;

/// <summary>Canonical OpenAPI request/response examples (snake_case wire format).</summary>
internal static class SwaggerExamples
{
    internal static readonly Guid SampleDepartmentId = Guid.Parse("11111111-1111-1111-1111-111111111101");
    internal static readonly Guid SampleBranchId = Guid.Parse("11111111-1111-1111-1111-111111111102");
    internal static readonly Guid SampleEmployeeId = Guid.Parse("22222222-2222-2222-2222-222222222201");
    internal static readonly Guid SampleReportsToId = Guid.Parse("33333333-3333-3333-3333-333333333301");

    internal const string SampleCurrencyId = "cur_ghs_default";
    internal const string SampleDocumentId1 = "doc_contract_a1b2c3";
    internal const string SampleDocumentId2 = "doc_national_id_d4e5f6";
    internal const string SampleBlobPathSingle = "tenant_demo/org_demo/bus_demo/employees/contract.pdf";
    internal const string SampleBlobPathMulti1 = "tenant_demo/org_demo/bus_demo/employees/contract.pdf";
    internal const string SampleBlobPathMulti2 = "tenant_demo/org_demo/bus_demo/employees/national_id.jpg";
    internal const string SampleDocumentsContainer = "employee-documents";
    internal const string SamplePresignedUrl =
        "https://devstorage.blob.core.windows.net/employee-documents/tenant_demo/org_demo/bus_demo/employees/contract.pdf?sv=2024&se=2026-05-20T12%3A00%3A00Z&sig=example";

    internal static JsonObject FileUploadMultipleResponse() => EnvelopeOk(new JsonArray(
        new JsonObject { ["id"] = SampleDocumentId1 },
        new JsonObject { ["id"] = SampleDocumentId2 }));

    internal static JsonObject FileListResponse() => EnvelopeOk(new JsonArray(
        new JsonObject
        {
            ["id"] = SampleDocumentId1,
            ["presigned_url"] = SamplePresignedUrl,
            ["description"] = "Employment contract",
            ["file_name"] = "contract.pdf",
        },
        new JsonObject
        {
            ["id"] = SampleDocumentId2,
            ["presigned_url"] = SamplePresignedUrl,
            ["description"] = "National ID scan",
            ["file_name"] = "national_id.jpg",
        }));

    internal static JsonObject FileUpdateResponse() => EnvelopeOk(new JsonObject
    {
        ["id"] = SampleDocumentId1,
        ["presigned_url"] = SamplePresignedUrl,
        ["description"] = "Employment contract (revised)",
        ["file_name"] = "contract_v2.pdf",
    });

    internal static JsonObject FileDeleteResponse() => EnvelopeOk(FileDeleteData());

    private static JsonObject EnvelopeOk(JsonNode data, JsonObject? pagination = null)
    {
        var envelope = new JsonObject
        {
            ["success"] = true,
            ["status_code"] = 200,
            ["detail"] = "OK",
            ["message"] = "OK",
            ["data"] = data,
        };

        if (pagination is not null)
            envelope["pagination"] = pagination;

        return envelope;
    }

    internal static JsonObject SamplePagination() => new()
    {
        ["page"] = 1,
        ["size"] = 20,
        ["total"] = 42,
        ["has_next"] = true,
        ["page_size"] = 20,
        ["total_count"] = 42,
        ["total_pages"] = 3,
    };

    internal static JsonObject SampleFieldErrors() => new()
    {
        ["work_email"] = "Work email is already registered for another employee.",
        ["compensation.currency_id"] = "Currency not found for this tenant.",
    };

    internal static JsonObject ValidationErrorEnvelope(JsonObject? fieldErrors = null) => new()
    {
        ["success"] = false,
        ["status_code"] = 400,
        ["detail"] = "Validation failed",
        ["message"] = "Validation failed",
        ["error"] = "Validation failed",
        ["field_errors"] = fieldErrors ?? SampleFieldErrors(),
        ["errors"] = new JsonArray(
            "Work email is already registered for another employee.",
            "Currency not found for this tenant."),
    };

    /// <summary>Full success/error envelope example for a declared API response type.</summary>
    internal static JsonObject? EnvelopeFor(Type type, int statusCode = 200)
    {
        if (statusCode is >= 400 and < 500)
            return statusCode switch
            {
                404 => NotFoundEnvelope(),
                _ => ValidationErrorEnvelope(),
            };

        if (TryUnwrapRespons(type, out var dataType))
            return EnvelopeOkForDataType(dataType);

        return EnvelopeOkForDataType(type);
    }

    private static JsonObject NotFoundEnvelope() => new()
    {
        ["success"] = false,
        ["status_code"] = 404,
        ["detail"] = "Employee not found.",
        ["message"] = "Employee not found.",
        ["error"] = "Employee not found.",
    };

    private static JsonObject EnvelopeOkForDataType(Type dataType)
    {
        if (TryGetCollectionElementType(dataType, out var elementType))
        {
            return elementType.Name switch
            {
                nameof(FileUploadMultipleReadDto) => FileUploadMultipleResponse(),
                nameof(FileResponseReadDto) => FileListResponse(),
                _ when elementType == typeof(string) => EnvelopeOk(new JsonArray("employee", "department")),
                _ => EnvelopeOk(BuildCollectionData(elementType)),
            };
        }

        return dataType.Name switch
        {
            nameof(FileDeleteReadDto) => FileDeleteResponse(),
            nameof(FileResponseReadDto) => FileUpdateResponse(),
            nameof(FileUploadMultipleReadDto) => EnvelopeOk(FileUploadItem()),
            nameof(EmployeeAggregateReadDto) => EmployeeAggregateReadResponse(),
            nameof(CreateEmployeeControllerReadDto) => EnvelopeOk(CreateEmployeeReadData()),
            nameof(EmployeeListDto) => EnvelopeOk(EmployeeListData(), SamplePagination()),
            nameof(CustomFieldSchemaDto) => EnvelopeOk(CustomFieldSchemaData()),
            nameof(CustomFieldDefinitionListDto) => EnvelopeOk(CustomFieldDefinitionListData(), SamplePagination()),
            nameof(CustomFieldsSummaryDto) => EnvelopeOk(CustomFieldsSummaryData()),
            nameof(EmployeeBulkImportResult) => EnvelopeOk(BulkImportData()),
            nameof(OrganisationSummaryDto) => EnvelopeOk(OrganisationSummaryData()),
            _ when dataType == typeof(string) => EnvelopeOk(JsonValue.Create("Operation completed successfully.")),
            _ when dataType == typeof(object) => EnvelopeOk(new JsonObject()),
            _ => EnvelopeOk(new JsonObject()),
        };
    }

    private static bool TryUnwrapRespons(Type type, out Type dataType)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Respons<>))
        {
            dataType = type.GetGenericArguments()[0];
            return true;
        }

        dataType = type;
        return false;
    }

    private static bool TryGetCollectionElementType(Type type, out Type elementType)
    {
        if (type.IsArray)
        {
            elementType = type.GetElementType()!;
            return true;
        }

        if (!type.IsGenericType)
        {
            elementType = type;
            return false;
        }

        var def = type.GetGenericTypeDefinition();
        if (def == typeof(IReadOnlyList<>) || def == typeof(IEnumerable<>) || def == typeof(List<>)
            || def == typeof(ICollection<>) || def == typeof(IList<>))
        {
            elementType = type.GetGenericArguments()[0];
            return true;
        }

        elementType = type;
        return false;
    }

    private static JsonArray BuildCollectionData(Type elementType) => elementType.Name switch
    {
        nameof(CustomFieldDefinitionDto) => new JsonArray(CustomFieldDefinitionItem()),
        _ => new JsonArray(),
    };

    internal static JsonObject FileDeleteData() => new()
    {
        ["blob_path"] = SampleBlobPathSingle,
        ["container_name"] = SampleDocumentsContainer,
        ["message"] = "File deleted successfully.",
    };

    internal static JsonObject FileResponseData() => new()
    {
        ["id"] = SampleDocumentId1,
        ["presigned_url"] = SamplePresignedUrl,
        ["description"] = "Employment contract",
        ["file_name"] = "contract.pdf",
    };

    private static JsonObject FileUploadItem() => new() { ["id"] = SampleDocumentId1 };

    private static JsonObject CreateEmployeeReadData() => new()
    {
        ["employee_id"] = SampleEmployeeId.ToString(),
        ["employee_code"] = "EMP-000042",
        ["first_name"] = "Ada",
        ["middle_name"] = "",
        ["last_name"] = "Lovelace",
        ["lifecycle_state"] = "active",
    };

    private static JsonObject EmployeeListData() => new()
    {
        ["items"] = new JsonArray(new JsonObject
        {
            ["employee_id"] = SampleEmployeeId.ToString(),
            ["employee_code"] = "EMP-000042",
            ["full_name"] = "Ada Lovelace",
            ["job_title"] = "Software Engineer",
            ["department_name"] = "Engineering",
            ["branch_name"] = "Accra HQ",
            ["work_location"] = "Accra HQ",
            ["lifecycle_state"] = "active",
            ["employment_status"] = "Active",
            ["employment_type"] = "Full-time",
            ["profile_url"] = "https://storage.example.com/profiles/ada.jpg",
        }),
    };

    private static JsonObject CustomFieldSchemaData() => new()
    {
        ["entity_type"] = "employee",
        ["fields"] = new JsonArray(CustomFieldDefinitionItem()),
    };

    private static JsonObject CustomFieldDefinitionListData() => new()
    {
        ["summary"] = CustomFieldsSummaryData(),
        ["items"] = new JsonArray(CustomFieldDefinitionItem()),
    };

    private static JsonObject CustomFieldsSummaryData() => new()
    {
        ["total_definitions"] = 12,
        ["active_definitions"] = 10,
        ["deleted_definitions"] = 2,
    };

    private static JsonObject CustomFieldDefinitionItem() => new()
    {
        ["id"] = "cf_bonus_eligible_001",
        ["entity_type"] = "employee",
        ["field_key"] = "bonus_eligible",
        ["label"] = "Bonus eligible",
        ["description"] = "Whether the employee qualifies for annual bonus.",
        ["field_type"] = "select",
        ["is_required"] = false,
        ["is_sensitive"] = false,
        ["is_filterable"] = true,
        ["is_searchable"] = false,
        ["display_order"] = 1,
        ["section_name"] = "compensation",
        ["section_order"] = 1,
        ["options"] = "[\"yes\",\"no\"]",
        ["is_active"] = true,
        ["is_deleted"] = false,
        ["created_at"] = "2025-01-15T10:30:00+00:00",
        ["updated_at"] = "2025-01-15T10:30:00+00:00",
        ["created_by"] = "usr_admin_001",
        ["updated_by"] = "usr_admin_001",
    };

    private static JsonObject BulkImportData() => new()
    {
        ["rows"] = new JsonArray(new JsonObject
        {
            ["index"] = 1,
            ["success"] = true,
            ["employee_id"] = SampleEmployeeId.ToString(),
        }),
        ["success_count"] = 1,
        ["failure_count"] = 0,
    };

    private static JsonObject OrganisationSummaryData() => new()
    {
        ["department_count"] = 8,
        ["branch_count"] = 3,
        ["archived_count"] = 1,
    };

    internal static bool IsResponsType(Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Respons<>);

    internal static JsonObject CreateEmployeeFinalised() => new()
    {
        ["status"] = "finalised",
        ["identity"] = IdentitySection(withCustomField: true),
        ["employment"] = EmploymentSection(),
        ["compensation"] = CompensationSection(withCustomField: true),
        ["education"] = new JsonArray(EducationEntry()),
        ["certifications"] = new JsonArray(CertificationEntry()),
        ["document_ids"] = new JsonArray(SampleDocumentId1, SampleDocumentId2),
    };

    internal static JsonObject CreateEmployeeDraft() => new()
    {
        ["status"] = "draft",
        ["identity"] = new JsonObject
        {
            ["full_name"] = "Kwame Mensah",
            ["work_email"] = "kwame.mensah@company.com",
            ["custom_fields"] = EmptyCustomFields("identity"),
        },
        ["employment"] = new JsonObject
        {
            ["job_title"] = "HR Coordinator",
            ["custom_fields"] = EmptyCustomFields("employment"),
        },
        ["compensation"] = new JsonObject
        {
            ["gross_salary"] = 4500.00m,
            ["pay_frequency"] = "Monthly",
            ["currency_id"] = SampleCurrencyId,
            ["custom_fields"] = EmptyCustomFields("compensation"),
        },
        ["education"] = new JsonArray(),
        ["certifications"] = new JsonArray(),
        ["document_ids"] = new JsonArray(),
    };

    internal static JsonObject UpdateEmployeePartial() => new()
    {
        ["id"] = SampleEmployeeId.ToString(),
        ["status"] = "finalised",
        ["compensation"] = new JsonObject
        {
            ["gross_salary"] = 9200.00m,
            ["pay_frequency"] = "Monthly",
            ["currency_id"] = SampleCurrencyId,
            ["custom_fields"] = new JsonObject { ["bonus_eligible"] = "yes" },
        },
        ["document_ids"] = new JsonArray(SampleDocumentId1),
        ["delete_document_ids"] = new JsonArray(SampleDocumentId2),
    };

    internal static JsonObject CreateCustomFieldCompensation() => new()
    {
        ["entity_type"] = "employee",
        ["field_key"] = "bonus_eligible",
        ["label"] = "Bonus eligible",
        ["description"] = "Whether the employee qualifies for annual bonus.",
        ["field_type"] = "select",
        ["section_name"] = "compensation",
        ["section_order"] = 1,
        ["display_order"] = 1,
        ["options"] = "[\"yes\",\"no\"]",
        ["is_required"] = false,
        ["is_sensitive"] = false,
        ["is_filterable"] = true,
        ["is_searchable"] = false,
        ["is_active"] = true,
    };

    internal static JsonObject CreateCustomFieldIdentity() => new()
    {
        ["entity_type"] = "employee",
        ["field_key"] = "emergency_contact_name",
        ["label"] = "Emergency contact name",
        ["field_type"] = "text",
        ["section_name"] = "identity",
        ["display_order"] = 2,
        ["is_required"] = false,
        ["is_active"] = true,
    };

    internal static JsonObject EmployeeAggregateReadData() =>
        (JsonObject)EmployeeAggregateReadResponse()["data"]!;

    internal static JsonObject EmployeeAggregateReadResponse() => new()
    {
        ["success"] = true,
        ["status_code"] = 200,
        ["detail"] = "OK",
        ["message"] = "OK",
        ["data"] = new JsonObject
        {
            ["id"] = SampleEmployeeId.ToString(),
            ["employee_code"] = "EMP-000042",
            ["status"] = "finalised",
            ["is_draft"] = false,
            ["user_id"] = "usr_cp_abc123",
            ["profile_url"] = "https://storage.example.com/profiles/ada.jpg",
            ["identity"] = IdentitySection(withCustomField: true),
            ["employment"] = EmploymentSection(withNames: true),
            ["compensation"] = CompensationReadSection(),
            ["education"] = new JsonArray(EducationEntry(withId: true)),
            ["certifications"] = new JsonArray(CertificationEntry(withId: true)),
            ["document_ids"] = new JsonArray(SampleDocumentId1, SampleDocumentId2),
        },
    };

    internal static JsonObject CustomFieldsForSection(string section) => section switch
    {
        "compensation" => new JsonObject { ["bonus_eligible"] = "yes" },
        "identity" => new JsonObject { ["emergency_contact_name"] = "Charles Babbage" },
        "employment" => new JsonObject { ["desk_number"] = "B-204" },
        _ => new JsonObject(),
    };

    internal static JsonObject EmptyCustomFields(string section) => CustomFieldsForSection(section);

    internal static string CustomFieldsHelpText(string section) =>
        $"""
        Values for tenant-defined custom fields in the **{section}** section.
        1. Admin creates definitions: `POST /api/v1/custom-fields/add` with `section_name: "{section}"`.
        2. Frontend loads schema: `GET /api/v1/custom-fields/schema?entityType=employee`.
        3. Keys here must match `field_key` from definitions with `section_name = "{section}"`.
        Unknown keys are ignored. Use an empty object (no keys) when there are no values.
        """;

    private static JsonObject IdentitySection(bool withCustomField = false) => new()
    {
        ["full_name"] = "Ada Lovelace",
        ["date_of_birth"] = "1990-05-15",
        ["gender"] = "female",
        ["country"] = "Ghana",
        ["id_type"] = "Ghana Card",
        ["id_issue_date"] = "2020-01-10",
        ["id_expiry_date"] = "2030-01-10",
        ["id_number"] = "GHA-123456789-0",
        ["personal_email"] = "ada.personal@example.com",
        ["work_email"] = "ada.lovelace@company.com",
        ["phone"] = "+233201234567",
        ["linked_in_url"] = "https://linkedin.com/in/adalovelace",
        ["residential_address"] = "12 Independence Ave, Accra",
        ["custom_fields"] = withCustomField
            ? CustomFieldsForSection("identity")
            : EmptyCustomFields("identity"),
    };

    private static JsonObject EmploymentSection(bool withNames = false)
    {
        var obj = new JsonObject
        {
            ["job_title"] = "Software Engineer",
            ["department_id"] = SampleDepartmentId.ToString(),
            ["branch_id"] = SampleBranchId.ToString(),
            ["employment_type"] = "Full-time",
            ["employment_status"] = "Active",
            ["contract_type"] = "Permanent",
            ["work_arrangement"] = "hybrid",
            ["work_location"] = "Accra HQ",
            ["pay_grade"] = "P4",
            ["start_date"] = "2025-06-01",
            ["probation_end_date"] = "2025-12-01",
            ["working_hours"] = "40",
            ["notice_period"] = "30 days",
            ["reports_to_id"] = SampleReportsToId.ToString(),
            ["custom_fields"] = EmptyCustomFields("employment"),
        };

        if (withNames)
        {
            obj["department_name"] = "Engineering";
            obj["branch_name"] = "Accra HQ";
        }

        return obj;
    }

    private static JsonObject CompensationSection(bool withCustomField = false) => new()
    {
        ["gross_salary"] = 8500.00m,
        ["pay_frequency"] = "Monthly",
        ["currency_id"] = SampleCurrencyId,
        ["custom_fields"] = withCustomField
            ? CustomFieldsForSection("compensation")
            : EmptyCustomFields("compensation"),
    };

    private static JsonObject CompensationReadSection() => new()
    {
        ["gross_salary"] = 8500.00m,
        ["pay_frequency"] = "Monthly",
        ["currency_id"] = SampleCurrencyId,
        ["currency_code"] = "GHS",
        ["currency_name"] = "Ghana Cedi",
        ["currency_symbol"] = "₵",
        ["annualized_cost"] = 102000.00m,
        ["custom_fields"] = CustomFieldsForSection("compensation"),
    };

    private static JsonObject EducationEntry(bool withId = false)
    {
        var obj = new JsonObject
        {
            ["institution"] = "University of Ghana",
            ["degree"] = "BSc",
            ["field_of_study"] = "Computer Science",
            ["start_date"] = "2008-09-01",
            ["end_date"] = "2012-06-30",
            ["is_current"] = false,
            ["custom_fields"] = EmptyCustomFields("education"),
        };

        if (withId)
            obj["id"] = Guid.Parse("55555555-5555-5555-5555-555555555501").ToString();

        return obj;
    }

    private static JsonObject CertificationEntry(bool withId = false)
    {
        var obj = new JsonObject
        {
            ["name"] = "AWS Solutions Architect",
            ["issuing_body"] = "Amazon Web Services",
            ["issue_date"] = "2023-03-15",
            ["expiry_date"] = "2026-03-15",
            ["credential_url"] = "https://aws.amazon.com/verification/example-cert",
            ["custom_fields"] = EmptyCustomFields("certification"),
        };

        if (withId)
            obj["id"] = Guid.Parse("66666666-6666-6666-6666-666666666601").ToString();

        return obj;
    }
}
