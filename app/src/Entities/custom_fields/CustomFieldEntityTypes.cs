namespace ZelosHR.Api.Entities.CustomFields;

/// <summary>Values for <c>entity_type</c> on <c>zhr_custom_field_definitions</c>.</summary>
public static class CustomFieldEntityTypes
{
    public const string Employee = "employee";
    public const string Department = "department";
    public const string Branch = "branch";
    public const string LifecycleEvent = "lifecycle_event";
    public const string EmployeeDocument = "employee_document";

    public static readonly IReadOnlyList<string> All =
    [
        Employee,
        Department,
        Branch,
        LifecycleEvent,
        EmployeeDocument,
    ];
}
