namespace ZelosHR.Api.Entities.Employees.Authorization;

/// <summary>
/// What an employee may do with a field. Admins implicitly have full edit on everything;
/// only the employee side needs a map.
/// </summary>
public enum FieldAccess
{
    None,
    Free,
    Approval,
}
