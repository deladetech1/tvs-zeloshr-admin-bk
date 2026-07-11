using ZelosHR.Api.Persistence.Entities;

namespace ZelosHR.Api.Entities.Employees;

/// <summary>Mirrors <c>employment_status</c> from the frontend into lifecycle columns — no server-side overrides.</summary>
internal static class EmployeeLifecycleSync
{
    internal static void SyncFromEmploymentStatus(EmployeeEntity entity, string employmentStatus)
    {
        if (string.IsNullOrWhiteSpace(employmentStatus))
            return;

        entity.EmploymentStatus = employmentStatus.Trim();
        var (lifecycleState, lifecycleStatus) = MapEmploymentStatus(entity.EmploymentStatus);
        entity.LifecycleState = lifecycleState;
        entity.LifecycleStatus = lifecycleStatus;
    }

    internal static (string LifecycleState, string LifecycleStatus) MapEmploymentStatus(string status)
    {
        if (string.Equals(status, EmploymentStatusValues.Draft, StringComparison.OrdinalIgnoreCase))
            return (EmployeeLifecycleStates.Draft, "draft");

        if (string.Equals(status, EmploymentStatusValues.PreHire, StringComparison.OrdinalIgnoreCase))
            return (EmployeeLifecycleStates.PreHire, "pre_hire");

        if (string.Equals(status, EmploymentStatusValues.Active, StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, EmploymentStatusValues.Probation, StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, EmploymentStatusValues.Inactive, StringComparison.OrdinalIgnoreCase))
            return (EmployeeLifecycleStates.Active, "active");

        if (string.Equals(status, EmploymentStatusValues.OnLeave, StringComparison.OrdinalIgnoreCase))
            return (EmployeeLifecycleStates.OnLeave, "on_leave");

        if (string.Equals(status, EmploymentStatusValues.Suspended, StringComparison.OrdinalIgnoreCase))
            return (EmployeeLifecycleStates.Suspended, "suspended");

        if (string.Equals(status, EmploymentStatusValues.Resigned, StringComparison.OrdinalIgnoreCase))
            return (EmployeeLifecycleStates.Resigned, "resigned");

        if (string.Equals(status, EmploymentStatusValues.Terminated, StringComparison.OrdinalIgnoreCase))
            return (EmployeeLifecycleStates.Terminated, "terminated");

        var trimmed = status.Trim();
        return (trimmed, ToLifecycleStatus(trimmed));
    }

    private static string ToLifecycleStatus(string status) =>
        status.Replace('-', '_').Replace(' ', '_').ToLowerInvariant();
}
