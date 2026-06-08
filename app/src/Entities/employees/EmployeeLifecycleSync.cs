using ZelosHR.Api.Persistence.Entities;

namespace ZelosHR.Api.Entities.Employees;

/// <summary>Keeps lifecycle_state / lifecycle_status aligned with employment_status after finalise.</summary>
internal static class EmployeeLifecycleSync
{
    internal static void ApplyPostFinaliseDefaults(EmployeeEntity entity)
    {
        if (ShouldDefaultToPreHire(entity.EmploymentStatus))
        {
            SetPreHire(entity);
            return;
        }

        ApplyFromEmploymentStatus(entity, entity.EmploymentStatus.Trim());
    }

    internal static bool ShouldDefaultToPreHire(string? employmentStatus) =>
        string.IsNullOrWhiteSpace(employmentStatus)
        || string.Equals(employmentStatus, EmploymentStatusValues.Draft, StringComparison.OrdinalIgnoreCase);

    internal static void ApplyFromEmploymentStatus(EmployeeEntity entity, string employmentStatus)
    {
        entity.EmploymentStatus = employmentStatus.Trim();
        var (lifecycleState, lifecycleStatus) = MapEmploymentStatus(entity.EmploymentStatus);
        entity.LifecycleState = lifecycleState;
        entity.LifecycleStatus = lifecycleStatus;
    }

    private static void SetPreHire(EmployeeEntity entity)
    {
        entity.LifecycleStatus = "pre_hire";
        entity.LifecycleState = EmployeeLifecycleStates.PreHire;
        entity.EmploymentStatus = EmploymentStatusValues.PreHire;
    }

    private static (string LifecycleState, string LifecycleStatus) MapEmploymentStatus(string status)
    {
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

        return (EmployeeLifecycleStates.PreHire, "pre_hire");
    }
}
