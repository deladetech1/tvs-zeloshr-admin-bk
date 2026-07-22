using System.Text.Json.Nodes;
using ZelosHR.Api.Entities.AuditLogs;
using ZelosHR.Api.Entities.Branches;
using ZelosHR.Api.Entities.CompanyInfo;
using ZelosHR.Api.Entities.CompanyLocalization;
using ZelosHR.Api.Entities.Currencies;
using ZelosHR.Api.Entities.Countries;
using ZelosHR.Api.Entities.CustomFields;
using ZelosHR.Api.Entities.Departments;
using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Entities.Employees.Authorization;
using ZelosHR.Api.Entities.EmploymentTypes;
using ZelosHR.Api.Entities.Files;
using ZelosHR.Api.Entities.IdCardTypes;
using ZelosHR.Api.Entities.Leave;
using ZelosHR.Api.Entities.OrgStructure;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Validation;

namespace ZelosHR.Api.Configs;

/// <summary>Canonical OpenAPI request/response examples (snake_case wire format).</summary>
internal static class SwaggerExamples
{
    internal static readonly Guid SampleDepartmentId = Guid.Parse("823eb77c-11b7-452b-9c18-6f547a0cd003");
    internal static readonly Guid SampleEmploymentTypeId = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
    internal static readonly Guid SampleChildDepartmentId = Guid.Parse("cc194e8e-b34d-42f6-baae-aae3b12041aa");
    internal static readonly Guid SampleBranchId = Guid.Parse("063e2b9a-9254-4154-89e7-98b8de5a4df5");
    internal static readonly Guid SampleBranchId2 = Guid.Parse("7f4e8291-2c55-4a9b-8d1e-5b6c7d8e9f0a");
    internal static readonly Guid SampleBranchId3 = Guid.Parse("5c3d2e1f-0a9b-8c7d-6e5f-4a3b2c1d0e9f");
    internal static readonly Guid SampleCustomFieldId = Guid.Parse("44444444-4444-4444-4444-444444444401");
    internal static readonly Guid SampleEmployeeId = Guid.Parse("3804deee-d6ee-4b05-9efc-6e8ccf3b5ae3");
    internal static readonly Guid SampleEducationRowId = Guid.Parse("55555555-5555-5555-5555-555555555501");
    internal static readonly Guid SampleCertificationRowId = Guid.Parse("66666666-6666-6666-6666-666666666601");
    internal static readonly Guid SampleIdentificationRowId = Guid.Parse("77777777-7777-7777-7777-777777777701");
    internal static readonly Guid SampleIdCardTypeId1 = Guid.Parse("753e2b9a-2322-4154-3456-98b8de5a4df5");
    internal static readonly Guid SampleIdCardTypeId2 = Guid.Parse("345e2b9a-2322-4154-3456-98b8de5a4df5");
    internal static readonly Guid SampleReportsToId = Guid.Parse("33333333-3333-3333-3333-333333333301");
    internal static readonly Guid SampleSecondaryReportsToId = Guid.Parse("33333333-3333-3333-3333-333333333302");
    internal static readonly Guid SampleAuditLogId = Guid.Parse("a1111111-1111-1111-1111-111111111101");
    internal static readonly Guid SampleLeaveRequestId = Guid.Parse("a2222222-2222-2222-2222-222222222201");
    internal static readonly Guid SampleLeaveBalanceId = Guid.Parse("a2222222-2222-2222-2222-222222222202");
    internal static readonly Guid SampleLeaveTypeId = Guid.Parse("a2222222-2222-2222-2222-222222222203");
    internal static readonly Guid SampleHolidayId = Guid.Parse("a2222222-2222-2222-2222-222222222204");
    internal static readonly Guid SampleChangeRequestId = Guid.Parse("b1111111-1111-1111-1111-111111111101");
    internal static readonly Guid SampleChangeRequestId2 = Guid.Parse("b1111111-1111-1111-1111-111111111102");
    internal static readonly Guid SampleCompanyProfileId = Guid.Parse("8f14e45f-ceea-4a3e-8c7f-1d8f6b9e2b41");
    internal static readonly Guid SampleOfficeId = Guid.Parse("b1000001-0000-4000-8000-000000000001");
    internal static readonly Guid SampleOfficeId2 = Guid.Parse("b1000001-0000-4000-8000-000000000002");
    internal static readonly Guid SampleCompanyLocalizationId = Guid.Parse("c1000001-0000-4000-8000-000000000001");

    internal const string SampleCurrencyId = "cur_ghs_default";
    internal const string SampleCountryId = "ctr_gh";
    internal const string SampleCountryName = "Ghana";
    internal const string SampleDocumentId1 = "doc_contract_a1b2c3";
    internal const string SampleDocumentId2 = "doc_national_id_d4e5f6";
    internal const string SampleBlobPathSingle = "tenant_demo/org_demo/bus_demo/employees/documents/a1b2c3d4-contract.pdf";
    internal const string SampleBlobPathMulti1 = "tenant_demo/org_demo/bus_demo/employees/documents/a1b2c3d4-contract.pdf";
    internal const string SampleBlobPathMulti2 = "tenant_demo/org_demo/bus_demo/employees/documents/e5f6g7h8-national_id.jpg";
    internal const string SampleDocumentsContainer = "employee-documents";
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

    private static JsonObject EnvelopeOk(JsonNode data, JsonObject? pagination = null, string? detail = null)
    {
        var envelope = new JsonObject
        {
            ["success"] = SwaggerExampleHints.EnvelopeSuccessPipe,
            ["status_code"] = SwaggerExampleHints.EnvelopeStatusCodePipe,
            ["detail"] = detail ?? SwaggerExampleHints.EnvelopeDetailPipe,
            ["data"] = data,
        };

        if (pagination is not null)
            envelope["pagination"] = pagination;

        return envelope;
    }

