using Microsoft.EntityFrameworkCore;
using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Persistence.Entities;

namespace ZelosHR.Api.Persistence.Repositories;

public sealed class EmployeeDirectoryRepository(ZelosHrDbContext db) : IEmployeeDirectoryRepository
{
    private IQueryable<EmployeeEntity> Scoped(string tenantId, string orgId) =>
        db.Employees.AsNoTracking()
            .Where(e => e.TenantId == tenantId && e.OrgId == orgId && !e.IsDeleted);

    public async Task<EmployeeDirectorySummaryDto> GetSummaryScopedAsync(
        string tenantId, string orgId, CancellationToken ct = default)
    {
        var query = Scoped(tenantId, orgId);
        return new EmployeeDirectorySummaryDto
        {
            TotalEmployees = await query.CountAsync(ct),
            ActiveEmployees = await query.CountAsync(e => e.EmploymentStatus == "Active", ct),
            OnProbation = await query.CountAsync(e => e.EmploymentStatus == "Probation", ct),
            OnContract = await query.CountAsync(
                e => e.EmploymentType == "Contractor" || e.ContractType == "Fixed-term", ct),
        };
    }

    public async Task<(IReadOnlyList<EmployeeDirectoryListRow> Items, int Total)> ListScopedAsync(
        EmployeeDirectoryQuery query,
        string tenantId,
        string orgId,
        CancellationToken ct = default)
    {
        var baseQuery = EmployeeDirectoryQueryBuilder.ApplyFilters(Scoped(tenantId, orgId), query);
        var total = await baseQuery.CountAsync(ct);

        var sorted = EmployeeDirectoryQueryBuilder.ApplySort(baseQuery, query.SortBy, query.SortOrder);
        var page = Math.Max(1, query.Page);
        var size = Math.Clamp(query.Size, 1, 100);

        var rows = await sorted
            .Skip((page - 1) * size)
            .Take(size)
            .Select(e => new EmployeeDirectoryListRow(
                e.Id,
                e.EmployeeCode,
                e.UserId,
                e.FullName,
                e.FirstName,
                e.MiddleName,
                e.LastName,
                e.JobTitle,
                e.DepartmentId,
                e.Department != null ? e.Department.Name : null,
                e.BranchId,
                e.Branch != null ? e.Branch.Name : null,
                e.EmploymentType,
                e.ManagerId,
                e.Manager != null ? e.Manager.UserId : null,
                e.Manager != null ? e.Manager.FirstName : null,
                e.Manager != null ? e.Manager.LastName : null,
                e.EmploymentStatus))
            .ToListAsync(ct);

        return (rows, total);
    }
}
