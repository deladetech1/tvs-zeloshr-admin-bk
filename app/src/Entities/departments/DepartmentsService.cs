using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Formatting;
using ZelosHR.Api.Shared.Pagination;

namespace ZelosHR.Api.Entities.Departments;

public class DepartmentsService
{
    private readonly IDepartmentRepository _departments;

    public DepartmentsService(IDepartmentRepository departments)
    {
        _departments = departments;
    }

    public async Task<Respons<OrganisationSummaryDto>> GetSummaryAsync(
        string tenantId,
        string orgId,
        CancellationToken ct = default)
    {
        var summary = await _departments.GetSummaryScopedAsync(tenantId, orgId, ct);
        return Respons<OrganisationSummaryDto>.Ok(summary);
    }

    public async Task<Respons<DepartmentListDto>> ListDepartmentsAsync(
        string? search,
        string sortBy,
        string sortOrder,
        bool includeArchived,
        int page,
        int size,
        string tenantId,
        string orgId,
        CancellationToken ct = default)
    {
        var paging = PagedQuery.From(page, size);
        var (rows, total) = await _departments.ListScopedAsync(
            tenantId, orgId, search, sortBy, sortOrder, includeArchived, paging.Page, paging.Size, ct);

        var items = rows.Select(r => new DepartmentListItemDto
        {
            DepartmentId = r.Id.ToString(),
            Name = r.Name,
            ParentDepartmentId = r.ParentDepartmentId?.ToString(),
            ParentDepartmentName = r.ParentDepartmentName,
            HeadOfDepartment = r.HeadId is null
                ? null
                : new DepartmentHeadDto
                {
                    EmployeeId = r.HeadId.Value.ToString(),
                    FullName = NameFormatting.BuildFullName(r.HeadFirstName!, null, r.HeadLastName!),
                    JobTitle = r.HeadJobTitle,
                    Initials = NameFormatting.BuildInitials(r.HeadFirstName!, r.HeadLastName!),
                },
            EmployeeCount = r.EmployeeCount,
            IsArchived = r.IsArchived,
            HierarchyLevel = r.ParentDepartmentId is null ? 0 : 1,
        }).ToList();

        var summary = await _departments.GetSummaryScopedAsync(tenantId, orgId, ct);

        return Respons<DepartmentListDto>.Ok(
            new DepartmentListDto
            {
                Summary = summary,
                Items = items,
                ShowingLabel = $"Showing {Math.Min(paging.Offset + items.Count, total)} of {total} departments",
            },
            pagination: new PaginationMeta
            {
                Page = paging.Page,
                Size = paging.Size,
                Total = total,
                HasNext = paging.Offset + items.Count < total,
            });
    }
}
