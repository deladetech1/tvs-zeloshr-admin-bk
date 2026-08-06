namespace ZelosHR.Api.Shared.Validation;

/// <summary>User-facing copy for employee create/update errors (shown in <c>detail</c> and <c>field_errors</c>).</summary>
public static class EmployeeErrorMessages
{
    public const string EmployeeCodeAllocationFailed =
        "We could not assign a new employee number (for example ZEL-0005). "
        + "Wait a few seconds and submit again. If this keeps happening, contact support.";

    public const string EmployeeCustomCodeAlreadyExists =
        "This employee_code_custom is already assigned to another employee in your organisation.";

    public const string WorkEmailUsedByAnotherOrganisation =
        "This work email is already used by someone in another organisation. "
        + "Use a different work email, or ask your administrator for help.";

    public const string WorkEmailLinkedToAnotherEmployee =
        "This work email is already linked to another employee in your organisation.";

    public const string WorkEmailAlreadyRegistered =
        "This work email is already registered on Trove. Use a different email or link the existing user.";

    public const string PhoneAlreadyRegistered =
        "This phone number is already registered on Trove. Use a different number or update the existing user.";

    public const string UserAlreadyLinkedToEmployee =
        "This platform user is already linked to an employee in your organisation.";
}
