namespace ZelosHR.Api.Entities.Employees;

internal static class EmployeeCodeAllocation
{
    internal const int MaxAttempts = 10;

    internal static string Format(long startSequence, int attemptOffset) =>
        $"ZEL-{startSequence + attemptOffset:D4}";
}
