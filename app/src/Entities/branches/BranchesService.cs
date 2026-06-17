using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Entities.OrgStructure;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Pagination;

namespace ZelosHR.Api.Entities.Branches;

public class BranchesService
{
    private readonly IBranchRepository _branches;
    private readonly ICpUserRepository _cpUsers;

    public BranchesService(IBranchRepository branches, ICpUserRepository cpUsers)
    {
        _branches = branches;
        _cpUsers = cpUsers;
    }

    public async Task<Respons<BranchListDto>> ListBranchesAsync(
        string tenantId,
        string orgId,
        OrgStructureListQuery query,
        CancellationToken ct = default)
    {
        var paging = PagedQuery.From(query.Page, query.Size);
        var (rows, total) = await _branches.ListPagedScopedAsync(
            tenantId,
            orgId,
            query.Search,
            query.SortBy,
            query.SortOrder,
            query.IncludeArchived,
            paging.Page,
            paging.Size,
            ct);

        var users = await _cpUsers.GetByIdsAsync(
            ResourceAuditMapper.CollectUserIds(rows.Select(r => new[] { r.CreatedBy, r.UpdatedBy })),
            tenantId,
            ct);

        var items = rows.Select(r => new BranchListItemDto
        {
            BranchId = r.Id.ToString(),
            Name = r.Name,
            Address = r.Address,
            Country = r.Country,
            Description = r.Description,
            EmployeeCount = r.EmployeeCount,
            IsArchived = r.IsArchived,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt,
            CreatedById = r.CreatedBy,
            UpdatedById = r.UpdatedBy,
            CreatedBy = ResourceAuditMapper.ResolveDisplayName(r.CreatedBy, users),
            UpdatedBy = ResourceAuditMapper.ResolveDisplayName(r.UpdatedBy, users),
        }).ToList();

        return Respons<BranchListDto>.Ok(
            new BranchListDto { Items = items },
            pagination: new PaginationMeta
            {
                Page = paging.Page,
                Size = paging.Size,
                Total = total,
                HasNext = paging.Offset + items.Count < total,
            });
    }
}
