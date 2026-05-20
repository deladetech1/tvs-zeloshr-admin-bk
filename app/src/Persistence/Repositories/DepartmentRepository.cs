using Microsoft.EntityFrameworkCore;
using ZelosHR.Api.Entities.Departments;
using ZelosHR.Api.Persistence.Entities;

namespace ZelosHR.Api.Persistence.Repositories;

public sealed class DepartmentRepository(ZelosHrDbContext db) : IDepartmentRepository
{
    private IQueryable<DepartmentEntity> Scoped(string tenantId, string orgId) =>
        db.Departments.AsNoTracking()
            .Where(d => d.TenantId == tenantId && d.OrgId == orgId);

    public async Task<OrganisationSummaryDto> GetSummaryScopedAsync(
        string tenantId, string orgId, CancellationToken ct = default)
    {
        var departments = Scoped(tenantId, orgId);
        var branches = db.Branches.AsNoTracking()
            .Where(b => b.TenantId == tenantId && b.OrgId == orgId);

        return new OrganisationSummaryDto
        {
            DepartmentCount = await departments.CountAsync(d => !d.IsArchived, ct),
            BranchCount = await branches.CountAsync(b => !b.IsArchived, ct),
            ArchivedCount = await departments.CountAsync(d => d.IsArchived, ct),
        };
    }

    public async Task<(IReadOnlyList<DepartmentListRow> Items, int Total)> ListScopedAsync(
        string tenantId,
        string orgId,
        string? search,
        string sortBy,
        string sortOrder,
        bool includeArchived,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = Scoped(tenantId, orgId);
        if (!includeArchived)
            query = query.Where(d => !d.IsArchived);

        if (!string.IsNullOrWhiteSpace(search) && search.Trim().Length >= 3)
            query = query.Where(d => EF.Functions.ILike(d.Name, $"%{search.Trim()}%"));

        var total = await query.CountAsync(ct);

        var projected = query.Select(d => new
        {
            d.Id,
            d.Name,
            d.ParentDepartmentId,
            ParentDepartmentName = d.ParentDepartment != null ? d.ParentDepartment.Name : null,
            d.IsArchived,
            HeadId = d.HeadOfDepartmentId,
            HeadFirstName = d.HeadOfDepartment != null ? d.HeadOfDepartment.FirstName : null,
            HeadLastName = d.HeadOfDepartment != null ? d.HeadOfDepartment.LastName : null,
            HeadJobTitle = d.HeadOfDepartment != null ? d.HeadOfDepartment.JobTitle : null,
            EmployeeCount = db.Employees.Count(e =>
                e.DepartmentId == d.Id
                && e.TenantId == tenantId
                && e.OrgId == orgId
                && !e.IsDeleted),
        });

        var byEmployeeCount = sortBy.Equals("employeeCount", StringComparison.OrdinalIgnoreCase)
            || sortBy.Equals("employeecount", StringComparison.OrdinalIgnoreCase);
        var desc = sortOrder.Equals("desc", StringComparison.OrdinalIgnoreCase);

        projected = byEmployeeCount
            ? (desc ? projected.OrderByDescending(x => x.EmployeeCount) : projected.OrderBy(x => x.EmployeeCount))
            : (desc ? projected.OrderByDescending(x => x.Name) : projected.OrderBy(x => x.Name));

        var pageItems = await projected
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var rows = pageItems.Select(x => new DepartmentListRow(
            x.Id,
            x.Name,
            x.ParentDepartmentId,
            x.ParentDepartmentName,
            x.IsArchived,
            x.HeadId,
            x.HeadFirstName,
            x.HeadLastName,
            x.HeadJobTitle,
            x.EmployeeCount)).ToList();

        return (rows, total);
    }
}
