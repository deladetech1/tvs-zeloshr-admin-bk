using ZelosHR.Api.Entities.Shared;

namespace ZelosHR.Api.Entities.Dashboard;

public class DashboardService
{
    private readonly IDashboardRepository _dashboard;

    public DashboardService(IDashboardRepository dashboard) => _dashboard = dashboard;

    public async Task<Respons<DashboardDto>> GetDashboardAsync(
        string tenantId, string orgId, CancellationToken ct = default)
    {
        var summary = await _dashboard.GetSummaryScopedAsync(tenantId, orgId, ct);
        var activity = await _dashboard.GetRecentActivityScopedAsync(tenantId, orgId, 8, ct);

        return Respons<DashboardDto>.Ok(new DashboardDto
        {
            Summary = summary,
            RecentActivity = activity,
        });
    }
}
