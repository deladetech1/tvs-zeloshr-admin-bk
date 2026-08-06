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
        entity.EmployeeCodeSystem = string.IsNullOrWhiteSpace(entity.EmployeeCodeSystem)
            ? throw new InvalidOperationException("EmployeeCodeSystem must be set before insert.")
            : entity.EmployeeCodeSystem.Trim();
        entity.EmployeeCodeCustom = EmployeeCodeResolver.NormalizeCustom(entity.EmployeeCodeCustom);

        entity.TenantId = string.IsNullOrWhiteSpace(entity.TenantId)
            ? throw new InvalidOperationException("TenantId must be set before insert.")
            : entity.TenantId;

        entity.OrgId = string.IsNullOrWhiteSpace(entity.OrgId)
            ? throw new InvalidOperationException("OrgId must be set before insert.")
            : entity.OrgId;

        entity.FullName ??= string.Empty;

        if (string.IsNullOrWhiteSpace(entity.LifecycleState))
            throw new InvalidOperationException("LifecycleState must be set before insert.");

        if (string.IsNullOrWhiteSpace(entity.LifecycleStatus))
            throw new InvalidOperationException("LifecycleStatus must be set before insert.");

        entity.CustomFieldsData = string.IsNullOrWhiteSpace(entity.CustomFieldsData)
            ? "{}"
            : entity.CustomFieldsData;
        entity.DocumentIds ??= [];
    }
}
