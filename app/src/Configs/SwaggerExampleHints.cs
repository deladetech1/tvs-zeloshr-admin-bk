using ZelosHR.Api.Entities.AuditLogs;
using ZelosHR.Api.Entities.CustomFields;
using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Entities.OrgStructure;

namespace ZelosHR.Api.Configs;

/// <summary>
/// Pipe-separated option strings for OpenAPI request examples (sourced from domain allow-lists).
/// Use in operation example JSON so the frontend sees all valid values; real API calls must send one value.
/// </summary>
internal static class SwaggerExampleHints
{
    internal static string PayFrequency => SwaggerOptionFormat.JoinPipe(EmployeeFieldOptions.PayFrequencies);
    internal static string Status => SwaggerOptionFormat.JoinPipe(EmployeeFieldOptions.CreateStatuses);
    internal static string Gender => SwaggerOptionFormat.JoinPipe(EmployeeFieldOptions.Genders);
    internal static string IdType => SwaggerOptionFormat.JoinPipe(EmployeeFieldOptions.IdTypes);
    internal static string EmploymentType => SwaggerOptionFormat.JoinPipe(EmployeeFieldOptions.EmploymentTypes);
    internal static string EmploymentStatus => SwaggerOptionFormat.JoinPipe(EmployeeFieldOptions.EmploymentStatuses);
    internal static string ListStatusFilter => SwaggerOptionFormat.JoinPipe(EmployeeFieldOptions.ListStatusFilters);
    internal static string ContractType => SwaggerOptionFormat.JoinPipe(EmployeeFieldOptions.ContractTypes);
    internal static string WorkArrangement => SwaggerOptionFormat.JoinPipe(EmployeeFieldOptions.WorkArrangements);
    internal static string LifecycleState => SwaggerOptionFormat.JoinPipe(EmployeeFieldOptions.LifecycleStatesAll);
    internal static string EntityType => SwaggerOptionFormat.JoinPipe(CustomFieldEntityTypes.All);
    internal static string FieldType => SwaggerOptionFormat.JoinPipe(CustomFieldFieldTypes.All);
    internal static string SectionName => SwaggerOptionFormat.JoinPipe(EmployeeCustomFieldSections.All);
    internal static string BooleanPipe => "true|false";
    internal static string SelectOptionsPipe => "[\"option_a|option_b|option_c\"]";
    internal static string EnvelopeSuccessPipe => "true|false";
    internal static string EnvelopeStatusCodePipe => "200|400|404|409|500";
    internal static string EnvelopeDetailPipe => "Success|Validation failed|Not found|Error";
    internal static string EnvelopeStatusCodeErrorPipe => "400|404|409|500";
    internal static string EnvelopeDetailErrorPipe => "Validation failed|Not found|Conflict|Error";
    internal static string OrgDepartmentSortBy => SwaggerOptionFormat.JoinPipe(OrgStructureFieldOptions.DepartmentSortBy);
    internal static string OrgSortOrder => SwaggerOptionFormat.JoinPipe(OrgStructureFieldOptions.SortOrder);
    internal static string OrgNodeType => SwaggerOptionFormat.JoinPipe(OrgStructureFieldOptions.NodeTypes);
    internal static string OrgIncludeArchived => SwaggerOptionFormat.JoinPipe(OrgStructureFieldOptions.IncludeArchived);
    internal static string AuditCategory => SwaggerOptionFormat.JoinPipe(AuditLogFieldOptions.Categories);
    internal static string AuditSeverity => SwaggerOptionFormat.JoinPipe(AuditLogFieldOptions.Severities);
    internal static string AuditFilterAll => SwaggerOptionFormat.JoinPipe(AuditLogFieldOptions.FilterAll);
}
