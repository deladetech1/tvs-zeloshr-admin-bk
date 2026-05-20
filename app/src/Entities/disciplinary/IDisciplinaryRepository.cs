namespace ZelosHR.Api.Entities.Disciplinary;

public interface IDisciplinaryRepository
{
    Task<DisciplinarySummaryDto> GetSummaryScopedAsync(
        string tenantId, string orgId, CancellationToken ct = default);

    Task<(IReadOnlyList<DisciplinaryCaseListItemDto> Items, int Total)> ListScopedAsync(
        string tenantId,
        string orgId,
        string? search,
        string? status,
        string? severity,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<DisciplinaryCaseListItemDto?> GetByIdScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default);

    Task<Guid> CreateScopedAsync(
        string tenantId,
        string orgId,
        Guid employeeId,
        string employeeFullName,
        string caseType,
        string severity,
        string status,
        DateOnly openedAt,
        string? description,
        CancellationToken ct = default);

    Task<DisciplinaryCaseListItemDto?> UpdateScopedAsync(
        Guid id,
        string tenantId,
        string orgId,
        string? caseType,
        string? severity,
        DateOnly? openedAt,
        string? description,
        string? status,
        CancellationToken ct = default);

    Task<bool> DeleteScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default);
}
