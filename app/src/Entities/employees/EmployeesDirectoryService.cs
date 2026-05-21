using ZelosHR.Api.Entities.Departments;
using ZelosHR.Api.Entities.Branches;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Formatting;
using ZelosHR.Api.Shared.Pagination;

namespace ZelosHR.Api.Entities.Employees;

public class EmployeesDirectoryService
{
    private readonly IEmployeeDirectoryRepository _directory;
    private readonly IDepartmentRepository _departments;
    private readonly IBranchRepository _branches;

    public EmployeesDirectoryService(
        IEmployeeDirectoryRepository directory,
        IDepartmentRepository departments,
        IBranchRepository branches)
    {
        _directory = directory;
        _departments = departments;
        _branches = branches;
    }

    public async Task<Respons<EmployeeDirectorySummaryDto>> GetSummaryAsync(
        string tenantId,
        string orgId,
        CancellationToken ct = default)
    {
        var summary = await _directory.GetSummaryScopedAsync(tenantId, orgId, ct);
        return Respons<EmployeeDirectorySummaryDto>.Ok(summary);
    }

    public async Task<Respons<EmployeeDirectoryListDto>> GetDirectoryAsync(
        EmployeeDirectoryQuery query,
        string tenantId,
        string orgId,
        CancellationToken ct = default)
    {
        var paging = PagedQuery.From(query.Page, query.Size);
        var (rows, total) = await _directory.ListScopedAsync(query, tenantId, orgId, ct);

        var items = rows.Select(MapRow).ToList();
        var summary = await _directory.GetSummaryScopedAsync(tenantId, orgId, ct);

        var list = new EmployeeDirectoryListDto
        {
            Summary = summary,
            Items = items,
            EmptyMessage = items.Count == 0
                ? "No employees found matching your search"
                : null,
        };

        return Respons<EmployeeDirectoryListDto>.Ok(
            list,
            pagination: new PaginationMeta
            {
                Page = paging.Page,
                Size = paging.Size,
                Total = total,
                HasNext = paging.Offset + items.Count < total,
            });
    }

    public async Task<Respons<EmployeeFilterOptionsDto>> GetFilterOptionsAsync(
        string tenantId,
        string orgId,
        CancellationToken ct = default)
    {
        var deptRows = await _departments.ListScopedAsync(
            tenantId, orgId, null, "name", "asc", includeArchived: false, page: 1, pageSize: 500, ct);
        var branchRows = await _branches.ListScopedAsync(tenantId, orgId, includeArchived: false, ct);

        return Respons<EmployeeFilterOptionsDto>.Ok(new EmployeeFilterOptionsDto
        {
            Departments = deptRows.Items
                .Select(d => new FilterOptionDto { Id = d.Id.ToString(), Name = d.Name })
                .ToList(),
            Branches = branchRows
                .Where(b => !b.IsArchived)
                .Select(b => new FilterOptionDto { Id = b.Id.ToString(), Name = b.Name })
                .ToList(),
            EmploymentTypes = ["Full-time", "Part-time", "Contractor", "Casual"],
            Statuses = ["Active", "Probation", "On Leave", "Suspended", "Resigned", "Terminated"],
        });
    }

    private static EmployeeDirectoryItemDto MapRow(EmployeeDirectoryListRow row) =>
        new()
        {
            EmployeeId = row.Id.ToString(),
            EmployeeCode = row.EmployeeCode,
            FullName = NameFormatting.ResolveFullName(row.FullName, row.FirstName, row.MiddleName, row.LastName),
            Initials = NameFormatting.BuildInitials(
                row.FirstName ?? row.FullName,
                row.LastName ?? string.Empty),
            JobTitle = row.JobTitle,
            DepartmentId = row.DepartmentId?.ToString(),
            DepartmentName = row.DepartmentName,
            BranchId = row.BranchId?.ToString(),
            BranchName = row.BranchName,
            EmploymentType = row.EmploymentType,
            ManagerId = row.ManagerId?.ToString(),
            ManagerName = row.ManagerFirstName is null
                ? null
                : $"{row.ManagerFirstName} {row.ManagerLastName}".Trim(),
            Status = row.Status,
        };
}
