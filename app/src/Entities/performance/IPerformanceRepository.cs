namespace ZelosHR.Api.Entities.Performance;

public interface IPerformanceRepository
{
    Task<PerformanceSummaryDto> GetSummaryScopedAsync(
        string tenantId, string orgId, CancellationToken ct = default);

    Task<(IReadOnlyList<PerformanceReviewListItemDto> Items, int Total)> ListScopedAsync(
        string tenantId,
        string orgId,
        string? search,
        string? status,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<PerformanceReviewListItemDto?> GetByIdScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default);

    Task<Guid> CreateScopedAsync(
        string tenantId,
        string orgId,
        Guid employeeId,
        string employeeFullName,
        string reviewPeriod,
        string? reviewerName,
        string status,
        DateOnly dueDate,
        CancellationToken ct = default);

    Task<PerformanceReviewListItemDto?> UpdateScopedAsync(
        Guid id,
        string tenantId,
        string orgId,
        string? reviewPeriod,
        DateOnly? dueDate,
        string? reviewerName,
        string? overallRating,
        string? status,
        CancellationToken ct = default);

    Task<bool> DeleteScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default);
}
