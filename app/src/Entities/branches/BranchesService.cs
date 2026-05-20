using ZelosHR.Api.Entities.Shared;

namespace ZelosHR.Api.Entities.Branches;

public class BranchesService
{
    private readonly IBranchRepository _branches;

    public BranchesService(IBranchRepository branches) => _branches = branches;

    public async Task<Respons<BranchListDto>> ListBranchesAsync(
        string tenantId,
        string orgId,
        bool includeArchived = false,
        CancellationToken ct = default)
    {
        var rows = await _branches.ListScopedAsync(tenantId, orgId, includeArchived, ct);
        var items = rows.Select(r => new BranchListItemDto
        {
            BranchId = r.Id.ToString(),
            Name = r.Name,
            EmployeeCount = r.EmployeeCount,
            IsArchived = r.IsArchived,
        }).ToList();

        return Respons<BranchListDto>.Ok(new BranchListDto { Items = items });
    }
}
