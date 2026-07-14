namespace ZelosHR.Api.Entities.Employees;

/// <summary>
/// Employment statuses that receive a sign-in invite when an employee record is finalised.
/// </summary>
internal static class EmployeeOnboardingInviteEligibility
{
    private static readonly HashSet<string> EligibleStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        EmploymentStatusValues.PreHire,
        EmploymentStatusValues.Probation,
        EmploymentStatusValues.Active,
        EmploymentStatusValues.OnLeave,
    };

    internal static bool IsEligible(string? employmentStatus) =>
        !string.IsNullOrWhiteSpace(employmentStatus)
        && EligibleStatuses.Contains(employmentStatus.Trim());
}
