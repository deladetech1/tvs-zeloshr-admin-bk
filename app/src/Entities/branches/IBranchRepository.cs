namespace ZelosHR.Api.Entities.Branches;

public interface IBranchRepository
{
    Task<IReadOnlyList<BranchListRow>> ListScopedAsync(
        string tenantId, string orgId, bool includeArchived, CancellationToken ct = default);

    Task<(IReadOnlyList<BranchListRow> Items, int Total)> ListPagedScopedAsync(
        string tenantId,
        string orgId,
        string? search,
        bool includeArchived,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<Guid> CreateScopedAsync(
        string tenantId, string orgId, string name, CancellationToken ct = default);

    Task<string?> UpdateNameScopedAsync(
        Guid id, string tenantId, string orgId, string name, CancellationToken ct = default);

    Task<bool> ArchiveScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default);

    Task<bool> ExistsActiveScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default);
}

public sealed record BranchListRow(Guid Id, string Name, int EmployeeCount, bool IsArchived);
