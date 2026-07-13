using Microsoft.EntityFrameworkCore;
using ZelosHR.Api.Persistence.Entities;

namespace ZelosHR.Api.Entities.Employees;

/// <summary>
/// Derived role flags: line manager (has qualifying direct reports) and head of department.
/// Source of truth: <c>reports_to_id</c> on subordinates and <c>head_of_department_id</c> on departments.
/// </summary>
public static class EmployeeRoleFlags
{
    public static IQueryable<EmployeeEntity> ApplyFilters(
        IQueryable<EmployeeEntity> query,
        bool? isLineManager,
        bool? isHeadOfDepartment,
        IQueryable<EmployeeEntity> employees,
        IQueryable<DepartmentEntity> departments,
        string tenantId,
        string orgId)
    {
        if (isLineManager is true)
        {
            query = query.Where(e => HasQualifyingDirectReport(employees, e.Id, tenantId, orgId));
        }
        else if (isLineManager is false)
        {
            query = query.Where(e => !HasQualifyingDirectReport(employees, e.Id, tenantId, orgId));
        }

        if (isHeadOfDepartment is true)
        {
            query = query.Where(e => HeadsActiveDepartment(departments, e.Id, tenantId, orgId));
        }
        else if (isHeadOfDepartment is false)
        {
            query = query.Where(e => !HeadsActiveDepartment(departments, e.Id, tenantId, orgId));
        }

        return query;
    }

    public static void AppendSqlFilters(
        List<string> conditions,
        bool? isLineManager,
        bool? isHeadOfDepartment,
        string employeesTable,
        string departmentsTable)
    {
        if (isLineManager is true)
        {
            conditions.Add(
                $"""
                 EXISTS (
                     SELECT 1 FROM {employeesTable} r
                     WHERE r.reports_to_id = e.id
                       AND r.tenant_id = e.tenant_id
                       AND r.org_id = e.org_id
                       AND r.is_deleted = FALSE
                       AND r.is_draft = FALSE
                 )
                 """);
        }
        else if (isLineManager is false)
        {
            conditions.Add(
                $"""
                 NOT EXISTS (
                     SELECT 1 FROM {employeesTable} r
                     WHERE r.reports_to_id = e.id
                       AND r.tenant_id = e.tenant_id
                       AND r.org_id = e.org_id
                       AND r.is_deleted = FALSE
                       AND r.is_draft = FALSE
                 )
                 """);
        }

        if (isHeadOfDepartment is true)
        {
            conditions.Add(
                $"""
                 EXISTS (
                     SELECT 1 FROM {departmentsTable} d
                     WHERE d.head_of_department_id = e.id
                       AND d.tenant_id = e.tenant_id
                       AND d.org_id = e.org_id
                       AND d.is_archived = FALSE
                 )
                 """);
        }
        else if (isHeadOfDepartment is false)
        {
            conditions.Add(
                $"""
                 NOT EXISTS (
                     SELECT 1 FROM {departmentsTable} d
                     WHERE d.head_of_department_id = e.id
                       AND d.tenant_id = e.tenant_id
                       AND d.org_id = e.org_id
                       AND d.is_archived = FALSE
                 )
                 """);
        }
    }

    private static bool HasQualifyingDirectReport(
        IQueryable<EmployeeEntity> employees,
        Guid managerId,
        string tenantId,
        string orgId) =>
        employees.Any(r =>
            r.ReportsToId == managerId
            && r.TenantId == tenantId
            && r.OrgId == orgId
            && !r.IsDeleted
            && !r.IsDraft);

    private static bool HeadsActiveDepartment(
        IQueryable<DepartmentEntity> departments,
        Guid employeeId,
        string tenantId,
        string orgId) =>
        departments.Any(d =>
            d.HeadOfDepartmentId == employeeId
            && d.TenantId == tenantId
            && d.OrgId == orgId
            && !d.IsArchived);
}

public sealed record EmployeeRoleFlagsBatch(
    IReadOnlySet<Guid> LineManagerIds,
    IReadOnlySet<Guid> HeadOfDepartmentIds)
{
    public static EmployeeRoleFlagsBatch Empty { get; } =
        new(new HashSet<Guid>(), new HashSet<Guid>());

    public bool IsLineManager(Guid employeeId) => LineManagerIds.Contains(employeeId);

    public bool IsHeadOfDepartment(Guid employeeId) => HeadOfDepartmentIds.Contains(employeeId);
}
