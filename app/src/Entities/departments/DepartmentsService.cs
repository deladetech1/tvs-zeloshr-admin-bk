using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Entities.Files;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Pagination;

namespace ZelosHR.Api.Entities.Departments;

public class DepartmentsService
{
    private readonly IDepartmentRepository _departments;
    private readonly ICpUserRepository _cpUsers;
    private readonly HrDocumentPresignedUrlService _profileUrls;

    public DepartmentsService(
        IDepartmentRepository departments,
        ICpUserRepository cpUsers,
        HrDocumentPresignedUrlService profileUrls)
    {
        _departments = departments;
        _cpUsers = cpUsers;
        _profileUrls = profileUrls;
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

        var users = await _cpUsers.GetByIdsAsync(
            ResourceAuditMapper.CollectUserIds(
                rows.Select(r => new[] { r.CreatedBy, r.UpdatedBy, r.HeadUserId })),
            tenantId,
            ct);

        var profileUrlMap = await DepartmentHeadProfiles.ResolveMapAsync(rows, users, _profileUrls, ct);

        var items = rows.Select(r => new DepartmentListItemDto
        {
            DepartmentId = r.Id.ToString(),
            Name = r.Name,
            Description = r.Description,
            ParentDepartmentId = r.ParentDepartmentId?.ToString(),
            ParentDepartmentName = r.ParentDepartmentName,
            HeadOfDepartment = DepartmentHeadMapper.Map(
                r,
                users,
                DepartmentHeadProfiles.ResolveForRow(r, users, profileUrlMap)),
            EmployeeCount = r.EmployeeCount,
            IsArchived = r.IsArchived,
            HierarchyLevel = r.ParentDepartmentId is null ? 0 : 1,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt,
            CreatedById = r.CreatedBy,
            UpdatedById = r.UpdatedBy,
            CreatedBy = ResourceAuditMapper.ResolveDisplayName(r.CreatedBy, users),
            UpdatedBy = ResourceAuditMapper.ResolveDisplayName(r.UpdatedBy, users),
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
