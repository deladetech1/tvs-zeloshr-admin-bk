namespace ZelosHR.Api.Entities.Dashboard;

public interface IDashboardRepository
{
    Task<DashboardSummaryDto> GetSummaryScopedAsync(
        string tenantId, string orgId, CancellationToken ct = default);

    Task<IReadOnlyList<DashboardActivityItemDto>> GetRecentActivityScopedAsync(
        string tenantId, string orgId, int limit = 8, CancellationToken ct = default);
}