    internal static JsonObject EnvelopeOkConcrete(JsonNode data, JsonObject? pagination = null)
    {
        var envelope = new JsonObject
        {
            ["success"] = true,
            ["status_code"] = 200,
            ["detail"] = "Success",
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
                nameof(ChangeRequestReadDto) => ChangeRequestListResponse(),
                nameof(FieldPolicyEntryDto) => FieldPolicyListResponse(),
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
            nameof(EmployeeBulkImportResult) => EnvelopeOk(BulkImportData(successCount: 2, failureCount: 0)),
            nameof(ImportEmployeesResult) => EnvelopeOk(ImportEmployeesData()),
            nameof(EmployeeDirectorySummaryDto) => EmployeeDirectoryStatisticsResponse(),
            nameof(EmployeeRegistrationReadDto) => EmployeeRegistrationImportResponse(),
            nameof(OrganisationSummaryDto) => EnvelopeOk(OrganisationSummaryData()),
            nameof(OrgChartDto) => EnvelopeOkConcrete(OrgChartData()),
            nameof(DepartmentListDto) => EnvelopeOk(DepartmentListData(), OrgDepartmentListPagination()),
            nameof(BranchListDto) => BranchListResponseExample(),
            nameof(CreateDepartmentResponseDto) => EnvelopeOk(CreateDepartmentResponseData()),
            nameof(BranchMutationResponseDto) => EnvelopeOk(BranchMutationResponseData()),
            nameof(AuditLogSummaryDto) => EnvelopeOk(AuditLogSummaryData()),
            nameof(AuditLogListDto) => AuditLogListResponse(),
            nameof(AuditLogListItemDto) => AuditLogGetResponse(),
            nameof(LeaveSummaryDto) => EnvelopeOk(LeaveSummaryData()),
            nameof(LeavePersonalSummaryDto) => EnvelopeOk(LeavePersonalSummaryData()),
            nameof(LeaveListDto) => LeaveListResponse(),
            nameof(LeaveApprovalListDto) => LeaveApprovalListResponse(),
            nameof(LeaveDashboardDto) => LeaveDashboardResponse(),
            nameof(LeaveCalendarDto) => LeaveCalendarResponse(),
            nameof(LeaveMyRequestListDto) => LeaveMyRequestListResponse(),
            nameof(LeaveRequestListItemDto) => EnvelopeOk(LeaveRequestItemData()),
            nameof(LeaveRequestDetailDto) => LeaveRequestGetResponse(),
            nameof(LeaveBalanceListDto) => LeaveBalanceListResponse(),
            nameof(LeaveBalanceListItemDto) => EnvelopeOk(LeaveBalanceItemData()),
            nameof(LeaveTypeListDto) => EnvelopeOk(LeaveTypeListData()),
            nameof(LeaveTypeListItemDto) => EnvelopeOk(LeaveTypeItemData()),
            nameof(EmploymentTypeListDto) => EnvelopeOk(EmploymentTypeListData()),
            nameof(EmploymentTypeListItemDto) => EnvelopeOk(EmploymentTypeItemData()),
            nameof(IdCardTypeListItemDto) => EnvelopeOk(IdCardTypeItemData()),
            nameof(CustomFieldDefinitionDto) => EnvelopeOk(CustomFieldDefinitionItem(
                EmployeeCustomFieldSections.Identity, "emergency_contact_name", "Emergency contact name")),
            nameof(CompanyInfoReadDto) => EnvelopeOk(CompanyInfoData()),
            nameof(CompanyOfficeReadDto) => EnvelopeOk(CompanyOfficeItemData()),
            nameof(CompanyLocalizationReadDto) => EnvelopeOk(CompanyLocalizationData()),
            nameof(PublicHolidayListDto) => LeaveHolidayListResponse(),
            nameof(PublicHolidayListItemDto) => EnvelopeOk(PublicHolidayItemData()),
            nameof(GetCountrySimpleReadDto) => EnvelopeOk(CountryItem()),
            nameof(ChangeRequestReadDto) => ChangeRequestGetResponse(),
            nameof(EmployeeSelfUpdateResultDto) => EmployeeSelfUpdateResultResponse(),
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
        nameof(GetCountrySimpleReadDto) => new JsonArray(CountryItem()),
        nameof(ChangeRequestReadDto) => new JsonArray(
            ChangeRequestReadDtoData(ChangeRequestStatuses.Pending),
            ChangeRequestReadDtoData(ChangeRequestStatuses.Approved, includeReview: true)),
        nameof(IdCardTypeListItemDto) => new JsonArray(IdCardTypeItemData()),
        nameof(FieldPolicyEntryDto) => FieldPolicySampleEntries(),
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
    };

    private static JsonObject EmployeeListData()
    {
        var item = new JsonObject
        {
            ["employee_id"] = SampleEmployeeId.ToString(),
            ["employee_code"] = "EMP-000042",
            ["full_name"] = "Ada Lovelace",
            ["job_title"] = "Software Engineer",
            ["department_name"] = "Engineering",
            ["branch_name"] = "Accra HQ",
            ["work_location"] = "Accra HQ",
            ["employment_status"] = "Active",
            ["engagement"] = "active",
            ["work_states"] = new JsonArray("probation"),
            ["employment_type"] = "Full-time",
            ["profile_url"] = EmployeeDocumentItem(SampleDocumentId1, "Employee profile photo"),
            ["is_line_manager"] = true,
            ["is_head_of_department"] = false,
        };
        AppendResourceAuditFields(item);
        return new JsonObject { ["items"] = new JsonArray(item) };
    }

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

    internal static JsonObject CustomFieldDefinitionItem(string section, string fieldKey, string label) => new()
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

    private static JsonObject BulkImportData(int successCount, int failureCount, bool allFailed = false) => new()
    {
        ["rows"] = allFailed
            ? new JsonArray(
                new JsonObject
                {
                    ["index"] = 0,
                    ["success"] = false,
                    ["error"] = EmployeeErrorMessages.WorkEmailLinkedToAnotherEmployee,
                })
            : new JsonArray(
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
        ["success_count"] = successCount,
        ["failure_count"] = failureCount,
    };

    internal static JsonObject BatchImportAllSucceededEnvelope() =>
        EnvelopeOkConcrete(BulkImportData(successCount: 2, failureCount: 0));

    internal static JsonObject BatchImportPartialEnvelope() => new()
    {
        ["success"] = false,
        ["status_code"] = 207,
        ["detail"] = "Row 2: work_email is already registered for another employee.",
        ["data"] = BulkImportData(successCount: 1, failureCount: 1),
    };

    internal static JsonObject BatchImportAllFailedEnvelope() => new()
    {
        ["success"] = false,
        ["status_code"] = 422,
        ["detail"] = EmployeeErrorMessages.WorkEmailLinkedToAnotherEmployee,
        ["data"] = BulkImportData(successCount: 0, failureCount: 3, allFailed: true),
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

    internal static JsonObject ImportEmployeesData(
        int successCount = 1,
        int failureCount = 1,
        bool allFailed = false) => new()
    {
        ["items"] = allFailed
            ? new JsonArray(
                new JsonObject
                {
                    ["user_id"] = "usr_cp_def456",
                    ["success"] = false,
                    ["error"] = "User is already linked to an employee.",
                })
            : new JsonArray(
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
        ["success_count"] = successCount,
        ["failure_count"] = failureCount,
    };

    internal static JsonObject ImportEmployeesResponse() => EnvelopeOk(ImportEmployeesData());

    internal static JsonObject ImportEmployeesAllSucceededResponse() =>
        EnvelopeOkConcrete(ImportEmployeesData(successCount: 2, failureCount: 0));

    internal static JsonObject ImportEmployeesPartialResponse() => new()
    {
        ["success"] = false,
        ["status_code"] = 207,
        ["detail"] = EmployeeErrorMessages.UserAlreadyLinkedToEmployee,
        ["data"] = ImportEmployeesData(successCount: 1, failureCount: 1),
    };

    internal static JsonObject ImportEmployeesAllFailedResponse() => new()
    {
        ["success"] = false,
        ["status_code"] = 422,
        ["detail"] = EmployeeErrorMessages.UserAlreadyLinkedToEmployee,
        ["data"] = ImportEmployeesData(successCount: 0, failureCount: 1, allFailed: true),
    };

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

    internal static JsonObject OrgChartResponse() => EnvelopeOkConcrete(OrgChartData());

    internal static JsonObject OrgChartEmptyResponse() => EnvelopeOkConcrete(new JsonObject
    {
        ["roots"] = new JsonArray(),
    });

    private static JsonObject OrgChartData() => new()
    {
        ["roots"] = new JsonArray(OrgChartRootNode()),
    };

    internal static JsonObject OrgChartDataForSchema() => OrgChartData();

    private static JsonObject OrgChartProfileUrlExample() =>
        EmployeeDocumentItem(SampleDocumentId1, "Employee profile photo", "profile.jpg");

    private static JsonObject OrgChartRootNode() => new()
    {
        ["id"] = "11111111-1111-1111-1111-111111111101",
        ["full_name"] = "Kwame Asante",
        ["job_title"] = "Chief Executive Officer",
        ["profile_url"] = OrgChartProfileUrlExample(),
        ["node_type"] = "employee",
        ["parent_id"] = null,
        ["secondary_reports_to_id"] = null,
        ["department"] = null,
        ["children"] = new JsonArray(
            OrgChartManagerNode(
                "11111111-1111-1111-1111-111111111102",
                "11111111-1111-1111-1111-111111111101",
                "Kwame Boateng",
                "Chief Technology Officer",
                OrgChartProfileUrlExample(),
                SampleDepartmentId,
                "Engineering",
                8,
                10,
                new JsonArray(
                    OrgChartEmployeeNode("11111111-1111-1111-1111-111111111105", "11111111-1111-1111-1111-111111111102", "Kofi Asante", "Software Engineer", null, "11111111-1111-1111-1111-111111111101"),
                    OrgChartEmployeeNode("11111111-1111-1111-1111-111111111106", "11111111-1111-1111-1111-111111111102", "Abena Mensah", "Senior product designer", null))),
            OrgChartManagerNode(
                "11111111-1111-1111-1111-111111111103",
                "11111111-1111-1111-1111-111111111101",
                "Yaw Mensah",
                "Chief Financial Officer",
                OrgChartProfileUrlExample(),
                SampleChildDepartmentId,
                "Finance",
                4,
                10,
                new JsonArray(
                    OrgChartEmployeeNode("11111111-1111-1111-1111-111111111107", "11111111-1111-1111-1111-111111111103", "Yaw Ofori", "Financial Analyst", null),
                    OrgChartEmployeeNode("11111111-1111-1111-1111-111111111108", "11111111-1111-1111-1111-111111111103", "Maame Bonsu", "Accountant", null))),
            OrgChartManagerNode(
                "11111111-1111-1111-1111-111111111104",
                "11111111-1111-1111-1111-111111111101",
                "Ama Darko",
                "Chief Operating Officer",
                OrgChartProfileUrlExample(),
                SampleBranchId,
                "Operations",
                6,
                10,
                new JsonArray(
                    OrgChartEmployeeNode("11111111-1111-1111-1111-111111111109", "11111111-1111-1111-1111-111111111104", "Efua Boateng", "Operations Manager", null),
                    OrgChartEmployeeNode("11111111-1111-1111-1111-111111111110", "11111111-1111-1111-1111-111111111104", "Nana Adjei", "Human Resources", null)))),
    };

    private static JsonObject OrgChartManagerNode(
        string id,
        string parentId,
        string fullName,
        string jobTitle,
        JsonNode? profileUrl,
        Guid departmentId,
        string departmentName,
        int employeeCount,
        int headcountCapacity,
        JsonArray children) => new()
    {
        ["id"] = id,
        ["full_name"] = fullName,
        ["job_title"] = jobTitle,
        ["profile_url"] = profileUrl,
        ["node_type"] = "employee",
        ["parent_id"] = parentId,
        ["secondary_reports_to_id"] = null,
        ["department"] = new JsonObject
        {
            ["department_id"] = departmentId.ToString(),
            ["name"] = departmentName,
            ["employee_count"] = employeeCount,
            ["headcount_capacity"] = headcountCapacity,
        },
        ["children"] = children,
    };

    private static JsonObject OrgChartEmployeeNode(
        string id,
        string parentId,
        string fullName,
        string jobTitle,
        JsonNode? profileUrl,
        string? secondaryReportsToId = null) => new()
    {
        ["id"] = id,
        ["full_name"] = fullName,
        ["job_title"] = jobTitle,
        ["profile_url"] = profileUrl,
        ["node_type"] = "employee",
        ["parent_id"] = parentId,
        ["secondary_reports_to_id"] = secondaryReportsToId,
        ["department"] = null,
        ["children"] = new JsonArray(),
    };

    private static JsonObject DepartmentHeadExample() => new()
    {
        ["employee_id"] = SampleEmployeeId.ToString(),
        ["full_name"] = "Ada Lovelace",
        ["job_title"] = "Engineering Director",
        ["profile_url"] = EmployeeDocumentItem(SampleDocumentId1, "Employee profile photo"),
    };

    private static JsonObject DepartmentListData() => new()
    {
        ["summary"] = OrganisationSummaryData(),
        ["items"] = new JsonArray(
            new JsonObject
            {
                ["department_id"] = SampleDepartmentId.ToString(),
                ["name"] = "Engineering",
                ["description"] = "Product engineering and platform",
                ["parent_department_id"] = null,
                ["parent_department_name"] = null,
                ["head_of_department"] = DepartmentHeadExample(),
                ["employee_count"] = 24,
                ["headcount_capacity"] = 30,
                ["is_archived"] = false,
                ["hierarchy_level"] = 0,
                ["created_at"] = "2025-01-15T10:30:00+00:00",
                ["updated_at"] = "2025-06-01T14:00:00+00:00",
                ["created_by_id"] = "uid_sample_user",
                ["updated_by_id"] = "uid_sample_user",
                ["created_by"] = "Larry Ntori",
                ["updated_by"] = "Larry Ntori",
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
                ["created_at"] = "2025-01-15T10:30:00+00:00",
                ["updated_at"] = "2025-06-01T14:00:00+00:00",
                ["created_by_id"] = "uid_sample_user",
                ["updated_by_id"] = "uid_sample_user",
                ["created_by"] = "Larry Ntori",
                ["updated_by"] = "Larry Ntori",
            }),
        ["showing_label"] = "Showing 2 of 8 departments",
    };

    internal static JsonObject BranchListItemExample()
    {
        var item = new JsonObject
        {
            ["branch_id"] = SampleBranchId.ToString(),
            ["name"] = "Accra HQ",
            ["address"] = "Greater Accra, 4th Avenue 128B Greda Estate, Teshie-Nungua",
            ["country"] = "Ghana",
            ["description"] = null,
            ["employee_count"] = 24,
            ["is_archived"] = false,
        };
        AppendResourceAuditFields(item);
        return item;
    }

    internal static JsonObject DepartmentListItemExample() => new()
    {
        ["department_id"] = SampleDepartmentId.ToString(),
        ["name"] = "Engineering",
        ["description"] = "Product engineering and platform",
        ["parent_department_id"] = null,
        ["parent_department_name"] = null,
        ["head_of_department"] = DepartmentHeadExample(),
        ["employee_count"] = 24,
        ["is_archived"] = SwaggerExampleHints.BooleanPipe,
        ["hierarchy_level"] = 0,
        ["created_at"] = "2025-01-15T10:30:00+00:00",
        ["updated_at"] = "2025-06-01T14:00:00+00:00",
        ["created_by_id"] = "uid_sample_user",
        ["updated_by_id"] = "uid_sample_user",
        ["created_by"] = "Larry Ntori",
        ["updated_by"] = "Larry Ntori",
    };

    internal static JsonObject OrgChartNodeExample() => OrgChartRootNode();

    private static JsonObject BranchListData() => new()
    {
        ["items"] = new JsonArray(
            new JsonObject
            {
                ["branch_id"] = SampleBranchId.ToString(),
                ["name"] = "Accra HQ",
                ["address"] = "Greater Accra, 4th Avenue 128B Greda Estate, Teshie-Nungua",
                ["country"] = "Ghana",
                ["description"] = null,
                ["employee_count"] = 24,
                ["is_archived"] = false,
                ["created_at"] = "2025-01-15T10:30:00+00:00",
                ["updated_at"] = "2025-06-01T14:00:00+00:00",
                ["created_by_id"] = "uid_sample_user",
                ["updated_by_id"] = "uid_sample_user",
                ["created_by"] = "Larry Ntori",
                ["updated_by"] = "Larry Ntori",
            },
            new JsonObject
            {
                ["branch_id"] = SampleBranchId2.ToString(),
                ["name"] = "London Office",
                ["address"] = "1 Canada Square, Canary Wharf",
                ["country"] = "United Kingdom",
                ["description"] = null,
                ["employee_count"] = 18,
                ["is_archived"] = false,
                ["created_at"] = "2025-01-15T10:30:00+00:00",
                ["updated_at"] = "2025-06-01T14:00:00+00:00",
                ["created_by_id"] = "uid_sample_user",
                ["updated_by_id"] = "uid_sample_user",
                ["created_by"] = "Larry Ntori",
                ["updated_by"] = "Larry Ntori",
            },
            new JsonObject
            {
                ["branch_id"] = SampleBranchId3.ToString(),
                ["name"] = "Nairobi Office",
                ["address"] = "Westlands Business Park",
                ["country"] = "Kenya",
                ["description"] = null,
                ["employee_count"] = 9,
                ["is_archived"] = false,
                ["created_at"] = "2025-01-15T10:30:00+00:00",
                ["updated_at"] = "2025-06-01T14:00:00+00:00",
                ["created_by_id"] = "uid_sample_user",
                ["updated_by_id"] = "uid_sample_user",
                ["created_by"] = "Larry Ntori",
                ["updated_by"] = "Larry Ntori",
            }),
    };

    private static JsonObject CreateDepartmentResponseData() => new()
    {
        ["department_id"] = SampleDepartmentId.ToString(),
        ["name"] = "Engineering",
        ["description"] = "Product engineering and platform",
        ["head_of_department"] = DepartmentHeadExample(),
        ["created_at"] = "2025-01-15T10:30:00+00:00",
        ["updated_at"] = "2025-06-01T14:00:00+00:00",
        ["created_by_id"] = "uid_sample_user",
        ["updated_by_id"] = "uid_sample_user",
        ["created_by"] = "Larry Ntori",
        ["updated_by"] = "Larry Ntori",
    };

    private static JsonObject BranchMutationResponseData()
    {
        var data = new JsonObject
        {
            ["branch_id"] = SampleBranchId.ToString(),
            ["name"] = "Accra HQ",
            ["address"] = "Greater Accra, 4th Avenue 128B Greda Estate, Teshie-Nungua",
            ["country"] = "Ghana",
            ["description"] = null,
        };
        AppendResourceAuditFields(data);
        return data;
    }

    internal static JsonObject CreateDepartmentRoot() => new()
    {
        ["name"] = "Engineering",
        ["parent_department_id"] = null,
        ["head_of_department_id"] = SampleEmployeeId.ToString(),
        ["head_of_department"] = new JsonObject
        {
            ["employee_id"] = SampleEmployeeId.ToString(),
        },
        ["description"] = "Product engineering and platform",
        ["headcount_capacity"] = 10,
    };

    internal static JsonObject CreateDepartmentChild() => new()
    {
        ["name"] = "Platform",
        ["parent_department_id"] = SampleDepartmentId.ToString(),
        ["head_of_department_id"] = null,
        ["description"] = null,
        ["headcount_capacity"] = null,
    };

    internal static JsonObject UpdateDepartmentBody() => new()
    {
        ["name"] = "Engineering & Product",
        ["parent_department_id"] = null,
        ["head_of_department_id"] = SampleEmployeeId.ToString(),
        ["description"] = "Product engineering and platform teams.",
        ["headcount_capacity"] = 30,
    };

    internal static JsonObject CreateBranchBody() => new()
    {
        ["name"] = "Accra HQ",
        ["address"] = "Greater Accra, 4th Avenue 128B Greda Estate, Teshie-Nungua",
        ["country"] = "Ghana",
        ["description"] = null,
    };

    internal static JsonObject UpdateBranchBody() => new()
    {
        ["name"] = "Accra Headquarters",
        ["address"] = "Greater Accra, 4th Avenue 128B Greda Estate, Teshie-Nungua",
        ["country"] = "Ghana",
        ["description"] = "Main office for Ghana operations.",
    };

    internal static bool IsResponsType(Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Respons<>);

    /// <summary>Canonical frontend contract for <c>POST /employees/add</c> and full-profile <c>PUT</c>.</summary>
    internal static JsonObject CreateEmployeeFinalised() => new()
    {
        ["identity"] = new JsonObject
        {
            ["full_name"] = "Ada Lovelace",
            ["date_of_birth"] = "1990-05-15",
            ["gender"] = "female",
            ["country"] = "Ghana",
            ["personal_email"] = "ada.personal@example.com",
            ["work_email"] = "ada.lovelace@company.com",
            ["phone"] = "+233201234567",
            ["linked_in_url"] = "https://linkedin.com/in/adalovelace",
            ["residential_address"] = "12 Independence Ave, Accra",
            ["profile_url"] = SampleDocumentId1,
            ["marital_status"] = "Single",
            ["next_of_kin_name"] = "Gary Ntori",
            ["next_of_kin_phone"] = "+233240257669",
            ["relationship_to_next_of_kin"] = "Brother",
            ["emergency"] = new JsonArray(
                new JsonObject
                {
                    ["emergency_contact_name"] = "Bright",
                    ["emergency_contact_phone"] = "+233503448860",
                    ["relationship"] = "Friend",
                }),
            ["identifications"] = new JsonArray(
                new JsonObject
                {
                    ["id_card_type_id"] = SampleIdCardTypeId1.ToString(),
                    ["id_card_type_number"] = "GHA-123456789-0",
                    ["id_card_type_issue_date"] = "2020-01-10",
                    ["id_card_type_expiry_date"] = "2030-01-10",
                },
                new JsonObject
                {
                    ["id_card_type_id"] = SampleIdCardTypeId2.ToString(),
                    ["id_card_type_number"] = "GHA-23232-0",
                    ["id_card_type_issue_date"] = "2020-01-10",
                    ["id_card_type_expiry_date"] = "2030-01-10",
                }),
            ["custom_fields"] = new JsonObject { ["fathers_name"] = "Charles Babbage" },
        },
        ["employment"] = new JsonObject
        {
            ["job_title"] = "Software Engineer",
            ["department_id"] = SampleDepartmentId.ToString(),
            ["branch_id"] = SampleBranchId.ToString(),
            ["employment_status"] = "Active",
            ["contract_type"] = "Permanent",
            ["work_arrangement"] = "Hybrid",
            ["work_location"] = "Accra HQ",
            ["pay_grade"] = "P4",
            ["start_date"] = "2025-06-01",
            ["probation_end_date"] = "2025-12-01",
            ["working_hours"] = 40,
            ["notice_period"] = "30 days",
            ["employment_type_id"] = SampleEmploymentTypeId.ToString(),
            ["reports_to_id"] = SampleReportsToId.ToString(),
            ["secondary_reports_to_id"] = SampleSecondaryReportsToId.ToString(),
        },
        ["compensation"] = new JsonObject
        {
            ["gross_salary"] = 8500,
            ["net_salary"] = 5000,
            ["currency_id"] = SampleCurrencyId,
            ["pay_frequency"] = "Monthly",
            ["ssnit_insurance_number"] = "SSNIT123456789",
            ["payment"] = new JsonArray(
                new JsonObject
                {
                    ["payment_mode"] = "bank_transfer",
                    ["bank_name"] = "GCB Bank",
                    ["account_name"] = "Ada Lovelace",
                    ["account_number"] = "0123456789",
                    ["branch_name"] = "Accra Main Branch",
                    ["is_primary"] = true,
                }),
        },
        ["medical"] = MedicalWriteSection(),
        ["education"] = new JsonArray(EducationEntry(includeCustomFields: false)),
        ["certifications"] = new JsonArray(CertificationEntry(name: "Masters in React", includeCustomFields: false)),
        ["skills"] = new JsonArray(
            new JsonObject
            {
                ["name"] = "React",
                ["proficiency"] = "Advanced",
                ["years_of_experience"] = 5,
            },
            new JsonObject
            {
                ["name"] = "Node.js",
                ["proficiency"] = "Intermediate",
                ["years_of_experience"] = 3,
            }),
        ["experiences"] = new JsonArray(
            new JsonObject
            {
                ["company"] = "TechCorp Ghana",
                ["job_title"] = "Frontend Developer",
                ["employment_type"] = "Full-time",
                ["location"] = "Accra, Ghana",
                ["start_date"] = "2018-03-01",
                ["end_date"] = "2022-08-31",
                ["is_current"] = false,
                ["description"] = "Built and maintained customer-facing web applications using React and TypeScript.",
            },
            new JsonObject
            {
                ["company"] = "Fintech Solutions Ltd",
                ["job_title"] = "Software Engineer",
                ["employment_type"] = "Contract",
                ["location"] = "Remote",
                ["start_date"] = "2022-09-01",
                ["end_date"] = null,
                ["is_current"] = true,
                ["description"] = "Developing payment integration services and internal dashboards.",
            }),
        ["referrals"] = new JsonArray(
            new JsonObject
            {
                ["name"] = "Kwame Mensah",
                ["job_title"] = "Engineering Manager",
                ["company"] = "TechCorp Ghana",
                ["relationship"] = "Former Manager",
                ["email"] = "kwame.mensah@techcorp.com",
                ["phone"] = "+233244123456",
            },
            new JsonObject
            {
                ["name"] = "Abena Osei",
                ["job_title"] = "Senior Software Engineer",
                ["company"] = "Fintech Solutions Ltd",
                ["relationship"] = "Colleague",
                ["email"] = "abena.osei@fintechsolutions.com",
                ["phone"] = "+233209876543",
            }),
        ["document_ids"] = new JsonArray(SampleDocumentId1, SampleDocumentId2),
    };

    internal static JsonObject CreateEmployeeDraft() => new()
    {
        ["identity"] = new JsonObject
        {
            ["full_name"] = "Ada Lovelace",
            ["phone"] = "+233201234567",
        },
        ["education"] = new JsonArray(),
        ["certifications"] = new JsonArray(),
        ["document_ids"] = new JsonArray(),
    };

    internal static JsonObject UpdateEmployeeFull() => new()
    {
        ["identity"] = IdentitySection(withCustomField: true),
        ["employment"] = EmploymentSection(),
        ["compensation"] = CompensationSection(withCustomField: true),
        ["education"] = new JsonArray(
            EducationEntry(withId: true),
            EducationEntry(
                withId: true,
                id: Guid.Parse("55555555-5555-5555-5555-555555555502"),
                degree: "MSc",
                fieldOfStudy: "Software Engineering")),
        ["certifications"] = new JsonArray(
            CertificationEntry(withId: true),
            CertificationEntry(
                withId: true,
                id: Guid.Parse("66666666-6666-6666-6666-666666666602"),
                name: "Masters in react fundamentals")),
        ["document_ids"] = new JsonArray(SampleDocumentId1, SampleDocumentId2),
    };

    /// <summary>Partial update — employment section only (e.g. change type from list).</summary>
    internal static JsonObject UpdateEmployeePartialEmployment() => new()
    {
        ["employment"] = new JsonObject
        {
            ["employment_type_id"] = SampleEmploymentTypeId.ToString(),
        },
    };

    /// <summary>Update custom employment type — system defaults: description/is_active only (name locked).</summary>
    internal static JsonObject UpdateEmploymentTypeCustom() => new()
    {
        ["name"] = "Apprentice",
        ["description"] = "Structured on-the-job training programme",
        ["is_active"] = true,
    };

    /// <summary>Update system default — name change rejected; description allowed.</summary>
    internal static JsonObject UpdateEmploymentTypeSystemDefault() => new()
    {
        ["description"] = "Standard salaried employment",
    };

    /// <summary>Partial update — one section only (typical bulk save).</summary>
    internal static JsonObject UpdateEmployeePartialIdentity() => new()
    {
        ["identity"] = new JsonObject
        {
            ["phone"] = "+233201234567",
        },
    };

    /// <summary>Bulk edit education/certification rows using ids from GET.</summary>
    internal static JsonObject UpdateEmployeeEducationCertUpsert() => new()
    {
        ["education"] = new JsonArray(
            EducationEntry(withId: true, degree: "MSc", fieldOfStudy: "Computer Science")),
        ["certifications"] = new JsonArray(
            CertificationEntry(
                withId: true,
                name: "Masters in react fundamentals",
                issuingBody: "Udemy",
                issueDate: "2026-05-31",
                credentialUrl: "https://udemy.com/certificate/3424-3424",
                omitExpiryDate: true)),
    };

    /// <summary>Add new education/certification rows (omit id).</summary>
    internal static JsonObject UpdateEmployeeAddSubRows() => new()
    {
        ["education"] = new JsonArray(EducationEntry()),
        ["certifications"] = new JsonArray(CertificationEntry()),
    };

    /// <summary>No sync — update one certification row; other rows stay untouched.</summary>
    internal static JsonObject UpdateEmployeePatchOneCertification() => new()
    {
        ["certifications"] = new JsonArray(
            CertificationEntry(
                withId: true,
                name: "Masters in react (updated)",
                issuingBody: "Udemy",
                issueDate: "2026-05-31",
                credentialUrl: "https://udemy.com/certificate/3424-3424",
                omitExpiryDate: true)),
    };

    /// <summary>sync_certifications true — keep only rows in the array; delete all others.</summary>
    internal static JsonObject UpdateEmployeeSyncCertificationsReplace() => new()
    {
        ["sync_certifications"] = true,
        ["certifications"] = new JsonArray(
            CertificationEntry(
                withId: true,
                name: "Masters in react",
                issuingBody: "Udemy",
                issueDate: "2026-05-31",
                credentialUrl: "https://udemy.com/certificate/3424-3424",
                omitExpiryDate: true)),
    };

    /// <summary>sync_education true — replace entire education section.</summary>
    internal static JsonObject UpdateEmployeeSyncEducationReplace() => new()
    {
        ["sync_education"] = true,
        ["education"] = new JsonArray(EducationEntry(withId: true)),
    };

    /// <summary>Replace education / delete certification rows by id.</summary>
    internal static JsonObject UpdateEmployeeSyncAndDelete() => new()
    {
        ["sync_education"] = true,
        ["education"] = new JsonArray(EducationEntry(withId: true)),
        ["delete_certification_ids"] = new JsonArray(
            Guid.Parse("66666666-6666-6666-6666-666666666602").ToString()),
    };

    /// <summary>sync_identifications true — keep only rows in identity.identifications; delete all others.</summary>
    internal static JsonObject UpdateEmployeeSyncIdentificationsReplace() => new()
    {
        ["sync_identifications"] = true,
        ["identity"] = new JsonObject
        {
            ["identifications"] = new JsonArray(
                IdentificationEntry(withId: true)),
        },
    };

    /// <summary>Add new identification rows under identity (omit id).</summary>
    internal static JsonObject UpdateEmployeeAddIdentifications() => new()
    {
        ["identity"] = new JsonObject
        {
            ["identifications"] = new JsonArray(IdentificationEntry()),
        },
    };

    /// <inheritdoc cref="UpdateEmployeePartialIdentity"/>
    internal static JsonObject UpdateEmployeePartial() => UpdateEmployeePartialIdentity();

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
                ["user_id"] = "usr_cp_abc123",
                ["identity"] = IdentitySection(withCustomField: true, forRead: true, withExtendedIdentity: true),
                ["employment"] = EmploymentSection(withNames: true),
                ["compensation"] = CompensationReadSection(),
                ["education"] = new JsonArray(
                    EducationEntry(withId: true),
                    EducationEntry(
                        withId: true,
                        id: Guid.Parse("55555555-5555-5555-5555-555555555502"),
                        degree: "MSc",
                        fieldOfStudy: "Software Engineering")),
                ["certifications"] = new JsonArray(
                    CertificationEntry(withId: true),
                    CertificationEntry(
                        withId: true,
                        id: Guid.Parse("66666666-6666-6666-6666-666666666602"),
                        name: "Masters in react fundamentals",
                        credentialUrl: "https://udemy.com/certificate/3424-3424")),
                ["medical"] = MedicalReadSection(),
                ["skills"] = new JsonArray(SkillEntry(withId: true)),
                ["experiences"] = new JsonArray(ExperienceEntry(withId: true)),
                ["referrals"] = new JsonArray(ReferralEntry(withId: true)),
                ["documents"] = EmployeeDocumentsArray(),
            },
        };
        AppendResourceAuditFields((JsonObject)response["data"]!);
        ApplyResponseEnvelopeHints(response);
        return response;
    }

    internal static JsonObject CustomFieldsForSection(string section) => section switch
    {
        EmployeeCustomFieldSections.Compensation => new JsonObject { ["bonus_eligible"] = "yes" },
        EmployeeCustomFieldSections.Identity => new JsonObject { ["fathers_name"] = "Charles Babbage" },
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

    private static JsonObject IdentitySection(
        bool withCustomField = false,
        bool optionHints = false,
        bool forRead = false,
        bool withExtendedIdentity = false)
    {
        var obj = new JsonObject
        {
            ["full_name"] = "Ada Lovelace",
            ["date_of_birth"] = "1990-05-15",
            ["gender"] = optionHints ? SwaggerExampleHints.Gender : "female",
            ["country"] = "Ghana",
            ["personal_email"] = "ada.personal@example.com",
            ["work_email"] = "ada.lovelace@company.com",
            ["phone"] = "+233201234567",
            ["linked_in_url"] = "https://linkedin.com/in/adalovelace",
            ["residential_address"] = "12 Independence Ave, Accra",
            ["profile_url"] = forRead
                ? EmployeeDocumentItem(SampleDocumentId1, "Employee profile photo")
                : SampleDocumentId1,
            ["identifications"] = forRead
                ? IdentificationsReadArray()
                : IdentificationsWriteArray(),
            ["custom_fields"] = withCustomField
                ? CustomFieldsForSection(EmployeeCustomFieldSections.Identity)
                : EmptyCustomFields(EmployeeCustomFieldSections.Identity),
        };

        if (withExtendedIdentity || forRead)
        {
            obj["marital_status"] = "Single";
            obj["next_of_kin_name"] = "Gary Ntori";
            obj["next_of_kin_phone"] = "+233240257669";
            obj["relationship_to_next_of_kin"] = "Brother";
            obj["emergency"] = forRead
                ? EmergencyReadArray()
                : EmergencyWriteArray();
        }

        return obj;
    }

    private static JsonArray IdentificationsWriteArray() => new(
        IdentificationEntry(idTypeId: SampleIdCardTypeId1, idNumber: "GHA-123456789-0"),
        IdentificationEntry(
            idTypeId: SampleIdCardTypeId2,
            idNumber: "G12345678",
            idIssueDate: "2023-05-01",
            idExpiryDate: "2033-05-01"));

    private static JsonArray IdentificationsReadArray() => new(
        IdentificationEntry(
            withId: true,
            idTypeId: SampleIdCardTypeId1,
            idTypeName: "Ghana Card",
            idNumber: "GHA-123456789-0",
            forRead: true),
        IdentificationEntry(
            withId: true,
            id: SampleIdentificationRowId,
            idTypeId: SampleIdCardTypeId2,
            idTypeName: "Passport",
            idNumber: "G12345678",
            idIssueDate: "2023-05-01",
            idExpiryDate: "2033-05-01",
            forRead: true));

    internal static JsonObject IdentificationEntry(
        bool withId = false,
        Guid? id = null,
        Guid? idTypeId = null,
        string? idTypeName = null,
        string? idNumber = null,
        string? idIssueDate = "2020-01-10",
        string? idExpiryDate = "2030-01-10",
        bool forRead = false)
    {
        var obj = new JsonObject
        {
            ["id_card_type_id"] = (idTypeId ?? SampleIdCardTypeId1).ToString(),
            ["id_card_type_number"] = idNumber ?? "GHA-123456789-0",
            ["id_card_type_issue_date"] = idIssueDate,
            ["id_card_type_expiry_date"] = idExpiryDate,
        };

        if (withId)
            obj["id"] = (id ?? SampleIdentificationRowId).ToString();

        if (forRead)
        {
            obj["id_card_type_name"] = idTypeName ?? "Ghana Card";
            AppendResourceAuditFields(obj, "2025-06-01T10:00:00+00:00");
        }

        return obj;
    }

    private static JsonArray EmergencyWriteArray() => new(EmergencyEntry());

    private static JsonArray EmergencyReadArray() => new(EmergencyEntry(withId: true, forRead: true));

    internal static JsonObject EmergencyEntry(
        bool withId = false,
        Guid? id = null,
        bool forRead = false)
    {
        var obj = new JsonObject
        {
            ["id"] = withId ? (id ?? Guid.Parse("88888888-8888-8888-8888-888888888801")).ToString() : null,
            ["emergency_contact_name"] = "Bright",
            ["emergency_contact_phone"] = "+233503448860",
            ["relationship"] = "Friend",
        };

        if (forRead)
            AppendResourceAuditFields(obj, "2025-06-01T10:00:00+00:00");

        return obj;
    }

    private static JsonArray PaymentWriteArray() => new(PaymentEntry());

    private static JsonArray PaymentReadArray() => new(PaymentEntry(withId: true, forRead: true));

    internal static JsonObject PaymentEntry(
        bool withId = false,
        Guid? id = null,
        bool forRead = false)
    {
        var obj = new JsonObject
        {
            ["id"] = withId ? (id ?? Guid.Parse("99999999-9999-9999-9999-999999999901")).ToString() : null,
            ["payment_mode"] = "bank_transfer",
            ["bank_name"] = "GCB Bank",
            ["account_name"] = "Ada Lovelace",
            ["account_number"] = "1234567890",
            ["branch_name"] = "Accra Main",
            ["is_primary"] = true,
        };

        if (forRead)
            AppendResourceAuditFields(obj, "2025-06-01T10:00:00+00:00");

        return obj;
    }

    private static JsonObject MedicalWriteSection() => new()
    {
        ["blood_group"] = "O+",
        ["has_medical_condition"] = true,
        ["medical_conditions"] = new JsonArray(MedicalConditionEntry()),
        ["allergies"] = new JsonArray(AllergyEntry()),
        ["takes_regular_medication"] = false,
        ["medications"] = new JsonArray(),
        ["disability_status"] = "None",
        ["requires_accommodation"] = false,
        ["accommodation_details"] = null,
        ["emergency_medical_notes"] = null,
    };

    private static JsonObject MedicalReadSection()
    {
        var section = new JsonObject
        {
            ["id"] = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa01").ToString(),
            ["blood_group"] = "O+",
            ["has_medical_condition"] = true,
            ["medical_conditions"] = new JsonArray(MedicalConditionEntry(withId: true, forRead: true)),
            ["allergies"] = new JsonArray(AllergyEntry(withId: true, forRead: true)),
            ["takes_regular_medication"] = false,
            ["medications"] = new JsonArray(),
            ["disability_status"] = "None",
            ["requires_accommodation"] = false,
            ["accommodation_details"] = null,
            ["emergency_medical_notes"] = null,
        };
        AppendResourceAuditFields(section, "2025-06-01T10:00:00+00:00");
        return section;
    }

    internal static JsonObject MedicalConditionEntry(bool withId = false, bool forRead = false)
    {
        var obj = new JsonObject
        {
            ["id"] = withId ? Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbb01").ToString() : null,
            ["condition"] = "Asthma",
            ["severity"] = "Mild",
            ["notes"] = "Carries inhaler; may need accommodation during high-exertion activities.",
            ["diagnosed_date"] = "2015-03-01",
        };

        if (forRead)
            AppendResourceAuditFields(obj, "2025-06-01T10:00:00+00:00");

        return obj;
    }

    internal static JsonObject AllergyEntry(bool withId = false, bool forRead = false)
    {
        var obj = new JsonObject
        {
            ["id"] = withId ? Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccc01").ToString() : null,
            ["allergen"] = "Penicillin",
            ["reaction"] = "Rash",
            ["severity"] = "Moderate",
        };

        if (forRead)
            AppendResourceAuditFields(obj, "2025-06-01T10:00:00+00:00");

        return obj;
    }

    internal static JsonObject MedicationEntry(bool withId = false, bool forRead = false)
    {
        var obj = new JsonObject
        {
            ["id"] = withId ? Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddd01").ToString() : null,
            ["name"] = "Lisinopril",
            ["dosage"] = "10mg",
            ["frequency"] = "Daily",
            ["notes"] = "Take in the morning",
        };

        if (forRead)
            AppendResourceAuditFields(obj, "2025-06-01T10:00:00+00:00");

        return obj;
    }

    internal static JsonObject SkillEntry(bool withId = false, bool forRead = false)
    {
        var obj = new JsonObject
        {
            ["id"] = withId ? Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeee01").ToString() : null,
            ["name"] = "React",
            ["proficiency"] = "advanced",
            ["years_of_experience"] = 5,
        };

        if (forRead)
            AppendResourceAuditFields(obj, "2025-06-01T10:00:00+00:00");

        return obj;
    }

    internal static JsonObject ExperienceEntry(bool withId = false, bool forRead = false)
    {
        var obj = new JsonObject
        {
            ["id"] = withId ? Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffff01").ToString() : null,
            ["company"] = "TechCorp Ghana",
            ["job_title"] = "Junior Developer",
            ["employment_type"] = "Full-time",
            ["location"] = "Accra",
            ["start_date"] = "2018-01-01",
            ["end_date"] = "2020-12-31",
            ["is_current"] = false,
            ["description"] = "Built internal tools with React and Node.js.",
        };

        if (forRead)
            AppendResourceAuditFields(obj, "2025-06-01T10:00:00+00:00");

        return obj;
    }

    internal static JsonObject ReferralEntry(bool withId = false, bool forRead = false)
    {
        var obj = new JsonObject
        {
            ["id"] = withId ? Guid.Parse("12121212-1212-1212-1212-121212121201").ToString() : null,
            ["name"] = "Grace Hopper",
            ["job_title"] = "Engineering Manager",
            ["company"] = "Naval Labs",
            ["relationship"] = "Former manager",
            ["email"] = "grace.hopper@example.com",
            ["phone"] = "+233201112233",
        };

        if (forRead)
            AppendResourceAuditFields(obj, "2025-06-01T10:00:00+00:00");

        return obj;
    }

    private static JsonObject EmploymentSection(
        bool withNames = false,
        bool optionHints = false,
        bool withSecondaryReportsTo = false)
    {
        var obj = new JsonObject
        {
            ["job_title"] = "Software Engineer",
            ["department_id"] = SampleDepartmentId.ToString(),
            ["branch_id"] = SampleBranchId.ToString(),
            ["employment_status"] = optionHints ? SwaggerExampleHints.EmploymentStatus : "Active",
            ["contract_type"] = optionHints ? SwaggerExampleHints.ContractType : "Permanent",
            ["work_arrangement"] = optionHints ? SwaggerExampleHints.WorkArrangement : "hybrid",
            ["work_location"] = "Accra HQ",
            ["pay_grade"] = "P4",
            ["start_date"] = "2025-06-01",
            ["probation_end_date"] = "2025-12-01",
            ["working_hours"] = 40,
            ["notice_period"] = "30 days",
            ["custom_fields"] = EmptyCustomFields(EmployeeCustomFieldSections.Employment),
        };

        if (!withNames)
        {
            obj["employment_type_id"] = SampleEmploymentTypeId.ToString();
            obj["reports_to_id"] = SampleReportsToId.ToString();
            if (withSecondaryReportsTo)
                obj["secondary_reports_to_id"] = SampleSecondaryReportsToId.ToString();
        }

        if (withNames)
        {
            obj["department_name"] = "Engineering";
            obj["branch_name"] = "Accra HQ";
            obj["employment_type"] = new JsonObject
            {
                ["id"] = SampleEmploymentTypeId.ToString(),
                ["name"] = "Full-time",
                ["description"] = "Standard salaried employment",
                ["type"] = "default",
            };
            obj["reports_to"] = new JsonObject
            {
                ["id"] = SampleReportsToId.ToString(),
                ["name"] = "Demo Admin",
                ["position"] = "Head of Engineering",
                ["photo_url"] = EmployeeDocumentItem(SampleDocumentId2, "Manager profile photo"),
            };
            obj["secondary_reports_to"] = new JsonObject
            {
                ["id"] = SampleSecondaryReportsToId.ToString(),
                ["name"] = "Jane Smith",
                ["position"] = "Engineering Lead",
                ["photo_url"] = EmployeeDocumentItem(SampleDocumentId2, "Secondary manager profile photo"),
            };
            obj["is_line_manager"] = false;
            obj["is_head_of_department"] = false;
        }

        return obj;
    }

    private static JsonObject CompensationSection(
        bool withCustomField = false,
        bool withExtendedCompensation = false)
    {
        var obj = new JsonObject
        {
            ["gross_salary"] = 8500.00m,
            ["pay_frequency"] = SwaggerExampleHints.PayFrequency,
            ["currency_id"] = SampleCurrencyId,
            ["custom_fields"] = withCustomField
                ? CustomFieldsForSection(EmployeeCustomFieldSections.Compensation)
                : EmptyCustomFields(EmployeeCustomFieldSections.Compensation),
        };

        if (withExtendedCompensation)
        {
            obj["net_salary"] = 7200.00m;
            obj["ssnit_insurance_number"] = "C1234567890";
            obj["payment"] = PaymentWriteArray();
        }

        return obj;
    }

    private static JsonObject CompensationReadSection() => new()
    {
        ["gross_salary"] = 8500.00m,
        ["net_salary"] = 7200.00m,
        ["ssnit_insurance_number"] = "C1234567890",
        ["pay_frequency"] = "Monthly",
        ["currency_id"] = SampleCurrencyId,
        ["currency_code"] = "GHS",
        ["currency_name"] = "Ghana Cedi",
        ["currency_symbol"] = "₵",
        ["annualized_cost"] = 102000.00m,
        ["payment"] = PaymentReadArray(),
        ["custom_fields"] = CustomFieldsForSection(EmployeeCustomFieldSections.Compensation),
    };

    internal static JsonObject EducationEntry(
        bool withId = false,
        Guid? id = null,
        string? degree = null,
        string? fieldOfStudy = null,
        bool includeCustomFields = true)
    {
        var obj = new JsonObject
        {
            ["institution"] = "University of Ghana",
            ["degree"] = degree ?? "BSc",
            ["field_of_study"] = fieldOfStudy ?? "Computer Science",
            ["start_date"] = "2008-09-01",
            ["end_date"] = "2012-06-30",
            ["is_current"] = false,
        };

        if (includeCustomFields)
            obj["custom_fields"] = EmptyCustomFields(EmployeeCustomFieldSections.Education);

        if (withId)
            obj["id"] = (id ?? SampleEducationRowId).ToString();

        if (withId)
            AppendResourceAuditFields(obj, "2025-06-01T10:00:00+00:00");

        return obj;
    }

    internal static JsonObject CertificationEntry(
        bool withId = false,
        Guid? id = null,
        string? name = null,
        string? issuingBody = null,
        string? issueDate = null,
        string? expiryDate = null,
        string? credentialUrl = null,
        bool omitExpiryDate = true,
        bool includeCustomFields = true)
    {
        var obj = new JsonObject
        {
            ["name"] = name ?? "Masters in react",
            ["issuing_body"] = issuingBody ?? "Udemy",
            ["issue_date"] = issueDate ?? "2026-05-31",
            ["credential_url"] = credentialUrl ?? "https://udemy.com/certificate/3424-3424",
        };

        if (includeCustomFields)
            obj["custom_fields"] = EmptyCustomFields(EmployeeCustomFieldSections.Certification);

        if (expiryDate is not null)
            obj["expiry_date"] = expiryDate;
        else if (!omitExpiryDate)
            obj["expiry_date"] = "2026-03-15";

        if (withId)
            obj["id"] = (id ?? SampleCertificationRowId).ToString();

        if (withId)
            AppendResourceAuditFields(obj, "2025-06-01T10:00:00+00:00");

        return obj;
    }

    internal static JsonObject AuditLogSummaryData() => new()
    {
        ["total_entries"] = 128,
        ["critical_count"] = 3,
        ["flagged_count"] = 5,
        ["sensitive_reads_count"] = 2,
        ["unique_actors_count"] = 12,
    };

    internal static JsonObject AuditLogListItemData() => new()
    {
        ["audit_log_id"] = SampleAuditLogId.ToString(),
        ["occurred_at"] = "2026-06-05T14:32:00Z",
        ["action_title"] = "Personal information updated",
        ["action_description"] = "Employee identity details updated.",
        ["employee"] = new JsonObject
        {
            ["employee_id"] = SampleEmployeeId.ToString(),
            ["employee_display_code"] = "EMP-0042",
            ["employee_full_name"] = "Ama Mensah",
        },
        ["actor_id"] = "u1000001-0000-0000-0000-000000000001",
        ["actor_full_name"] = "Demo Admin",
        ["category"] = SwaggerExampleHints.AuditCategory,
        ["severity"] = "Medium",
        ["is_flagged"] = SwaggerExampleHints.BooleanPipe,
    };

    internal static JsonObject AuditLogStatisticsResponse() => EnvelopeOk(AuditLogSummaryData());

    internal static JsonObject AuditLogListResponse() => EnvelopeOk(AuditLogListData(), SamplePagination());

    internal static JsonObject AuditLogListData() => new()
    {
        ["summary"] = AuditLogSummaryData(),
        ["items"] = new JsonArray(AuditLogListItemData()),
    };

    internal static JsonObject AuditLogGetResponse() => EnvelopeOk(AuditLogListItemData());

    internal static JsonObject AuditLogPurgeData() => new()
    {
        ["deleted_count"] = 42,
        ["retention_window"] = 90,
        ["cutoff_before"] = "2026-03-05T12:00:00+00:00",
    };

    internal static JsonObject AuditLogPurgePreviewData() => new()
    {
        ["eligible_count"] = 847,
        ["retention_window"] = 180,
        ["cutoff_before"] = "2025-12-10T12:00:00+00:00",
    };

    internal static JsonObject AuditLogPurgeResponse() => EnvelopeOk(AuditLogPurgeData());

    internal static JsonObject AuditLogPurgePreviewResponse() => EnvelopeOk(AuditLogPurgePreviewData());

    internal static JsonObject PlatformUserListItemData() => new()
    {
        ["id"] = "u1000001-0000-0000-0000-000000000001",
        ["tenant_id"] = "tenant_demo",
        ["fullname"] = "Demo Admin",
        ["email"] = "admin@demo.trovesuite.com",
        ["contact"] = "+233200000001",
        ["address"] = "Accra",
        ["gender"] = "MALE",
        ["dob"] = "1990-01-15",
        ["profile_pic"] = null,
        ["can_login"] = true,
        ["delete_status"] = "NOT_DELETED",
        ["is_active"] = true,
        ["is_owner"] = false,
        ["description"] = null,
        ["cdate"] = "2026-01-01",
        ["ctime"] = "10:00:00",
        ["cdatetime"] = "2026-01-01T10:00:00+00:00",
    };

    internal static JsonObject PlatformUsersListResponse() =>
        EnvelopeOk(new JsonArray(PlatformUserListItemData()), SamplePagination());

    // ── Leave management ─────────────────────────────────────────────────────

    internal static JsonObject LeaveSummaryData() => new()
    {
        ["pending_requests"] = 4,
        ["pending_final_approvals"] = 2,
        ["approved_this_month"] = 12,
        ["on_leave_today"] = 2,
        ["leaving_this_week"] = 7,
        ["low_balance_alert"] = 1,
        ["total_requests"] = 86,
    };

    internal static JsonObject LeaveEmployeeRefData(
        string fullName = "Ama Asante",
        string jobTitle = "Senior Product Designer",
        bool includeProfile = true) => new()
    {
        ["employee_id"] = SampleEmployeeId.ToString(),
        ["full_name"] = fullName,
        ["employee_code"] = "EMP-001",
        ["job_title"] = jobTitle,
        ["department_id"] = SampleDepartmentId.ToString(),
        ["department_name"] = "Data & Insights",
        ["profile_url"] = includeProfile
            ? EmployeeDocumentItem(SampleDocumentId1, "Employee profile photo", "profile.jpg")
            : null,
    };

    internal static JsonObject LeaveTypeRefData(string name = "Annual Leave") => new()
    {
        ["leave_type_id"] = SampleLeaveTypeId.ToString(),
        ["name"] = name,
    };

    internal static JsonObject LeaveApproverRefData() => new()
    {
        ["approver_id"] = "cp-user-demo-admin",
        ["full_name"] = "Fiifi Boakye",
    };

    internal static void AppendResourceAuditFields(JsonObject target) => AppendResourceAuditFields(
        target,
        "2026-01-15T08:00:00+00:00",
        "2026-06-01T12:00:00+00:00");

    internal static void AppendResourceAuditFields(JsonObject target, string createdAt) =>
        AppendResourceAuditFields(target, createdAt, createdAt);

    internal static void AppendResourceAuditFields(
        JsonObject target,
        string createdAt,
        string updatedAt)
    {
        target["created_at"] = createdAt;
        target["updated_at"] = updatedAt;
        target["created_by_id"] = "cp-user-demo-admin";
        target["updated_by_id"] = "cp-user-demo-admin";
        target["created_by"] = "Fiifi Boakye";
        target["updated_by"] = "Fiifi Boakye";
    }

    internal static JsonObject LeaveApprovalStepApproverData() => LeaveApproverRefData();

    internal static JsonObject LeaveRequestItemData(
        string status = "Pending",
        decimal? remainingDays = 14,
        string employeeName = "Ama Asante",
        string leaveTypeName = "Annual Leave",
        string? leaveRequestId = null)
    {
        var data = new JsonObject
        {
            ["leave_request_id"] = leaveRequestId ?? SampleLeaveRequestId.ToString(),
            ["employee"] = LeaveEmployeeRefData(employeeName),
            ["leave_type"] = LeaveTypeRefData(leaveTypeName),
            ["start_date"] = "2026-07-07",
            ["end_date"] = "2026-07-11",
            ["days_requested"] = 5,
            ["status"] = status,
            ["approval_stage"] = status switch
            {
                "Approved" => "approved",
                "Rejected" => "rejected",
                _ => "pending_final",
            },
            ["approver"] = status is "Approved" or "Rejected" ? LeaveApproverRefData() : null,
            ["approved_by"] = new JsonArray(
                LeaveApproverRefData(),
                new JsonObject
                {
                    ["approver_id"] = "cp-user-hod",
                    ["full_name"] = "Demo Admin",
                }),
            ["prior_approvers"] = new JsonArray(
                new JsonObject
                {
                    ["stage"] = "line_manager",
                    ["status"] = "approved",
                    ["approver"] = LeaveApprovalStepApproverData(),
                    ["decided_at"] = "2026-06-09T10:00:00+00:00",
                },
                new JsonObject
                {
                    ["stage"] = "head_of_department",
                    ["status"] = "approved",
                    ["approver"] = LeaveApprovalStepApproverData(),
                    ["decided_at"] = "2026-06-09T14:30:00+00:00",
                }),
            ["notes"] = status == "Rejected" ? "Team coverage required during sprint." : "Family visit.",
            ["remaining_days"] = remainingDays,
            ["waiting_hours"] = status == "Pending" ? 53 : null,
            ["returns_on"] = "2026-07-12",
            ["days_since_last_approval"] = null,
            ["submitted_at"] = "2026-06-10T09:15:00+00:00",
            ["decided_at"] = status is "Approved" or "Rejected" ? "2026-06-11T16:20:00+00:00" : null,
        };
        AppendResourceAuditFields(data, "2026-06-10T09:15:00+00:00", "2026-06-11T16:20:00+00:00");
        return data;
    }

    internal static JsonObject LeaveRequestDetailData(
        string status = "Pending",
        string approvalStage = "pending_final")
    {
        var data = new JsonObject
        {
            ["leave_request_id"] = SampleLeaveRequestId.ToString(),
            ["employee"] = LeaveEmployeeRefData(),
            ["leave_type"] = LeaveTypeRefData(),
            ["start_date"] = "2026-07-07",
            ["end_date"] = "2026-07-11",
            ["days_requested"] = 5,
            ["working_days"] = 5,
            ["public_holidays_in_range"] = 0,
            ["status"] = status,
            ["approval_stage"] = approvalStage,
            ["approver"] = status is "Approved" or "Rejected" ? LeaveApproverRefData() : null,
            ["approval_trail"] = new JsonArray(
                new JsonObject
                {
                    ["stage"] = "line_manager",
                    ["status"] = "approved",
                    ["approver"] = LeaveApprovalStepApproverData(),
                    ["decided_at"] = "2026-06-09T10:00:00+00:00",
                },
                new JsonObject
                {
                    ["stage"] = "head_of_department",
                    ["status"] = "approved",
                    ["approver"] = LeaveApprovalStepApproverData(),
                    ["decided_at"] = "2026-06-09T14:30:00+00:00",
                },
                new JsonObject
                {
                    ["stage"] = "final",
                    ["status"] = status == "Approved" ? "approved" : status == "Rejected" ? "rejected" : "pending",
                    ["approver"] = status is "Approved" or "Rejected" ? LeaveApproverRefData() : null,
                    ["decided_at"] = status is "Approved" or "Rejected" ? "2026-06-11T16:20:00+00:00" : null,
                }),
            ["notes"] = status == "Rejected" ? "Conflicts with audit period." : "Family visit.",
            ["remaining_days"] = 14,
            ["balance_impact"] = new JsonObject
            {
                ["current"] = 14,
                ["after"] = 9,
                ["deduction"] = 5,
            },
            ["waiting_hours"] = status == "Pending" ? 53 : null,
            ["submitted_at"] = "2026-06-10T09:15:00+00:00",
            ["decided_at"] = status is "Approved" or "Rejected" ? "2026-06-11T16:20:00+00:00" : null,
        };
        AppendResourceAuditFields(data, "2026-06-10T09:15:00+00:00", "2026-06-11T16:20:00+00:00");
        return data;
    }

    internal static JsonObject LeaveBalanceItemData()
    {
        var data = new JsonObject
        {
            ["leave_balance_id"] = SampleLeaveBalanceId.ToString(),
            ["employee"] = LeaveEmployeeRefData(),
            ["leave_type"] = LeaveTypeRefData(),
            ["entitled_days"] = 21,
            ["used_days"] = 7,
            ["remaining_days"] = 14,
        };
        AppendResourceAuditFields(data);
        return data;
    }

    internal static JsonArray LeaveBalanceItemsArray()
    {
        var second = new JsonObject
        {
            ["leave_balance_id"] = "a2222222-2222-2222-2222-222222222205",
            ["employee"] = LeaveEmployeeRefData(),
            ["leave_type"] = new JsonObject
            {
                ["leave_type_id"] = "a2222222-2222-2222-2222-222222222206",
                ["name"] = "Sick Leave",
            },
            ["entitled_days"] = 10,
            ["used_days"] = 2,
            ["remaining_days"] = 8,
        };
        AppendResourceAuditFields(second);
        return new JsonArray(LeaveBalanceItemData(), second);
    }

    internal static JsonObject LeaveBalanceListData() => new()
    {
        ["items"] = LeaveBalanceItemsArray(),
    };

    internal static JsonObject LeaveListData() => new()
    {
        ["summary"] = LeaveSummaryData(),
        ["items"] = new JsonArray(
            LeaveRequestItemData("Pending", 14, "Ama Asante", "Annual Leave"),
            LeaveRequestItemData("Pending", 7, "Kwame Asare", "Sick Leave", "a2222222-2222-2222-2222-222222222202"),
            LeaveRequestItemData("Approved", 9, "Abena Osei", "Unpaid Leave", "a2222222-2222-2222-2222-222222222203")),
    };

    internal static JsonObject LeaveDashboardOnLeaveItemData(
        string leaveRequestId,
        string employeeName,
        string leaveType,
        string returnsOn,
        bool includeProfile = true)
    {
        var data = new JsonObject
        {
            ["leave_request_id"] = leaveRequestId,
            ["employee_id"] = SampleEmployeeId.ToString(),
            ["employee_name"] = employeeName,
            ["profile_url"] = includeProfile
                ? EmployeeDocumentItem(SampleDocumentId1, "Employee profile photo", "profile.jpg")
                : null,
            ["leave_type"] = leaveType,
            ["returns_on"] = returnsOn,
        };
        AppendResourceAuditFields(data);
        return data;
    }

    internal static JsonObject LeaveDashboardPendingItemData(
        string leaveRequestId,
        string employeeName,
        string leaveType,
        decimal leaveDays,
        int? waiting = null,
        int? hoursSinceLastApproval = null,
        int? daysSinceLastApproval = null,
        bool includeProfile = true)
    {
        var data = new JsonObject
        {
            ["leave_request_id"] = leaveRequestId,
            ["employee_id"] = SampleEmployeeId.ToString(),
            ["employee_name"] = employeeName,
            ["profile_url"] = includeProfile
                ? EmployeeDocumentItem(SampleDocumentId1, "Employee profile photo", "profile.jpg")
                : null,
            ["leave_type"] = leaveType,
            ["leave_days"] = leaveDays,
            ["waiting"] = waiting,
            ["hours_since_last_approval"] = hoursSinceLastApproval,
            ["days_since_last_approval"] = daysSinceLastApproval,
        };
        AppendResourceAuditFields(data);
        return data;
    }

    internal static JsonObject LeaveDashboardLeavingItemData(
        string leaveRequestId,
        string employeeName,
        string leaveType,
        string startsOn,
        decimal leaveDays,
        bool includeProfile = true)
    {
        var data = new JsonObject
        {
            ["leave_request_id"] = leaveRequestId,
            ["employee_id"] = SampleEmployeeId.ToString(),
            ["employee_name"] = employeeName,
            ["profile_url"] = includeProfile
                ? EmployeeDocumentItem(SampleDocumentId1, "Employee profile photo", "profile.jpg")
                : null,
            ["leave_type"] = leaveType,
            ["starts_on"] = startsOn,
            ["leave_days"] = leaveDays,
        };
        AppendResourceAuditFields(data);
        return data;
    }

    internal static JsonObject LeaveDashboardSummaryData() => new()
    {
        ["on_leave_today"] = 3,
        ["pending_approvals"] = 5,
        ["leaving_this_week"] = 3,
        ["low_balance_alert"] = 0,
    };

    internal static JsonObject LeaveDashboardData() => new()
    {
        ["summary"] = LeaveDashboardSummaryData(),
        ["on_leave_today"] = new JsonArray(
            LeaveDashboardOnLeaveItemData(
                SampleLeaveRequestId.ToString(),
                "Ama Asante",
                "Annual Leave",
                "2026-09-25"),
            LeaveDashboardOnLeaveItemData(
                "a2222222-2222-2222-2222-222222222212",
                "Kwame Nkrumah",
                "Sick Leave",
                "2026-06-16",
                includeProfile: false),
            LeaveDashboardOnLeaveItemData(
                "a2222222-2222-2222-2222-222222222214",
                "Efua Sutherland",
                "Vacation",
                "2026-06-20")),
        ["pending_approvals"] = new JsonArray(
            LeaveDashboardPendingItemData(
                SampleLeaveRequestId.ToString(),
                "Kwame Asare",
                "Annual Leave",
                5,
                waiting: 53),
            LeaveDashboardPendingItemData(
                "a2222222-2222-2222-2222-222222222215",
                "Ama Boateng",
                "Sick Leave",
                2,
                hoursSinceLastApproval: 12),
            LeaveDashboardPendingItemData(
                "a2222222-2222-2222-2222-222222222216",
                "Ebo Kusi",
                "Maternity Leave",
                30,
                daysSinceLastApproval: 5,
                includeProfile: false)),
        ["leaving_this_week"] = new JsonArray(
            LeaveDashboardLeavingItemData(
                "a2222222-2222-2222-2222-222222222211",
                "Ama Asante",
                "Maternity Leave",
                "2026-06-10",
                2),
            LeaveDashboardLeavingItemData(
                "a2222222-2222-2222-2222-222222222213",
                "Efua Sutherland",
                "Vacation",
                "2026-06-11",
                5,
                includeProfile: false),
            LeaveDashboardLeavingItemData(
                "a2222222-2222-2222-2222-222222222217",
                "Kwame Nkrumah",
                "Personal Leave",
                "2026-06-12",
                1)),
    };

    internal static JsonObject LeaveDashboardResponse() => EnvelopeOk(LeaveDashboardData());

    internal static JsonObject LeaveCalendarListItemData(
        string employeeName = "Ama Asante",
        string title = "Senior Product Designer",
        string employeeCode = "ZEL-0042",
        bool includeProfile = true,
        bool includeLeave = true,
        string? employeeId = null,
        string leaveType = "Annual Leave",
        string status = "Approved",
        string leaveFrom = "2026-06-05",
        string leaveTo = "2026-06-07") => new()
    {
        ["employee_id"] = employeeId ?? SampleEmployeeId.ToString(),
        ["employee_name"] = employeeName,
        ["employee_code"] = employeeCode,
        ["title"] = title,
        ["profile_url"] = includeProfile
            ? EmployeeDocumentItem(SampleDocumentId1, "Profile photo", "ama.jpg")
            : null,
        ["leave_request_id"] = includeLeave ? SampleLeaveRequestId.ToString() : null,
        ["leave_type_id"] = includeLeave ? SampleLeaveTypeId.ToString() : null,
        ["leave_type"] = includeLeave ? leaveType : null,
        ["status"] = includeLeave ? status : null,
        ["leave_from"] = includeLeave ? leaveFrom : null,
        ["leave_to"] = includeLeave ? leaveTo : null,
        ["leave_days"] = includeLeave ? 3 : null,
    };

    internal static JsonObject LeaveCalendarData() => new()
    {
        ["view"] = "week",
        ["anchor_date"] = "2026-06-05",
        ["from_date"] = "2026-06-02",
        ["to_date"] = "2026-06-08",
        ["items"] = new JsonArray(
            LeaveCalendarListItemData(),
            LeaveCalendarListItemData(
                "Ama Asante",
                "Senior Product Designer",
                "ZEL-0042",
                includeProfile: true,
                includeLeave: true,
                leaveType: "Maternity Leave",
                status: "Pending",
                leaveFrom: "2026-06-10",
                leaveTo: "2026-06-12"),
            LeaveCalendarListItemData(
                "Kwame Asare",
                "Software Engineer",
                "ZEL-0055",
                includeLeave: false,
                employeeId: "a2222222-2222-2222-2222-222222222212")),
    };

    internal static JsonObject LeaveCalendarPagination() => new()
    {
        ["page"] = 1,
        ["size"] = 50,
        ["total"] = 42,
        ["has_next"] = false,
    };

    internal static JsonObject LeaveCalendarResponse() =>
        EnvelopeOk(LeaveCalendarData(), LeaveCalendarPagination());

    internal static JsonObject LeaveBalanceListResponse() => EnvelopeOk(LeaveBalanceListData());

    internal static JsonObject LeaveBalanceGetResponse() => EnvelopeOk(LeaveBalanceItemData());

    internal static JsonObject LeaveTypeListResponse() => EnvelopeOk(
        LeaveTypeListData(),
        pagination: new JsonObject
        {
            ["page"] = 1,
            ["size"] = 20,
            ["total"] = 2,
            ["has_next"] = false,
        });

    internal static JsonObject EmploymentTypeListResponse() => EnvelopeOk(
        EmploymentTypeListData(),
        pagination: new JsonObject
        {
            ["page"] = 1,
            ["size"] = 20,
            ["total"] = 5,
            ["has_next"] = false,
        });

    internal static JsonObject EmploymentTypeGetResponse() => EnvelopeOk(EmploymentTypeItemData());

    internal static JsonObject EmploymentTypeGetCustomResponse() => EnvelopeOk(EmploymentTypeCustomItemData());

    internal static JsonObject CreateEmploymentTypeBody() => new()
    {
        ["name"] = "Apprentice",
        ["description"] = "Structured on-the-job training programme",
    };

    internal static JsonObject EmploymentTypeRenameBlockedResponse() => ValidationErrorEnvelope(
        new JsonObject { ["name"] = "System default employment types cannot be renamed." });

    internal static JsonObject EmploymentTypeDeleteBlockedResponse() => new()
    {
        ["success"] = false,
        ["status_code"] = 409,
        ["detail"] = "Employment type is assigned to employees and cannot be deleted.",
        ["data"] = null,
        ["field_errors"] = null,
    };

    internal static JsonObject EmploymentTypeDeleteSuccessResponse() => EnvelopeOk(
        new JsonObject(),
        detail: "Employment type deleted.");

    internal static JsonObject EmploymentTypeCustomItemData()
    {
        var data = new JsonObject
        {
            ["employment_type_id"] = "b3333333-3333-3333-3333-333333333307",
            ["name"] = "Apprentice",
            ["description"] = "Structured on-the-job training programme",
            ["type"] = "custom",
            ["is_system_default"] = false,
            ["employee_count"] = 8,
            ["is_active"] = true,
        };
        AppendResourceAuditFields(data);
        return data;
    }

    internal static JsonObject LeaveTypeGetResponse() => EnvelopeOk(LeaveTypeItemData());

    internal static JsonObject LeaveHolidayGetResponse() => EnvelopeOk(PublicHolidayItemData());

    internal static JsonObject LeaveListResponse() =>
        EnvelopeOk(LeaveListData(), LeaveListPagination());

    internal static JsonObject LeaveApprovalApproverData(
        string name = "Fiifi Boakye",
        bool includeProfile = true) => new()
    {
        ["name"] = name,
        ["profile_url"] = includeProfile
            ? EmployeeDocumentItem(SampleDocumentId2, "Approver profile photo", "fiifi.jpg")
            : null,
    };

    internal static JsonObject LeaveApprovalListItemData(
        string employeeName = "Ama Asante",
        string leaveType = "Annual Leave",
        int waiting = 28,
        string title = "Senior Product Designer",
        string employeeCode = "ZEL-0042",
        bool includeProfile = true,
        string? leaveRequestId = null,
        string leaveFrom = "2026-06-15",
        string leaveTo = "2026-06-19",
        decimal leaveDays = 5,
        JsonArray? approvedBy = null)
    {
        var data = new JsonObject
        {
            ["leave_request_id"] = leaveRequestId ?? SampleLeaveRequestId.ToString(),
            ["employee_id"] = SampleEmployeeId.ToString(),
            ["employee_name"] = employeeName,
            ["employee_code"] = employeeCode,
            ["title"] = title,
            ["profile_url"] = includeProfile
                ? EmployeeDocumentItem(SampleDocumentId1, "Employee profile photo", "profile.jpg")
                : null,
            ["leave_type"] = leaveType,
            ["leave_from"] = leaveFrom,
            ["leave_to"] = leaveTo,
            ["leave_days"] = leaveDays,
            ["waiting"] = waiting,
            ["approved_by"] = approvedBy ?? new JsonArray(
                LeaveApprovalApproverData("Fiifi Boakye"),
                LeaveApprovalApproverData("Kwame Mensah")),
        };
        AppendResourceAuditFields(data);
        return data;
    }

    internal static JsonObject LeaveApprovalListData() => new()
    {
        ["pending_count"] = 6,
        ["items"] = new JsonArray(
            LeaveApprovalListItemData(
                "Ama Asante",
                "Annual Leave",
                28,
                "Senior Product Designer"),
            LeaveApprovalListItemData(
                "Kofi Adom",
                "Sick Leave",
                8,
                "Backend Engineer",
                leaveRequestId: "a1111111-1111-1111-1111-111111111102",
                leaveFrom: "2026-06-16",
                leaveTo: "2026-06-18",
                leaveDays: 3),
            LeaveApprovalListItemData(
                "Abena Osei",
                "Unpaid Leave",
                15,
                "HR Business Partner",
                "ZEL-0108",
                includeProfile: false,
                leaveRequestId: "a1111111-1111-1111-1111-111111111103",
                leaveFrom: "2026-06-20",
                leaveTo: "2026-06-24",
                leaveDays: 5,
                approvedBy: new JsonArray(
                    LeaveApprovalApproverData("Fiifi Boakye", includeProfile: false),
                    LeaveApprovalApproverData("Kwame Mensah", includeProfile: false)))),
    };

    internal static JsonObject LeaveApprovalListResponse() =>
        EnvelopeOk(LeaveApprovalListData(), LeaveApprovalListPagination());

    internal static JsonObject LeaveMyRequestListData() => new()
    {
        ["items"] = new JsonArray(
            LeaveRequestItemData("Pending", 14),
            LeaveRequestItemData("Approved", 9, "Ama Asante", "Annual Leave", "a2222222-2222-2222-2222-222222222204"),
            LeaveRequestItemData("Rejected", 11, "Ama Asante", "Casual Leave", "a2222222-2222-2222-2222-222222222205")),
    };

    internal static JsonObject LeaveMyRequestListResponse() =>
        EnvelopeOk(LeaveMyRequestListData(), LeaveListPagination());

    internal static JsonObject LeaveApprovalListPagination() => new()
    {
        ["page"] = 2,
        ["size"] = 10,
        ["total"] = 15,
        ["has_next"] = true,
        ["page_size"] = 10,
        ["total_count"] = 15,
        ["total_pages"] = 2,
    };

    internal static JsonObject LeaveListPagination() => new()
    {
        ["page"] = 1,
        ["size"] = 20,
        ["total"] = 24,
        ["has_next"] = SwaggerExampleHints.BooleanPipe,
        ["page_size"] = 20,
        ["total_count"] = 24,
        ["total_pages"] = 2,
    };

    internal static JsonObject LeavePersonalSummaryData() => new()
    {
        ["total_remaining_days"] = 22,
        ["pending_requests"] = 1,
        ["approved_this_year"] = 3,
        ["balances"] = LeaveBalanceItemsArray(),
    };

    internal static JsonObject LeavePersonalSummaryResponse() => EnvelopeOk(LeavePersonalSummaryData());

    internal static JsonObject LeaveStatisticsResponse() => EnvelopeOk(LeaveSummaryData());

    internal static JsonObject LeaveRequestGetResponse() => EnvelopeOk(LeaveRequestDetailData());

    internal static JsonObject LeaveRequestApprovedResponse() =>
        EnvelopeOk(LeaveRequestDetailData("Approved", "approved"), detail: "Leave request approved.");

    internal static JsonObject LeaveRequestRejectedResponse() =>
        EnvelopeOk(LeaveRequestDetailData("Rejected", "rejected"), detail: "Leave request rejected.");

    internal static JsonObject LeaveTypeItemData(bool optionHints = false)
    {
        var data = new JsonObject
        {
            ["leave_type_id"] = SampleLeaveTypeId.ToString(),
            ["name"] = optionHints ? SwaggerExampleHints.LeaveTypeName : "Annual Leave",
            ["default_entitled_days"] = 21,
            ["is_paid"] = optionHints ? SwaggerExampleHints.BooleanPipe : true,
            ["accrual_method"] = optionHints
                ? SwaggerExampleHints.LeaveAccrualMethod
                : LeaveAccrualMethods.FrontLoaded,
            ["carry_over_allowed"] = optionHints ? SwaggerExampleHints.BooleanPipe : false,
            ["applies_to_employment_types"] = optionHints
                ? new JsonArray(SwaggerExampleHints.LeaveTypeEmploymentType)
                : new JsonArray("Full-time", "Part-time", "Contract"),
            ["min_notice_working_days"] = 5,
            ["max_consecutive_days"] = null,
            ["requires_supporting_document"] = optionHints ? SwaggerExampleHints.BooleanPipe : false,
        };
        AppendResourceAuditFields(data);
        return data;
    }

    internal static JsonObject LeaveTypeListData()
    {
        var second = new JsonObject
        {
            ["leave_type_id"] = "a2222222-2222-2222-2222-222222222206",
            ["name"] = "Sick Leave",
            ["default_entitled_days"] = 10,
            ["is_paid"] = true,
            ["accrual_method"] = LeaveAccrualMethods.Monthly,
            ["carry_over_allowed"] = false,
            ["applies_to_employment_types"] = new JsonArray("Full-time"),
            ["min_notice_working_days"] = null,
            ["max_consecutive_days"] = 5,
            ["requires_supporting_document"] = true,
        };
        AppendResourceAuditFields(second);
        return new JsonObject
        {
            ["items"] = new JsonArray(LeaveTypeItemData(), second),
        };
    }

    internal static JsonObject EmploymentTypeItemData(bool optionHints = false)
    {
        var data = new JsonObject
        {
            ["employment_type_id"] = SampleEmploymentTypeId.ToString(),
            ["name"] = optionHints ? SwaggerExampleHints.EmploymentType : "Full-time",
            ["description"] = "Standard salaried employment",
            ["type"] = optionHints ? "default|custom" : "default",
            ["is_system_default"] = true,
            ["employee_count"] = 198,
            ["is_active"] = true,
        };
        AppendResourceAuditFields(data);
        return data;
    }

    internal static JsonObject IdCardTypeItemData(bool optionHints = false)
    {
        var data = new JsonObject
        {
            ["id_card_type_id"] = SampleIdCardTypeId1.ToString(),
            ["name"] = optionHints ? "National ID|Voter's ID|Driver's License" : "National ID",
            ["description"] = "Ghana Card national identity document",
            ["type"] = optionHints ? "default|custom" : "default",
            ["is_system_default"] = true,
            ["is_active"] = true,
        };
        AppendResourceAuditFields(data);
        return data;
    }

    internal static JsonObject EmploymentTypeListData()
    {
        var custom = new JsonObject
        {
            ["employment_type_id"] = "b3333333-3333-3333-3333-333333333307",
            ["name"] = "Apprentice",
            ["description"] = "Structured on-the-job training",
            ["type"] = "custom",
            ["is_system_default"] = false,
            ["employee_count"] = 8,
            ["is_active"] = true,
        };
        AppendResourceAuditFields(custom);
        return new JsonObject
        {
            ["items"] = new JsonArray(EmploymentTypeItemData(), custom),
        };
    }

    internal static JsonObject CompanyOfficeItemData()
    {
        var data = new JsonObject
        {
            ["office_id"] = SampleOfficeId.ToString(),
            ["name"] = "Accra Office",
            ["country"] = "Ghana",
            ["city"] = "Accra",
            ["phone"] = "+233244000000",
            ["is_head_office"] = true,
        };
        AppendResourceAuditFields(data);
        return data;
    }

    private static JsonObject CompanyOfficeItemData2()
    {
        var data = new JsonObject
        {
            ["office_id"] = SampleOfficeId2.ToString(),
            ["name"] = "Kigali Office",
            ["country"] = "Rwanda",
            ["city"] = "Kigali",
            ["phone"] = "+250788000000",
            ["is_head_office"] = false,
        };
        AppendResourceAuditFields(data);
        return data;
    }

    internal static JsonObject CompanyInfoData()
    {
        var data = new JsonObject
        {
            ["id"] = SampleCompanyProfileId.ToString(),
            ["legal_name"] = "Marvel Industries",
            ["trading_name"] = "Marvel",
            ["industry"] = "Technology",
            ["company_size"] = "201-500 employees",
            ["business_registration_number"] = "CS-04829-2018",
            ["tin"] = "C0009827451",
            ["primary_work_country"] = "Ghana",
            ["company_email"] = "people@marvel.com",
            ["website"] = "marvel.com",
            ["logo_url"] = EmployeeDocumentItem(SampleDocumentId1, "Company logo", "marvel-logo.png"),
            ["banner_url"] = null,
            ["offices"] = new JsonArray(CompanyOfficeItemData(), CompanyOfficeItemData2()),
        };
        AppendResourceAuditFields(data);
        return data;
    }

    internal static JsonObject CompanyInfoStubData()
    {
        var data = new JsonObject
        {
            ["id"] = SampleCompanyProfileId.ToString(),
            ["legal_name"] = "Marvel Industries",
            ["trading_name"] = null,
            ["industry"] = null,
            ["company_size"] = null,
            ["business_registration_number"] = null,
            ["tin"] = null,
            ["primary_work_country"] = null,
            ["company_email"] = null,
            ["website"] = null,
            ["logo_url"] = null,
            ["banner_url"] = null,
            ["offices"] = new JsonArray(),
        };
        AppendResourceAuditFields(data);
        return data;
    }

    internal static JsonObject CompanyInfoGetStubResponse() => EnvelopeOk(CompanyInfoStubData());

    internal static JsonObject CreateCompanyInfoBody() => new()
    {
        ["legal_name"] = "Marvel Industries",
        ["trading_name"] = "Marvel",
        ["industry"] = "Technology",
        ["company_size"] = "201-500 employees",
        ["business_registration_number"] = "CS-04829-2018",
        ["tin"] = "C0009827451",
        ["primary_work_country"] = "Ghana",
        ["company_email"] = "people@marvel.com",
        ["website"] = "marvel.com",
        ["logo_url"] = SampleDocumentId1,
        ["offices"] = new JsonArray(
            new JsonObject
            {
                ["name"] = "Accra Office",
                ["country"] = "Ghana",
                ["city"] = "Accra",
                ["phone"] = "+233244000000",
                ["is_head_office"] = true,
            }),
    };

    internal static JsonObject UpdateCompanyInfoBody() => new()
    {
        ["id"] = SampleCompanyProfileId.ToString(),
        ["legal_name"] = "Marvel Industries",
        ["trading_name"] = "Marvel",
        ["industry"] = "Technology",
        ["company_size"] = "201-500 employees",
        ["business_registration_number"] = "CS-04829-2018",
        ["tin"] = "C0009827451",
        ["primary_work_country"] = "Ghana",
        ["company_email"] = "people@marvel.com",
        ["website"] = "https://marvel.com",
        ["logo_url"] = SampleDocumentId1,
        ["offices"] = new JsonArray(
            new JsonObject
            {
                ["office_id"] = SampleOfficeId.ToString(),
                ["name"] = "Accra Office",
                ["country"] = "Ghana",
                ["city"] = "Accra",
                ["phone"] = "+233244000000",
                ["is_head_office"] = true,
            },
            new JsonObject
            {
                ["name"] = "Kumasi Office",
                ["country"] = "Ghana",
                ["city"] = "Kumasi",
                ["phone"] = "+233322000000",
                ["is_head_office"] = false,
            }),
    };

    internal static JsonObject CompanyLocalizationGetStubResponse() => EnvelopeOk(CompanyLocalizationData());

    internal static JsonObject CompanyLocalizationData()
    {
        var data = new JsonObject
        {
            ["id"] = SampleCompanyLocalizationId.ToString(),
            ["time_zone"] = "Africa/Accra",
            ["currency_id"] = SampleCurrencyId,
            ["date_format"] = "DD/MM/YYYY",
            ["number_format"] = "1,234.56",
            ["first_day_of_week"] = "Monday",
            ["year_start_month"] = "January",
            ["year_start_day"] = 1,
        };
        AppendResourceAuditFields(data);
        return data;
    }

    internal static JsonObject CreateCompanyLocalizationBody() => new()
    {
        ["time_zone"] = "Africa/Accra",
        ["currency_id"] = SampleCurrencyId,
        ["date_format"] = "DD/MM/YYYY",
        ["number_format"] = "1,234.56",
        ["first_day_of_week"] = "Monday",
        ["year_start_month"] = "January",
        ["year_start_day"] = 1,
    };

    internal static JsonObject UpdateCompanyLocalizationBody() => new()
    {
        ["id"] = SampleCompanyLocalizationId.ToString(),
        ["time_zone"] = "Africa/Accra",
        ["currency_id"] = SampleCurrencyId,
        ["date_format"] = "DD/MM/YYYY",
        ["number_format"] = "1,234.56",
        ["first_day_of_week"] = "Monday",
        ["year_start_month"] = "January",
        ["year_start_day"] = 1,
    };

    internal static JsonObject PublicHolidayItemData()
    {
        var data = new JsonObject
        {
            ["holiday_id"] = SampleHolidayId.ToString(),
            ["holiday_name"] = "Independence Day",
            ["date"] = "2026-03-06",
            ["is_recurring_annually"] = true,
            ["country"] = SampleCountryName,
        };
        AppendResourceAuditFields(data, "2026-01-10T09:00:00+00:00", "2026-01-10T09:00:00+00:00");
        return data;
    }

    internal static JsonObject PublicHolidayListData()
    {
        var second = new JsonObject
        {
            ["holiday_id"] = "a2222222-2222-2222-2222-222222222207",
            ["holiday_name"] = "Madaraka Day",
            ["date"] = "2026-06-01",
            ["is_recurring_annually"] = true,
            ["occurrence_date"] = "2026-06-01",
            ["country"] = "Kenya",
        };
        AppendResourceAuditFields(second, "2026-01-10T09:00:00+00:00", "2026-01-10T09:00:00+00:00");
        return new JsonObject
        {
            ["items"] = new JsonArray(PublicHolidayItemData(), second),
        };
    }

    internal static JsonObject CountryItem() => new()
    {
        ["id"] = SampleCountryId,
        ["name"] = "Ghana",
        ["code"] = "GH",
    };

    internal static JsonArray CountryListData() => new JsonArray(CountryItem(), new JsonObject
    {
        ["id"] = "ctr_ke",
        ["name"] = "Kenya",
        ["code"] = "KE",
    });

    internal static JsonObject CountryListResponse() => EnvelopeOk(CountryListData());

    internal static JsonObject CountryGetResponse() => EnvelopeOk(new JsonArray(CountryItem()));

    internal static JsonObject CountryNotFoundResponse() => new()
    {
        ["success"] = false,
        ["status_code"] = 404,
        ["detail"] = "Country not found.",
    };

    internal static JsonObject LeaveHolidayListResponse() =>
        EnvelopeOk(PublicHolidayListData(), LeaveHolidayListPagination());

    internal static JsonObject LeaveHolidayListPagination() => new()
    {
        ["page"] = 1,
        ["size"] = 50,
        ["total"] = 12,
        ["has_next"] = SwaggerExampleHints.BooleanPipe,
        ["page_size"] = 50,
        ["total_count"] = 12,
        ["total_pages"] = 1,
    };

    internal static JsonObject LeaveDeleteRequestResponse() => EnvelopeOk(new JsonObject
    {
        ["leave_request_id"] = SampleLeaveRequestId.ToString(),
    }, detail: "Leave request deleted.");

    internal static JsonObject LeaveDeleteTypeResponse() => EnvelopeOk(new JsonObject
    {
        ["leave_type_id"] = SampleLeaveTypeId.ToString(),
    }, detail: "Leave type deleted.");

    internal static JsonObject LeaveArchiveTypeResponse() => EnvelopeOk(
        LeaveTypeItemData(),
        detail: "Leave type archived.");

    internal static JsonObject LeaveDeleteHolidayResponse() => EnvelopeOk(new JsonObject
    {
        ["holiday_id"] = SampleHolidayId.ToString(),
    }, detail: "Public holiday removed.");

    internal static JsonObject CreateLeaveRequestBody() => new()
    {
        ["employee_id"] = SampleEmployeeId.ToString(),
        ["leave_type_id"] = SampleLeaveTypeId.ToString(),
        ["start_date"] = "2026-07-07",
        ["end_date"] = "2026-07-11",
        ["days_requested"] = 5,
        ["notes"] = "Family visit — advance notice given to manager.",
    };

    internal static JsonObject CreateLeaveRequestMyBody() => new()
    {
        ["leave_type_id"] = SampleLeaveTypeId.ToString(),
        ["start_date"] = "2026-08-04",
        ["end_date"] = "2026-08-08",
        ["days_requested"] = 5,
        ["notes"] = "Personal travel — scoped to the logged-in employee profile.",
    };

    internal static JsonObject UpdateLeaveRequestBody() => new()
    {
        ["status"] = SwaggerExampleHints.LeaveRequestStatus,
        ["notes"] = "Approved after coverage confirmed.",
    };

    internal static JsonObject RejectLeaveRequestBody() => new()
    {
        ["notes"] = "Insufficient team coverage during sprint deadline.",
    };

    internal static JsonObject CreateLeaveBalanceBody() => new()
    {
        ["employee_id"] = SampleEmployeeId.ToString(),
        ["leave_type_id"] = SampleLeaveTypeId.ToString(),
        ["entitled_days"] = 21,
        ["used_days"] = 0,
    };

    internal static JsonObject UpdateLeaveBalanceBody() => new()
    {
        ["entitled_days"] = 25,
        ["used_days"] = 7,
    };

    internal static JsonObject CreateLeaveTypeBody(bool optionHints = false) => new()
    {
        ["name"] = optionHints ? SwaggerExampleHints.LeaveTypeName : "Annual Leave",
        ["default_entitled_days"] = 21,
        ["is_paid"] = optionHints ? SwaggerExampleHints.BooleanPipe : true,
        ["accrual_method"] = optionHints
            ? SwaggerExampleHints.LeaveAccrualMethod
            : LeaveAccrualMethods.FrontLoaded,
        ["carry_over_allowed"] = optionHints ? SwaggerExampleHints.BooleanPipe : false,
        ["applies_to_employment_types"] = optionHints
            ? new JsonArray(SwaggerExampleHints.LeaveTypeEmploymentType)
            : new JsonArray("Full-time", "Part-time", "Contract"),
        ["min_notice_working_days"] = 5,
        ["max_consecutive_days"] = null,
        ["requires_supporting_document"] = optionHints ? SwaggerExampleHints.BooleanPipe : false,
    };

    internal static JsonObject CreateLeaveTypeSickBody() => new()
    {
        ["name"] = "Sick Leave",
        ["default_entitled_days"] = 10,
        ["is_paid"] = true,
        ["accrual_method"] = LeaveAccrualMethods.Monthly,
        ["carry_over_allowed"] = false,
        ["applies_to_employment_types"] = new JsonArray("Full-time"),
        ["min_notice_working_days"] = null,
        ["max_consecutive_days"] = 5,
        ["requires_supporting_document"] = true,
    };

    internal static JsonObject UpdateLeaveTypeBody(bool optionHints = false) =>
        CreateLeaveTypeBody(optionHints);

    internal static JsonObject CreatePublicHolidayBody() => new()
    {
        ["holiday_name"] = "Christmas Day",
        ["date"] = "2026-12-25",
        ["is_recurring_annually"] = true,
        ["country"] = SampleCountryName,
    };

    internal static JsonObject UpdatePublicHolidayBody() => CreatePublicHolidayBody();

    internal static JsonObject ChangeRequestReadDtoData(
        string status = ChangeRequestStatuses.Pending,
        bool includeReview = false,
        string fieldPath = "identity.full_name",
        string? changeRequestId = null)
    {
        var data = new JsonObject
        {
            ["id"] = changeRequestId ?? SampleChangeRequestId.ToString(),
            ["employee_id"] = SampleEmployeeId.ToString(),
            ["field_path"] = fieldPath,
            ["old_value"] = fieldPath switch
            {
                "identity.date_of_birth" => "1990-01-15",
                "compensation.payment" => new JsonObject
                {
                    ["bank_name"] = "GCB Bank",
                    ["account_number"] = "****4521",
                },
                _ => "Ama Mensah",
            },
            ["new_value"] = fieldPath switch
            {
                "identity.date_of_birth" => "1990-01-20",
                "compensation.payment" => new JsonObject
                {
                    ["bank_name"] = "Ecobank Ghana",
                    ["account_number"] = "****8890",
                },
                _ => "Ama Mensah-Osei",
            },
            ["status"] = status,
            ["requested_by_id"] = "cp-user-employee-demo",
            ["requested_by"] = "Ama Mensah",
        };

        if (includeReview)
        {
            data["reviewed_by_id"] = "cp-user-demo-admin";
            data["reviewed_by"] = "Fiifi Boakye";
            data["review_note"] = status == ChangeRequestStatuses.Rejected
                ? "Please submit an official name-change document before we update HR records."
                : null;
        }
        else
        {
            data["reviewed_by_id"] = null;
            data["reviewed_by"] = null;
            data["review_note"] = null;
        }

        AppendResourceAuditFields(
            data,
            "2026-07-10T09:00:00+00:00",
            includeReview ? "2026-07-11T14:30:00+00:00" : "2026-07-10T09:00:00+00:00");
        return data;
    }

    internal static JsonObject ChangeRequestGetResponse() =>
        EnvelopeOk(ChangeRequestReadDtoData());

    internal static JsonObject ChangeRequestListResponse() => EnvelopeOk(new JsonArray(
        ChangeRequestReadDtoData(ChangeRequestStatuses.Pending, fieldPath: "identity.full_name"),
        ChangeRequestReadDtoData(
            ChangeRequestStatuses.Pending,
            fieldPath: "identity.date_of_birth",
            changeRequestId: SampleChangeRequestId2.ToString())),
        SamplePagination());

    internal static JsonObject ChangeRequestRejectedResponse() => EnvelopeOk(
        ChangeRequestReadDtoData(ChangeRequestStatuses.Rejected, includeReview: true),
        detail: "Change request rejected.");

    internal static JsonObject ChangeRequestConflictResponse() => new()
    {
        ["success"] = false,
        ["status_code"] = 409,
        ["detail"] = "Only pending change requests can be approved.",
        ["data"] = null,
        ["field_errors"] = null,
    };

    internal static JsonObject ChangeRequestApproveEmployeeResponse() =>
        EmployeeAggregateReadResponse();

    internal static JsonObject RejectChangeRequestBody() => new()
    {
        ["review_note"] = "Please submit an official name-change document before we update HR records.",
    };

    internal static JsonObject EmployeeSelfUpdateFreeFieldBody() => new()
    {
        ["identity"] = new JsonObject
        {
            ["phone"] = "+233201234567",
            ["personal_email"] = "ama.mensah@personal.example",
        },
    };

    internal static JsonObject EmployeeSelfUpdateApprovalFieldBody() => new()
    {
        ["identity"] = new JsonObject
        {
            ["full_name"] = "Ama Mensah-Osei",
        },
    };

    internal static JsonObject EmployeeSelfUpdateMixedBody() => new()
    {
        ["identity"] = new JsonObject
        {
            ["phone"] = "+233201234567",
            ["full_name"] = "Ama Mensah-Osei",
        },
        ["employment"] = new JsonObject
        {
            ["job_title"] = "Senior Engineer",
        },
    };

    internal static JsonObject EmployeeSelfUpdateResultResponse(bool withPending = true) =>
        EnvelopeOk(EmployeeSelfUpdateResultData(withPending));

    internal static JsonObject EmployeeSelfUpdateResultData(bool withPending = true) => new()
    {
        ["employee"] = EmployeeAggregateReadData().DeepClone(),
        ["applied"] = new JsonArray("identity.phone"),
        ["pending"] = withPending
            ? new JsonArray(ChangeRequestReadDtoData(ChangeRequestStatuses.Pending))
            : new JsonArray(),
        ["rejected"] = withPending
            ? new JsonArray("employment")
            : new JsonArray(),
    };

    internal static JsonArray FieldPolicySampleEntries() => new(
        new JsonObject { ["path"] = "identity.phone", ["access"] = "free" },
        new JsonObject { ["path"] = "identity.full_name", ["access"] = "approval" },
        new JsonObject { ["path"] = "identity.date_of_birth", ["access"] = "approval" },
        new JsonObject { ["path"] = "education", ["access"] = "approval" },
        new JsonObject { ["path"] = "medical.blood_group", ["access"] = "free" });

    internal static JsonObject FieldPolicyListResponse() => EnvelopeOk(FieldPolicySampleEntries());
}
