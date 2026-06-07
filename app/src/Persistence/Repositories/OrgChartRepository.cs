using Microsoft.EntityFrameworkCore;
using ZelosHR.Api.Entities.OrgStructure;

namespace ZelosHR.Api.Persistence.Repositories;

public sealed class OrgChartRepository(ZelosHrDbContext db) : IOrgChartRepository
{
    public async Task<(IReadOnlyList<OrgChartEmployeeRow> Employees, IReadOnlyList<OrgChartDepartmentHeadRow> DepartmentHeads)>
        GetReportingHierarchyScopedAsync(string tenantId, string orgId, CancellationToken ct = default)
    {
        var employees = await db.Employees.AsNoTracking()
            .Where(e => e.TenantId == tenantId && e.OrgId == orgId && !e.IsDeleted && !e.IsDraft)
            .OrderBy(e => e.FullName)
            .Select(e => new OrgChartEmployeeRow(
                e.Id,
                e.FullName,
                e.FirstName,
                e.LastName,
                e.JobTitle,
                e.ReportsToId,
                e.UserId,
                e.ProfilePhotoUrl))
            .ToListAsync(ct);

        var departmentHeads = await db.Departments.AsNoTracking()
            .Where(d => d.TenantId == tenantId && d.OrgId == orgId && !d.IsArchived && d.HeadOfDepartmentId != null)
            .Select(d => new OrgChartDepartmentHeadRow(
                d.Id,
                d.Name,
                d.HeadOfDepartmentId!.Value,
                db.Employees.Count(e =>
                    e.DepartmentId == d.Id
                    && e.TenantId == tenantId
                    && e.OrgId == orgId
                    && !e.IsDeleted
                    && !e.IsDraft),
                d.HeadcountCapacity))
            .ToListAsync(ct);

        return (employees, departmentHeads);
    }
}
