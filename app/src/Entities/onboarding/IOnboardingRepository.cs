namespace ZelosHR.Api.Entities.Onboarding;

public interface IOnboardingRepository
{
    Task<OnboardingSummaryDto> GetSummaryScopedAsync(
        string tenantId, string orgId, CancellationToken ct = default);

    Task<(IReadOnlyList<OnboardingTaskListItemDto> Items, int Total)> ListScopedAsync(
        string tenantId,
        string orgId,
        string? search,
        string? status,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<OnboardingTaskListItemDto?> GetByIdScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default);

    Task<Guid> CreateScopedAsync(
        string tenantId,
        string orgId,
        Guid employeeId,
        string employeeFullName,
        string taskName,
        string category,
        DateOnly dueDate,
        string status,
        string? assignedTo,
        CancellationToken ct = default);

    Task<OnboardingTaskListItemDto?> UpdateScopedAsync(
        Guid id,
        string tenantId,
        string orgId,
        string? taskName,
        string? category,
        DateOnly? dueDate,
        string? status,
        string? assignedTo,
        CancellationToken ct = default);

    Task<bool> DeleteScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default);
}
