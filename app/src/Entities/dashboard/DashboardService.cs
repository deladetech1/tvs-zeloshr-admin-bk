using Dapper;
using Microsoft.Extensions.Options;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Shared;

namespace ZelosHR.Api.Entities.Dashboard;

public class DashboardService
{
    private readonly IDatabaseManager _database;
    private readonly AppSettings _settings;

    public DashboardService(IDatabaseManager database, IOptions<AppSettings> settings)
    {
        _database = database;
        _settings = settings.Value;
    }

    public async Task<Respons<DashboardDto>> GetDashboardAsync(
        string tenantId, string orgId, CancellationToken ct = default)
    {
        await using var connection = await _database.GetConnectionAsync(ct);
        var today = DateTime.UtcNow.Date;

        var summary = await connection.QuerySingleAsync<DashboardSummaryDto>(
            $"""
            SELECT
                (SELECT COUNT(*)::int FROM {_settings.EmployeesTable}
                 WHERE tenant_id = @TenantId AND org_id = @OrgId AND is_deleted = FALSE) AS TotalEmployees,
                (SELECT COUNT(*)::int FROM {_settings.EmployeesTable}
                 WHERE tenant_id = @TenantId AND org_id = @OrgId AND employment_status = 'Active') AS ActiveEmployees,
                (SELECT COUNT(*)::int FROM zeloshr.zhr_leave_requests
                 WHERE tenant_id = @TenantId AND org_id = @OrgId AND status = 'Approved'
                   AND @Today BETWEEN start_date AND end_date) AS OnLeaveToday,
                (SELECT COUNT(*)::int FROM zeloshr.zhr_attendance_records
                 WHERE tenant_id = @TenantId AND org_id = @OrgId AND attendance_date = @Today
                   AND status = 'Absent') AS AbsentToday,
                (SELECT COUNT(*)::int FROM zeloshr.zhr_leave_requests
                 WHERE tenant_id = @TenantId AND org_id = @OrgId AND status = 'Pending') AS PendingLeaveRequests,
                (SELECT COUNT(*)::int FROM zeloshr.zhr_job_postings
                 WHERE tenant_id = @TenantId AND org_id = @OrgId AND status = 'Open') AS OpenJobPostings,
                (SELECT COUNT(*)::int FROM zeloshr.zhr_lifecycle_events
                 WHERE tenant_id = @TenantId AND org_id = @OrgId AND urgency = 'Overdue') AS OverdueLifecycleEvents,
                (SELECT COUNT(*)::int FROM zeloshr.zhr_disciplinary_cases
                 WHERE tenant_id = @TenantId AND org_id = @OrgId AND status = 'Open') AS OpenDisciplinaryCases,
                (SELECT COUNT(*)::int FROM zeloshr.zhr_onboarding_tasks
                 WHERE tenant_id = @TenantId AND org_id = @OrgId AND status = 'Pending') AS PendingOnboardingTasks
            """,
            new { TenantId = tenantId, OrgId = orgId, Today = today });

        var activity = (await connection.QueryAsync<DashboardActivityItemDto>(
            """
            SELECT action_title AS Title, COALESCE(action_description, '') AS Description,
                   occurred_at AS OccurredAt, category AS Category
            FROM zeloshr.zhr_audit_logs
            WHERE tenant_id = @TenantId AND org_id = @OrgId
            ORDER BY occurred_at DESC
            LIMIT 8
            """,
            new { TenantId = tenantId, OrgId = orgId })).ToList();

        return Respons<DashboardDto>.Ok(new DashboardDto
        {
            Summary = summary,
            RecentActivity = activity,
        });
    }
}
