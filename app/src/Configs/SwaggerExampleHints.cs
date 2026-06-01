using ZelosHR.Api.Entities.CustomFields;
using ZelosHR.Api.Entities.Employees;

namespace ZelosHR.Api.Configs;

/// <summary>
/// Pipe-separated option strings for OpenAPI request examples (sourced from domain allow-lists).
/// Use in operation example JSON so the frontend sees all valid values; real API calls must send one value.
/// </summary>
internal static class SwaggerExampleHints
{
    internal static string PayFrequency => SwaggerOptionFormat.Join(EmployeeFieldOptions.PayFrequencies);
    internal static string Status => SwaggerOptionFormat.Join(EmployeeFieldOptions.CreateStatuses);
    internal static string Gender => SwaggerOptionFormat.Join(EmployeeFieldOptions.Genders);
    internal static string IdType => SwaggerOptionFormat.Join(EmployeeFieldOptions.IdTypes);
    internal static string EmploymentType => SwaggerOptionFormat.Join(EmployeeFieldOptions.EmploymentTypes);
    internal static string EmploymentStatus => SwaggerOptionFormat.Join(EmployeeFieldOptions.EmploymentStatuses);
    internal static string ContractType => SwaggerOptionFormat.Join(EmployeeFieldOptions.ContractTypes);
    internal static string WorkArrangement => SwaggerOptionFormat.Join(EmployeeFieldOptions.WorkArrangements);
    internal static string LifecycleState => SwaggerOptionFormat.Join(EmployeeFieldOptions.LifecycleStatesAll);
    internal static string EntityType => SwaggerOptionFormat.Join(CustomFieldEntityTypes.All);
    internal static string FieldType => SwaggerOptionFormat.Join(CustomFieldFieldTypes.All);
    internal static string SectionName => SwaggerOptionFormat.Join(EmployeeCustomFieldSections.All);
}
