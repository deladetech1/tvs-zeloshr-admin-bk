namespace ZelosHR.Api.Shared.Authorization;

/// <summary>RBAC permission IDs seeded in tvs-sqlscript (02_permissions.sql).</summary>
public static class ZelosHrPermissions
{
    public const string EmployeeGet = "permission-zeloshr-employee-get";
    public const string EmployeeCreate = "permission-zeloshr-employee-create";
    public const string EmployeeUpdate = "permission-zeloshr-employee-update";
}
