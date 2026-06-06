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
        BranchWriteModel model,
        string tenantId,
        string orgId,
        CancellationToken ct = default);

    Task<BranchListRow?> GetActiveScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default);

    Task<BranchListRow?> UpdateScopedAsync(
        Guid id,
        string tenantId,
        string orgId,
        string? name,
        string? city,
        string? region,
        string? countryCode,
        bool updateName,
        bool updateCity,
        bool updateRegion,
        bool updateCountryCode,
        CancellationToken ct = default);

    Task<bool> ArchiveScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default);

    Task<bool> ExistsActiveScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default);
}

public sealed record BranchWriteModel(
    string Name,
    string? City,
    string? Region,
    string? CountryCode);

public sealed record BranchListRow(
    Guid Id,
    string Name,
    string? City,
    string? Region,
    string? CountryCode,
    int EmployeeCount,
    bool IsArchived);
