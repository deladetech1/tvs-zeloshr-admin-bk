namespace ZelosHR.Api.Shared.Authorization;

/// <summary>RBAC permission IDs seeded in tvs-sqlscript (02_permissions.sql).</summary>
public static class ZelosHrPermissions
{
    public const string EmployeeGet = "permission-zeloshr-employee-get";
    public const string EmployeeCreate = "permission-zeloshr-employee-create";
    public const string EmployeeUpdate = "permission-zeloshr-employee-update";

    public const string CustomFieldsGet = "permission-zeloshr-custom-fields-get";
    public const string CustomFieldsCreate = "permission-zeloshr-custom-fields-create";
    public const string CustomFieldsUpdate = "permission-zeloshr-custom-fields-update";
    public const string CustomFieldsDelete = "permission-zeloshr-custom-fields-delete";
    public const string CustomFieldsAdmin = "permission-zeloshr-custom-fields-admin";

    public const string CustomFieldValuesGet = "permission-zeloshr-custom-field-values-get";
    public const string CustomFieldValuesCreate = "permission-zeloshr-custom-field-values-create";
    public const string CustomFieldValuesUpdate = "permission-zeloshr-custom-field-values-update";
    public const string CustomFieldValuesDelete = "permission-zeloshr-custom-field-values-delete";
    public const string CustomFieldValuesAdmin = "permission-zeloshr-custom-field-values-admin";

    public const string SensitiveFieldsReveal = "permission-zeloshr-sensitive-fields-reveal";

    public const string LeaveGet = "permission-zeloshr-leave-get";
    public const string LeaveCreate = "permission-zeloshr-leave-create";
    public const string LeaveUpdate = "permission-zeloshr-leave-update";
    public const string LeaveDelete = "permission-zeloshr-leave-delete";
    public const string LeaveAdmin = "permission-zeloshr-leave-admin";
}
