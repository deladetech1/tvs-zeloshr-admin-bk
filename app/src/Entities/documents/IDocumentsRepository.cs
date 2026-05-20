namespace ZelosHR.Api.Entities.Documents;

public interface IDocumentsRepository
{
    Task<DocumentsSummaryDto> GetSummaryScopedAsync(
        string tenantId, string orgId, CancellationToken ct = default);

    Task<(IReadOnlyList<DocumentListItemDto> Items, int Total)> ListScopedAsync(
        string tenantId,
        string orgId,
        string? search,
        string? category,
        Guid? employeeId,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<DocumentListItemDto?> GetByIdScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default);

    Task<Guid> CreateScopedAsync(
        string tenantId,
        string orgId,
        Guid employeeId,
        string employeeFullName,
        string documentName,
        string category,
        int fileSizeKb,
        string uploadedBy,
        string status,
        CancellationToken ct = default);

    Task<DocumentListItemDto?> UpdateScopedAsync(
        Guid id,
        string tenantId,
        string orgId,
        string? documentName,
        string? category,
        int? fileSizeKb,
        string? uploadedBy,
        string? status,
        CancellationToken ct = default);

    Task<bool> DeleteScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default);
}
