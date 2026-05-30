using System.Text.Json.Nodes;
using Microsoft.OpenApi;

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

    internal static OpenApiObject FileUploadMultipleResponse() => EnvelopeOk(new JsonArray(
        new OpenApiObject { ["id"] = SampleDocumentId1 },
        new OpenApiObject { ["id"] = SampleDocumentId2 }));

    internal static OpenApiObject FileListResponse() => EnvelopeOk(new JsonArray(
        new OpenApiObject
        {
            ["id"] = SampleDocumentId1,
            ["presigned_url"] = SamplePresignedUrl,
            ["description"] = "Employment contract",
            ["file_name"] = "contract.pdf",
        },
        new OpenApiObject
        {
            ["id"] = SampleDocumentId2,
            ["presigned_url"] = SamplePresignedUrl,
            ["description"] = "National ID scan",
            ["file_name"] = "national_id.jpg",
        }));

    internal static OpenApiObject FileUpdateResponse() => EnvelopeOk(new OpenApiObject
    {
        ["id"] = SampleDocumentId1,
        ["presigned_url"] = SamplePresignedUrl,
        ["description"] = "Employment contract (revised)",
        ["file_name"] = "contract_v2.pdf",
    });

    internal static OpenApiObject FileDeleteResponse() => EnvelopeOk(new OpenApiObject
    {
        ["blob_path"] = SampleBlobPathSingle,
        ["container_name"] = SampleDocumentsContainer,
        ["message"] = "File deleted successfully.",
    });

    private static OpenApiObject EnvelopeOk(JsonNode data) => new()
    {
        ["success"] = true,
        ["status_code"] = 200,
        ["detail"] = "OK",
        ["data"] = data,
    };

    internal static OpenApiObject CreateEmployeeFinalised() => new()
    {
        ["status"] = "finalised",
        ["identity"] = IdentitySection(withCustomField: true),
        ["employment"] = EmploymentSection(),
        ["compensation"] = CompensationSection(withCustomField: true),
        ["education"] = new JsonArray(EducationEntry()),
        ["certifications"] = new JsonArray(CertificationEntry()),
        ["document_ids"] = new JsonArray(SampleDocumentId1, SampleDocumentId2),
    };

    internal static OpenApiObject CreateEmployeeDraft() => new()
    {
        ["status"] = "draft",
        ["identity"] = new OpenApiObject
        {
            ["full_name"] = "Kwame Mensah",
            ["work_email"] = "kwame.mensah@company.com",
            ["custom_fields"] = EmptyCustomFields("identity"),
        },
        ["employment"] = new OpenApiObject
        {
            ["job_title"] = "HR Coordinator",
            ["custom_fields"] = EmptyCustomFields("employment"),
        },
        ["compensation"] = new OpenApiObject
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

    internal static OpenApiObject UpdateEmployeePartial() => new()
    {
        ["id"] = SampleEmployeeId.ToString(),
        ["status"] = "finalised",
        ["compensation"] = new OpenApiObject
        {
            ["gross_salary"] = 9200.00m,
            ["pay_frequency"] = "Monthly",
            ["currency_id"] = SampleCurrencyId,
            ["custom_fields"] = new OpenApiObject { ["bonus_eligible"] = "yes" },
        },
        ["document_ids"] = new JsonArray(SampleDocumentId1),
        ["delete_document_ids"] = new JsonArray(SampleDocumentId2),
    };

    internal static OpenApiObject CreateCustomFieldCompensation() => new()
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

    internal static OpenApiObject CreateCustomFieldIdentity() => new()
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

    internal static OpenApiObject EmployeeAggregateReadData() =>
        (OpenApiObject)EmployeeAggregateReadResponse()["data"]!;

    internal static OpenApiObject EmployeeAggregateReadResponse() => new()
    {
        ["success"] = true,
        ["status_code"] = 200,
        ["detail"] = "OK",
        ["data"] = new OpenApiObject
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

    internal static OpenApiObject CustomFieldsForSection(string section) => section switch
    {
        "compensation" => new OpenApiObject { ["bonus_eligible"] = "yes" },
        "identity" => new OpenApiObject { ["emergency_contact_name"] = "Charles Babbage" },
        "employment" => new OpenApiObject { ["desk_number"] = "B-204" },
        _ => new OpenApiObject(),
    };

    internal static OpenApiObject EmptyCustomFields(string section) => CustomFieldsForSection(section);

    internal static string CustomFieldsHelpText(string section) =>
        $"""
        Values for tenant-defined custom fields in the **{section}** section.
        1. Admin creates definitions: `POST /api/v1/custom-fields/add` with `section_name: "{section}"`.
        2. Frontend loads schema: `GET /api/v1/custom-fields/schema?entityType=employee`.
        3. Keys here must match `field_key` from definitions with `section_name = "{section}"`.
        Unknown keys are ignored. Use `{{}}` when no values.
        """;

    private static OpenApiObject IdentitySection(bool withCustomField = false) => new()
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

    private static OpenApiObject EmploymentSection(bool withNames = false)
    {
        var obj = new OpenApiObject
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

    private static OpenApiObject CompensationSection(bool withCustomField = false) => new()
    {
        ["gross_salary"] = 8500.00m,
        ["pay_frequency"] = "Monthly",
        ["currency_id"] = SampleCurrencyId,
        ["custom_fields"] = withCustomField
            ? CustomFieldsForSection("compensation")
            : EmptyCustomFields("compensation"),
    };

    private static OpenApiObject CompensationReadSection() => new()
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

    private static OpenApiObject EducationEntry(bool withId = false)
    {
        var obj = new OpenApiObject
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

    private static OpenApiObject CertificationEntry(bool withId = false)
    {
        var obj = new OpenApiObject
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
