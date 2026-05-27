using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Pagination;

namespace ZelosHR.Api.Entities.Branches;

public class BranchesService
{
    private readonly IBranchRepository _branches;

    public BranchesService(IBranchRepository branches) => _branches = branches;

    public async Task<Respons<BranchListDto>> ListBranchesAsync(
        string tenantId,
        string orgId,
        string? search,
        bool includeArchived,
        int page,
        int size,
        CancellationToken ct = default)
    {
        var paging = PagedQuery.From(page, size);
        var (rows, total) = await _branches.ListPagedScopedAsync(
            tenantId, orgId, search, includeArchived, paging.Page, paging.Size, ct);
        var items = rows.Select(r => new BranchListItemDto
        {
            BranchId = r.Id.ToString(),
            Name = r.Name,
            EmployeeCount = r.EmployeeCount,
            IsArchived = r.IsArchived,
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
