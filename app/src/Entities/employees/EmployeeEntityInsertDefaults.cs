using ZelosHR.Api.Persistence.Entities;

namespace ZelosHR.Api.Entities.Employees;

/// <summary>
/// Ensures <c>zhr_employees</c> NOT NULL columns are populated before EF insert.
/// Optional API fields (job_title, department_id, branch_id, work_arrangement) stay null when omitted.
/// </summary>
internal static class EmployeeEntityInsertDefaults
{
    /// <summary>Columns with NOT NULL in tvs-sqlscript (no nullable employment/org fields).</summary>
    internal static void EnsureRequiredColumns(EmployeeEntity entity)
    {
        entity.EmployeeCode = string.IsNullOrWhiteSpace(entity.EmployeeCode)
            ? throw new InvalidOperationException("EmployeeCode must be set before insert.")
            : entity.EmployeeCode.Trim();

        entity.TenantId = string.IsNullOrWhiteSpace(entity.TenantId)
            ? throw new InvalidOperationException("TenantId must be set before insert.")
            : entity.TenantId;

        entity.OrgId = string.IsNullOrWhiteSpace(entity.OrgId)
            ? throw new InvalidOperationException("OrgId must be set before insert.")
            : entity.OrgId;

        entity.FullName ??= string.Empty;
        entity.LifecycleState = string.IsNullOrWhiteSpace(entity.LifecycleState)
            ? EmployeeLifecycleStates.Draft
            : entity.LifecycleState;
        entity.LifecycleStatus = string.IsNullOrWhiteSpace(entity.LifecycleStatus)
            ? "draft"
            : entity.LifecycleStatus;
        entity.EmploymentStatus = string.IsNullOrWhiteSpace(entity.EmploymentStatus)
            ? EmploymentStatusValues.Draft
            : entity.EmploymentStatus;
        entity.CustomFieldsData = string.IsNullOrWhiteSpace(entity.CustomFieldsData)
            ? "{}"
            : entity.CustomFieldsData;
        entity.DocumentIds ??= [];
    }
}
