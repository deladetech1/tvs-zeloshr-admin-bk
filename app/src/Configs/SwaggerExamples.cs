using System.Text.Json.Nodes;
using ZelosHR.Api.Entities.Branches;
using ZelosHR.Api.Entities.Currencies;
using ZelosHR.Api.Entities.CustomFields;
using ZelosHR.Api.Entities.Departments;
using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Entities.Files;
using ZelosHR.Api.Entities.OrgStructure;
using ZelosHR.Api.Entities.Shared;

namespace ZelosHR.Api.Configs;

/// <summary>Canonical OpenAPI request/response examples (snake_case wire format).</summary>
internal static class SwaggerExamples
{
    internal static readonly Guid SampleDepartmentId = Guid.Parse("823eb77c-11b7-452b-9c18-6f547a0cd003");
    internal static readonly Guid SampleChildDepartmentId = Guid.Parse("cc194e8e-b34d-42f6-baae-aae3b12041aa");
    internal static readonly Guid SampleBranchId = Guid.Parse("063e2b9a-9254-4154-89e7-98b8de5a4df5");
    internal static readonly Guid SampleBranchId2 = Guid.Parse("7f4e8291-2c55-4a9b-8d1e-5b6c7d8e9f0a");
    internal static readonly Guid SampleBranchId3 = Guid.Parse("5c3d2e1f-0a9b-8c7d-6e5f-4a3b2c1d0e9f");
    internal static readonly Guid SampleCustomFieldId = Guid.Parse("44444444-4444-4444-4444-444444444401");
    internal static readonly Guid SampleEmployeeId = Guid.Parse("3804deee-d6ee-4b05-9efc-6e8ccf3b5ae3");
    internal static readonly Guid SampleReportsToId = Guid.Parse("33333333-3333-3333-3333-333333333301");

    internal const string SampleCurrencyId = "cur_ghs_default";
    internal const string SampleDocumentId1 = "doc_contract_a1b2c3";
    internal const string SampleDocumentId2 = "doc_national_id_d4e5f6";
    internal const string SampleBlobPathSingle = "tenant_demo/org_demo/bus_demo/employees/documents/a1b2c3d4-contract.pdf";
    internal const string SampleBlobPathMulti1 = "tenant_demo/org_demo/bus_demo/employees/documents/a1b2c3d4-contract.pdf";
    internal const string SampleBlobPathMulti2 = "tenant_demo/org_demo/bus_demo/employees/documents/e5f6g7h8-national_id.jpg";
    internal const string SampleDocumentsContainer = "zeloshr";
    internal const string SamplePresignedUrl =
        "https://devstorage.blob.core.windows.net/zeloshr/tenant_demo/org_demo/bus_demo/employees/documents/a1b2c3d4-contract.pdf?sv=2024&se=2026-05-20T12%3A00%3A00Z&sig=example";

    internal static JsonObject FileUploadMultipleResponse() => EnvelopeOk(new JsonArray(
        new JsonObject { ["id"] = SampleDocumentId1 },
        new JsonObject { ["id"] = SampleDocumentId2 }));

    internal static JsonObject FileListResponse() => EnvelopeOk(new JsonArray(
        FileResponseData(),
        new JsonObject
        {
            ["id"] = SampleDocumentId2,
            ["presigned_url"] = SamplePresignedUrl,
            ["description"] = "National ID scan",
            ["file_name"] = "national_id.jpg",
        }));

    internal static JsonObject EmployeeDocumentItem(
        string? docId = null,
        string? description = null,
        string? name = null)
    {
        var item = new JsonObject
        {
            ["doc_id"] = docId ?? SampleDocumentId1,
            ["presigned_url"] = SamplePresignedUrl,
            ["description"] = description ?? "Employment contract",
            ["name"] = name ?? "contract.pdf",
        };
        return item;
    }

