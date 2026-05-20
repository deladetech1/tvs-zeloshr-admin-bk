namespace ZelosHR.Api.Entities.LifecycleEvents;

public interface ILifecycleEventRepository
{
    Task<LifecycleEventSummaryDto> GetSummaryScopedAsync(
        string tenantId, string orgId, CancellationToken ct = default);

    Task<(IReadOnlyList<LifecycleEventListItemDto> Items, int Total)> ListScopedAsync(
        string tenantId,
        string orgId,
        string? search,
        string? eventType,
        string? urgency,
        string? department,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<LifecycleEventListItemDto?> GetByIdScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default);

    Task<Guid> CreateScopedAsync(
        string tenantId,
        string orgId,
        Guid employeeId,
        string employeeFullName,
        string eventType,
        string? departmentName,
        string? branchName,
        DateOnly dueDate,
        string status,
        string urgency,
        CancellationToken ct = default);

    Task<LifecycleEventListItemDto?> UpdateScopedAsync(
        Guid id,
        string tenantId,
        string orgId,
        string? eventType,
        DateOnly? dueDate,
        string? status,
        string? urgency,
        CancellationToken ct = default);

    Task<bool> DeleteScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default);
}
