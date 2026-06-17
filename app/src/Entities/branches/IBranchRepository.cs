using ZelosHR.Api.Entities.OrgStructure;

namespace ZelosHR.Api.Entities.Branches;

public interface IBranchRepository
{
    Task<IReadOnlyList<BranchListRow>> ListScopedAsync(
        string tenantId, string orgId, bool includeArchived, CancellationToken ct = default);

    Task<(IReadOnlyList<BranchListRow> Items, int Total)> ListPagedScopedAsync(
        string tenantId,
        string orgId,
        string? search,
        string sortBy,
        string sortOrder,
        bool includeArchived,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<Guid> CreateScopedAsync(
        BranchWriteModel model,
        string tenantId,
        string orgId,
        string? actedBy,
        CancellationToken ct = default);

    Task<BranchListRow?> GetActiveScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default);

    Task<BranchListRow?> UpdateScopedAsync(
        Guid id,
        string tenantId,
        string orgId,
        string? name,
        string? address,
        string? country,
        string? description,
        bool updateName,
        bool updateAddress,
        bool updateCountry,
        bool updateDescription,
        string? actedBy,
        CancellationToken ct = default);

    Task<OrgStructureDeleteResult> DeleteScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default);

    Task<bool> ExistsActiveScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default);
}

public sealed record BranchWriteModel(
    string Name,
    string? Address,
    string? Country,
    string? Description);

public sealed record BranchListRow(
    Guid Id,
    string Name,
    string? Address,
    string? Country,
    string? Description,
    int EmployeeCount,
    bool IsArchived,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string? CreatedBy,
    string? UpdatedBy);