    internal static JsonArray EmployeeDocumentsArray() => new(
        EmployeeDocumentItem(SampleDocumentId1, "Employment contract", "contract.pdf"),
        EmployeeDocumentItem(SampleDocumentId2, "National ID scan", "national_id.jpg"));

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
            ["success"] = SwaggerExampleHints.EnvelopeSuccessPipe,
            ["status_code"] = SwaggerExampleHints.EnvelopeStatusCodePipe,
            ["detail"] = SwaggerExampleHints.EnvelopeDetailPipe,
            ["data"] = data,
        };

        if (pagination is not null)
            envelope["pagination"] = pagination;

        return envelope;
    }

    internal static void ApplyResponseEnvelopeHints(JsonObject envelope)
    {
        envelope["success"] = SwaggerExampleHints.EnvelopeSuccessPipe;
        envelope["status_code"] = SwaggerExampleHints.EnvelopeStatusCodePipe;
        envelope["detail"] = SwaggerExampleHints.EnvelopeDetailPipe;
    }

    internal static JsonObject BranchListResponseExample() =>
        EnvelopeOk(BranchListData(), OrgBranchListPagination());

    internal static JsonObject OrgBranchListPagination() => new()
    {
        ["page"] = 1,
        ["size"] = 20,
        ["total"] = 3,
        ["has_next"] = SwaggerExampleHints.BooleanPipe,
        ["page_size"] = 20,
        ["total_count"] = 3,
        ["total_pages"] = 1,
    };

    internal static JsonObject OrgDepartmentListPagination() => new()
    {
        ["page"] = 1,
        ["size"] = 15,
        ["total"] = 8,
        ["has_next"] = SwaggerExampleHints.BooleanPipe,
        ["page_size"] = 15,
        ["total_count"] = 8,
        ["total_pages"] = 1,
    };

    internal static JsonObject SamplePagination() => new()
    {
        ["page"] = 1,
        ["size"] = 20,
        ["total"] = 42,
        ["has_next"] = SwaggerExampleHints.BooleanPipe,
        ["page_size"] = 20,
        ["total_count"] = 42,
        ["total_pages"] = 3,
    };

    internal static JsonObject SampleFieldErrors() => new()
    {
        ["work_email"] = "Work email is already registered for another employee.",
        ["compensation.currency_id"] = "Currency not found for this tenant.",
    };

    internal static JsonObject ValidationErrorEnvelope(JsonObject? fieldErrors = null)
    {
        var errors = fieldErrors ?? SampleFieldErrors();
        var detail = errors.Count == 1
            ? errors.First().Value?.GetValue<string>() ?? "Validation failed."
            : $"Fix {errors.Count} validation errors: {string.Join(", ", errors.Select(e => e.Key))}.";

        return new JsonObject
        {
            ["success"] = "false",
            ["status_code"] = SwaggerExampleHints.EnvelopeStatusCodeErrorPipe,
            ["detail"] = detail,
            ["error"] = SwaggerExampleHints.EnvelopeDetailErrorPipe,
            ["field_errors"] = errors,
        };
    }

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
        ["success"] = "false",
        ["status_code"] = "404|409",
        ["detail"] = "Not found|Conflict",
        ["error"] = "Not found|Conflict",
    };

    internal static JsonObject NotFoundEnvelopeForEmployee() => NotFoundEnvelope();

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
            nameof(CustomFieldSectionsDto) => EnvelopeOk(CustomFieldSectionsData()),
            nameof(CustomFieldDefinitionListDto) => EnvelopeOk(CustomFieldDefinitionListData(), SamplePagination()),
            nameof(CustomFieldsSummaryDto) => EnvelopeOk(CustomFieldsSummaryData()),
            nameof(EmployeeBulkImportResult) => EnvelopeOk(BulkImportData()),
            nameof(ImportEmployeesResult) => EnvelopeOk(ImportEmployeesData()),
            nameof(EmployeeDirectorySummaryDto) => EmployeeDirectoryStatisticsResponse(),
            nameof(EmployeeRegistrationReadDto) => EmployeeRegistrationImportResponse(),
            nameof(OrganisationSummaryDto) => EnvelopeOk(OrganisationSummaryData()),
            nameof(OrgChartDto) => EnvelopeOk(OrgChartData()),
            nameof(DepartmentListDto) => EnvelopeOk(DepartmentListData(), OrgDepartmentListPagination()),
            nameof(BranchListDto) => BranchListResponseExample(),
            nameof(CreateDepartmentResponseDto) => EnvelopeOk(CreateDepartmentResponseData()),
            nameof(BranchMutationResponseDto) => EnvelopeOk(BranchMutationResponseData()),
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
        nameof(CustomFieldDefinitionDto) => CustomFieldDefinitionItemsAllSections(),
        nameof(CpUserDto) => new JsonArray(CpUserSearchItem()),
        nameof(GetCurrencySimpleReadDto) => new JsonArray(CurrencyItem()),
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
            ["profile_url"] = EmployeeDocumentItem(SampleDocumentId1, "Employee profile photo"),
        }),
    };

    private static JsonObject CustomFieldSchemaData() => new()
    {
        ["entity_type"] = CustomFieldEntityTypes.Employee,
        ["fields"] = CustomFieldDefinitionItemsAllSections(),
    };

    internal static JsonObject CustomFieldSectionsData() => new()
    {
        ["entity_type"] = CustomFieldEntityTypes.Employee,
        ["sections"] = new JsonArray(
            new JsonObject { ["value"] = EmployeeCustomFieldSections.Identity, ["label"] = "Identity" },
            new JsonObject { ["value"] = EmployeeCustomFieldSections.Employment, ["label"] = "Employment" },
            new JsonObject { ["value"] = EmployeeCustomFieldSections.Compensation, ["label"] = "Compensation" },
            new JsonObject { ["value"] = EmployeeCustomFieldSections.Education, ["label"] = "Education" },
            new JsonObject { ["value"] = EmployeeCustomFieldSections.Certification, ["label"] = "Certification" }),
    };

    private static JsonObject CustomFieldDefinitionListData() => new()
    {
        ["summary"] = CustomFieldsSummaryData(),
        ["items"] = CustomFieldDefinitionItemsAllSections(),
    };

    private static JsonObject CustomFieldsSummaryData() => new()
    {
        ["total_definitions"] = 12,
        ["active_definitions"] = 10,
        ["deleted_definitions"] = 2,
    };

    private static JsonObject CustomFieldDefinitionItem(string section, string fieldKey, string label) => new()
    {
        ["id"] = $"cf_{fieldKey}_001",
        ["entity_type"] = CustomFieldEntityTypes.Employee,
        ["field_key"] = fieldKey,
        ["label"] = label,
        ["description"] = $"Example custom field for section `{section}`.",
        ["field_type"] = "select",
        ["is_required"] = false,
        ["is_sensitive"] = false,
        ["is_filterable"] = true,
        ["is_searchable"] = false,
        ["display_order"] = 1,
        ["section_name"] = section,
        ["section_order"] = 1,
        ["options"] = "[\"yes\",\"no\"]",
        ["is_active"] = true,
        ["is_deleted"] = false,
        ["created_at"] = "2025-01-15T10:30:00+00:00",
        ["updated_at"] = "2025-01-15T10:30:00+00:00",
        ["created_by_id"] = "uid_admin_001",
        ["updated_by_id"] = "uid_admin_001",
        ["created_by"] = "Ada Lovelace",
        ["updated_by"] = "Ada Lovelace",
    };

    internal static JsonArray CustomFieldDefinitionItemsAllSections() => new JsonArray(
        CustomFieldDefinitionItem(EmployeeCustomFieldSections.Identity, "emergency_contact_name", "Emergency contact name"),
        CustomFieldDefinitionItem(EmployeeCustomFieldSections.Employment, "desk_number", "Desk number"),
        CustomFieldDefinitionItem(EmployeeCustomFieldSections.Compensation, "bonus_eligible", "Bonus eligible"),
        CustomFieldDefinitionItem(EmployeeCustomFieldSections.Education, "honors", "Honors"),
        CustomFieldDefinitionItem(EmployeeCustomFieldSections.Certification, "verified", "Verified"));

    private static JsonObject BulkImportData() => new()
    {
        ["rows"] = new JsonArray(
            new JsonObject
            {
                ["index"] = 1,
                ["success"] = true,
                ["employee_id"] = SampleEmployeeId.ToString(),
            },
            new JsonObject
            {
                ["index"] = 2,
                ["success"] = false,
                ["error"] = "Row 2: work_email is already registered for another employee.",
            }),
        ["success_count"] = 1,
        ["failure_count"] = 1,
    };

    internal static JsonObject EmployeeDirectorySummaryData() => new()
    {
        ["total_employees"] = 128,
        ["active_employees"] = 95,
        ["on_probation"] = 12,
        ["on_contract"] = 21,
    };

    internal static JsonObject EmployeeDirectoryStatisticsResponse() => EnvelopeOk(EmployeeDirectorySummaryData());

    internal static JsonObject EmployeeRegistrationReadData() => new()
    {
        ["id"] = SampleEmployeeId.ToString(),
        ["employee_code"] = "EMP-000042",
        ["full_name"] = "Ada Lovelace",
        ["user_id"] = "usr_cp_abc123",
        ["is_draft"] = true,
        ["lifecycle_status"] = "draft",
        ["job_title"] = "Software Engineer",
        ["department_id"] = SampleDepartmentId.ToString(),
        ["work_email"] = "ada.lovelace@company.com",
        ["annualized_cost"] = 102000.00m,
        ["currency_id"] = SampleCurrencyId,
        ["masked_ssnit_number"] = "****5678",
        ["masked_tin_number"] = "****9012",
    };

    internal static JsonObject EmployeeRegistrationImportResponse() => EnvelopeOk(EmployeeRegistrationReadData());

    internal static JsonObject ImportSearchResponse() => EnvelopeOk(new JsonArray(CpUserSearchItem()));

    internal static JsonObject ImportEmployeesRequestBody() => new()
    {
        ["user_ids"] = new JsonArray("usr_cp_abc123", "usr_cp_def456"),
    };

    internal static JsonObject ImportEmployeesData() => new()
    {
        ["items"] = new JsonArray(
            new JsonObject
            {
                ["user_id"] = "usr_cp_abc123",
                ["success"] = true,
                ["employee_id"] = SampleEmployeeId.ToString(),
                ["employee_code"] = "EMP-000042",
                ["full_name"] = "Ada Lovelace",
            },
            new JsonObject
            {
                ["user_id"] = "usr_cp_def456",
                ["success"] = false,
                ["error"] = "User is already linked to an employee.",
            }),
        ["success_count"] = 1,
        ["failure_count"] = 1,
    };

    internal static JsonObject ImportEmployeesResponse() => EnvelopeOk(ImportEmployeesData());

    internal static JsonArray CurrencyListData() => new JsonArray(CurrencyItem(), CurrencyItemUsd());

    internal static JsonObject CurrencyListResponse() => EnvelopeOk(CurrencyListData());

    internal static JsonObject CurrencyGetResponse() => EnvelopeOk(new JsonArray(CurrencyItem()));

    internal static JsonObject CurrencyNotFoundResponse() => new()
    {
        ["success"] = "false",
        ["status_code"] = "404",
        ["detail"] = "Currency not found.",
        ["error"] = "Currency not found.",
    };

    internal static JsonObject CurrencyItem() => new()
    {
        ["id"] = SampleCurrencyId,
        ["name"] = "Ghana Cedi",
        ["code"] = "GHS",
        ["symbol"] = "₵",
        ["decimal_places"] = 2,
        ["currency_position"] = "before",
        ["is_default"] = true,
    };

    private static JsonObject CurrencyItemUsd() => new()
    {
        ["id"] = "cur_usd_default",
        ["name"] = "US Dollar",
        ["code"] = "USD",
        ["symbol"] = "$",
        ["decimal_places"] = 2,
        ["currency_position"] = "before",
        ["is_default"] = false,
    };

    internal static JsonObject DeleteEmployeeSuccessResponse() => EnvelopeOk(new JsonObject());

    private static JsonObject CpUserSearchItem() => new()
    {
        ["id"] = "usr_cp_abc123",
        ["full_name"] = "Ada Lovelace",
        ["email"] = "ada.lovelace@company.com",
        ["phone"] = "+233201234567",
        ["is_active"] = true,
        ["gender"] = "female",
        ["dob"] = "1990-05-15",
        ["address"] = "12 Independence Ave, Accra",
        ["profile_pic"] = "https://storage.example.com/profiles/ada.jpg",
    };

    private static JsonObject OrganisationSummaryData() => new()
    {
        ["department_count"] = 8,
        ["branch_count"] = 3,
        ["archived_count"] = 1,
    };

    internal static JsonObject OrgChartResponse() => EnvelopeOk(OrgChartData());

    internal static JsonObject OrgChartEmptyResponse() => EnvelopeOk(new JsonObject
    {
        ["roots"] = new JsonArray(),
    });

    private static JsonObject OrgChartData() => new()
    {
        ["roots"] = new JsonArray(OrgChartRootNode()),
    };

    internal static JsonObject OrgChartDataForSchema() => OrgChartData();

    private static JsonObject OrgChartRootNode() => new()
    {
        ["id"] = SampleDepartmentId.ToString(),
        ["name"] = "Engineering",
        ["node_type"] = SwaggerExampleHints.OrgNodeType,
        ["parent_id"] = null,
        ["head_of_department"] = DepartmentHeadExample(),
        ["employee_count"] = 24,
        ["children"] = new JsonArray(new JsonObject
        {
            ["id"] = SampleChildDepartmentId.ToString(),
            ["name"] = "Platform",
            ["node_type"] = SwaggerExampleHints.OrgNodeType,
            ["parent_id"] = SampleDepartmentId.ToString(),
            ["head_of_department"] = null,
            ["employee_count"] = 12,
            ["children"] = new JsonArray(),
        }),
    };

    private static JsonObject DepartmentHeadExample() => new()
    {
        ["employee_id"] = SampleEmployeeId.ToString(),
        ["full_name"] = "Ada Lovelace",
        ["job_title"] = "Engineering Director",
        ["initials"] = "AL",
    };

    private static JsonObject DepartmentListData() => new()
    {
        ["summary"] = OrganisationSummaryData(),
        ["items"] = new JsonArray(
            new JsonObject
            {
                ["department_id"] = SampleDepartmentId.ToString(),
                ["name"] = "Engineering",
                ["parent_department_id"] = null,
                ["parent_department_name"] = null,
                ["head_of_department"] = DepartmentHeadExample(),
                ["employee_count"] = 24,
                ["is_archived"] = false,
                ["hierarchy_level"] = 0,
            },
            new JsonObject
            {
                ["department_id"] = SampleChildDepartmentId.ToString(),
                ["name"] = "Platform",
                ["parent_department_id"] = SampleDepartmentId.ToString(),
                ["parent_department_name"] = "Engineering",
                ["head_of_department"] = null,
                ["employee_count"] = 12,
                ["is_archived"] = SwaggerExampleHints.BooleanPipe,
                ["hierarchy_level"] = 1,
            }),
        ["showing_label"] = "Showing 2 of 8 departments",
    };

    internal static JsonObject BranchListItemExample() => new()
    {
        ["branch_id"] = SampleBranchId.ToString(),
        ["name"] = "Accra HQ",
        ["city"] = "Accra",
        ["region"] = "Greater Accra",
        ["country_code"] = "GH",
        ["employee_count"] = 24,
        ["is_archived"] = SwaggerExampleHints.BooleanPipe,
    };

    internal static JsonObject DepartmentListItemExample() => new()
    {
        ["department_id"] = SampleDepartmentId.ToString(),
        ["name"] = "Engineering",
        ["parent_department_id"] = null,
        ["parent_department_name"] = null,
        ["head_of_department"] = DepartmentHeadExample(),
        ["employee_count"] = 24,
        ["is_archived"] = SwaggerExampleHints.BooleanPipe,
        ["hierarchy_level"] = 0,
    };

    internal static JsonObject OrgChartNodeExample() => OrgChartRootNode();

    private static JsonObject BranchListData() => new()
    {
        ["items"] = new JsonArray(
            new JsonObject
            {
                ["branch_id"] = SampleBranchId.ToString(),
                ["name"] = "Accra HQ",
                ["city"] = "Accra",
                ["region"] = "Greater Accra",
                ["country_code"] = "GH",
                ["employee_count"] = 24,
                ["is_archived"] = false,
            },
            new JsonObject
            {
                ["branch_id"] = SampleBranchId2.ToString(),
                ["name"] = "Kumasi Office",
                ["employee_count"] = 18,
                ["is_archived"] = false,
            },
            new JsonObject
            {
                ["branch_id"] = SampleBranchId3.ToString(),
                ["name"] = "Tamale Office",
                ["employee_count"] = 9,
                ["is_archived"] = SwaggerExampleHints.BooleanPipe,
            }),
    };

    private static JsonObject CreateDepartmentResponseData() => new()
    {
        ["department_id"] = SampleDepartmentId.ToString(),
        ["name"] = "Engineering",
    };

    private static JsonObject BranchMutationResponseData() => new()
    {
        ["branch_id"] = SampleBranchId.ToString(),
        ["name"] = "Accra HQ",
        ["city"] = "Accra",
        ["region"] = "Greater Accra",
        ["country_code"] = "GH",
    };

    internal static JsonObject CreateDepartmentRoot() => new()
    {
        ["name"] = "Engineering",
        ["parent_department_id"] = null,
        ["head_of_department_id"] = SampleEmployeeId.ToString(),
    };

    internal static JsonObject CreateDepartmentChild() => new()
    {
        ["name"] = "Platform",
        ["parent_department_id"] = SampleDepartmentId.ToString(),
        ["head_of_department_id"] = null,
    };

    internal static JsonObject UpdateDepartmentBody() => new()
    {
        ["name"] = "Engineering & Product",
        ["parent_department_id"] = null,
        ["head_of_department_id"] = SampleEmployeeId.ToString(),
    };

    internal static JsonObject CreateBranchBody() => new()
    {
        ["name"] = "Accra HQ",
        ["city"] = "Accra",
        ["region"] = "Greater Accra",
        ["country_code"] = "GH",
    };

    internal static JsonObject UpdateBranchBody() => new()
    {
        ["name"] = "Accra Headquarters",
        ["city"] = "Accra",
        ["region"] = "Greater Accra",
        ["country_code"] = "GH",
    };

    internal static bool IsResponsType(Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Respons<>);

    internal static JsonObject CreateEmployeeFinalised() => new()
    {
        ["status"] = SwaggerExampleHints.Status,
        ["identity"] = IdentitySection(withCustomField: true, optionHints: true),
        ["employment"] = EmploymentSection(optionHints: true),
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
            ["full_name"] = "Ada Lovelace",
            ["phone"] = "+233201234567",
        },
        ["education"] = new JsonArray(),
        ["certifications"] = new JsonArray(),
        ["document_ids"] = new JsonArray(),
    };

    internal static JsonObject UpdateEmployeeFull()
    {
        var update = CreateEmployeeFinalised();
        update["education"] = new JsonArray(EducationEntry(withId: true));
        update["certifications"] = new JsonArray(CertificationEntry(withId: true));
        return update;
    }

    /// <inheritdoc cref="UpdateEmployeeFull"/>
    internal static JsonObject UpdateEmployeePartial() => UpdateEmployeeFull();

    internal static JsonObject UpdateCustomFieldBody() => new()
    {
        ["label"] = "Emergency contact name (updated)",
        ["is_required"] = true,
        ["placeholder"] = "Full name of emergency contact",
    };

    internal static JsonObject CreateCustomFieldAddBody() => new()
    {
        ["entity_type"] = SwaggerExampleHints.EntityType,
        ["field_key"] = "tier",
        ["label"] = "Compensation tier",
        ["description"] = "Optional help text shown to admins.",
        ["field_type"] = SwaggerExampleHints.FieldType,
        ["is_required"] = SwaggerExampleHints.BooleanPipe,
        ["is_sensitive"] = SwaggerExampleHints.BooleanPipe,
        ["is_filterable"] = SwaggerExampleHints.BooleanPipe,
        ["is_searchable"] = SwaggerExampleHints.BooleanPipe,
        ["display_order"] = 0,
        ["section_name"] = SwaggerExampleHints.SectionName,
        ["section_order"] = 0,
        ["options"] = SwaggerExampleHints.SelectOptionsPipe,
        ["validation_rules"] = null,
        ["default_value"] = "option_a",
        ["placeholder"] = "Select…",
        ["is_active"] = SwaggerExampleHints.BooleanPipe,
    };

    internal static JsonObject CreateCustomFieldDefinitionTemplate() => CreateCustomFieldAddBody();

    internal static string CreateCustomFieldAddExampleDescription() =>
        """
        One request creates **one** field definition. Pipe-separated values in the example (`employee|branch`, `text|select`, `true|false`) list allowed choices — **send one value** on real API calls.

        **Required:** `entity_type`, `field_key`, `label`, `field_type`
        **Required when field_type is select or multiselect:** `options` (JSON array string; pick one choice per slot)
        **Optional:** `description`, `is_required`, `is_sensitive`, `is_filterable`, `is_searchable`, `display_order`, `section_name` (from GET /custom-fields/sections?entity_type=), `section_order`, `validation_rules`, `default_value`, `placeholder`, `is_active`

        Omit `options` for text, number, date, boolean, email, phone, url.
        """;

    internal static JsonObject EmployeeAggregateReadData() =>
        (JsonObject)EmployeeAggregateReadResponse()["data"]!;

    internal static JsonObject EmployeeAggregateReadResponse()
    {
        var response = new JsonObject
        {
            ["data"] = new JsonObject
            {
                ["id"] = SampleEmployeeId.ToString(),
                ["employee_code"] = "EMP-000042",
                ["status"] = SwaggerExampleHints.Status,
                ["is_draft"] = SwaggerExampleHints.BooleanPipe,
                ["user_id"] = "usr_cp_abc123",
                ["identity"] = IdentitySection(withCustomField: true, forRead: true),
                ["employment"] = EmploymentSection(withNames: true),
                ["compensation"] = CompensationReadSection(),
                ["education"] = new JsonArray(EducationEntry(withId: true)),
                ["certifications"] = new JsonArray(CertificationEntry(withId: true)),
                ["documents"] = EmployeeDocumentsArray(),
            },
        };
        ApplyResponseEnvelopeHints(response);
        return response;
    }

    internal static JsonObject CustomFieldsForSection(string section) => section switch
    {
        EmployeeCustomFieldSections.Compensation => new JsonObject { ["bonus_eligible"] = "yes" },
        EmployeeCustomFieldSections.Identity => new JsonObject { ["emergency_contact_name"] = "Charles Babbage" },
        EmployeeCustomFieldSections.Employment => new JsonObject { ["desk_number"] = "B-204" },
        EmployeeCustomFieldSections.Education => new JsonObject { ["honors"] = "First Class" },
        EmployeeCustomFieldSections.Certification => new JsonObject { ["verified"] = "yes" },
        _ => new JsonObject(),
    };

    internal static JsonObject EmptyCustomFields(string section) => CustomFieldsForSection(section);

    internal static string CustomFieldsHelpText(string section) =>
        $"""
        Values for tenant-defined custom fields bound to section `{section}`.
        1. Admin creates definitions: `POST /api/v1/custom-fields/add` with `section_name` one of: {SwaggerExampleHints.SectionName}.
        2. Frontend loads schema: `GET /api/v1/custom-fields/schema?entityType=employee`.
        3. Keys here must match `field_key` from definitions with matching `section_name`.
        Unknown keys are ignored. Use an empty object when there are no values.
        """;

    private static JsonObject IdentitySection(bool withCustomField = false, bool optionHints = false, bool forRead = false) => new()
    {
        ["full_name"] = "Ada Lovelace",
        ["date_of_birth"] = "1990-05-15",
        ["gender"] = optionHints ? SwaggerExampleHints.Gender : "female",
        ["country"] = "Ghana",
        ["id_type"] = optionHints ? SwaggerExampleHints.IdType : "ghana_card",
        ["id_issue_date"] = "2020-01-10",
        ["id_expiry_date"] = "2030-01-10",
        ["id_number"] = "GHA-123456789-0",
        ["personal_email"] = "ada.personal@example.com",
        ["work_email"] = "ada.lovelace@company.com",
        ["phone"] = "+233201234567",
        ["linked_in_url"] = "https://linkedin.com/in/adalovelace",
        ["residential_address"] = "12 Independence Ave, Accra",
        ["profile_url"] = forRead
            ? EmployeeDocumentItem(SampleDocumentId1, "Employee profile photo")
            : SampleDocumentId1,
        ["custom_fields"] = withCustomField
            ? CustomFieldsForSection(EmployeeCustomFieldSections.Identity)
            : EmptyCustomFields(EmployeeCustomFieldSections.Identity),
    };

    private static JsonObject EmploymentSection(bool withNames = false, bool optionHints = false)
    {
        var obj = new JsonObject
        {
            ["job_title"] = "Software Engineer",
            ["department_id"] = SampleDepartmentId.ToString(),
            ["branch_id"] = SampleBranchId.ToString(),
            ["employment_type"] = optionHints ? SwaggerExampleHints.EmploymentType : "Full-time",
            ["employment_status"] = optionHints ? SwaggerExampleHints.EmploymentStatus : "Active",
            ["contract_type"] = optionHints ? SwaggerExampleHints.ContractType : "Permanent",
            ["work_arrangement"] = optionHints ? SwaggerExampleHints.WorkArrangement : "hybrid",
            ["work_location"] = "Accra HQ",
            ["pay_grade"] = "P4",
            ["start_date"] = "2025-06-01",
            ["probation_end_date"] = "2025-12-01",
            ["working_hours"] = "40",
            ["notice_period"] = "30 days",
            ["reports_to_id"] = SampleReportsToId.ToString(),
            ["custom_fields"] = EmptyCustomFields(EmployeeCustomFieldSections.Employment),
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
        ["pay_frequency"] = SwaggerExampleHints.PayFrequency,
        ["currency_id"] = SampleCurrencyId,
        ["custom_fields"] = withCustomField
            ? CustomFieldsForSection(EmployeeCustomFieldSections.Compensation)
            : EmptyCustomFields(EmployeeCustomFieldSections.Compensation),
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
        ["custom_fields"] = CustomFieldsForSection(EmployeeCustomFieldSections.Compensation),
    };

    internal static JsonObject EducationEntry(bool withId = false)
    {
        var obj = new JsonObject
        {
            ["institution"] = "University of Ghana",
            ["degree"] = "BSc",
            ["field_of_study"] = "Computer Science",
            ["start_date"] = "2008-09-01",
            ["end_date"] = "2012-06-30",
            ["is_current"] = false,
            ["custom_fields"] = EmptyCustomFields(EmployeeCustomFieldSections.Education),
        };

        if (withId)
            obj["id"] = Guid.Parse("55555555-5555-5555-5555-555555555501").ToString();

        return obj;
    }

    internal static JsonObject CertificationEntry(bool withId = false)
    {
        var obj = new JsonObject
        {
            ["name"] = "AWS Solutions Architect",
            ["issuing_body"] = "Amazon Web Services",
            ["issue_date"] = "2023-03-15",
            ["expiry_date"] = "2026-03-15",
            ["credential_url"] = "https://aws.amazon.com/verification/example-cert",
            ["custom_fields"] = EmptyCustomFields(EmployeeCustomFieldSections.Certification),
        };

        if (withId)
            obj["id"] = Guid.Parse("66666666-6666-6666-6666-666666666601").ToString();

        return obj;
    }
}
