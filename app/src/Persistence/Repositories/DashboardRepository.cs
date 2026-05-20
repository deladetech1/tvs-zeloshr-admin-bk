using Microsoft.EntityFrameworkCore;
using ZelosHR.Api.Entities.Dashboard;

namespace ZelosHR.Api.Persistence.Repositories;

public sealed class DashboardRepository(ZelosHrDbContext db) : IDashboardRepository
{
    public async Task<DashboardSummaryDto> GetSummaryScopedAsync(
        string tenantId, string orgId, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var employees = db.Employees.AsNoTracking()
            .Where(e => e.TenantId == tenantId && e.OrgId == orgId);

        var leaveRequests = db.LeaveRequests.AsNoTracking()
            .Where(r => r.TenantId == tenantId && r.OrgId == orgId);

        var attendance = db.AttendanceRecords.AsNoTracking()
            .Where(a => a.TenantId == tenantId && a.OrgId == orgId);

        var jobPostings = db.JobPostings.AsNoTracking()
            .Where(j => j.TenantId == tenantId && j.OrgId == orgId);

        var lifecycleEvents = db.LifecycleEvents.AsNoTracking()
            .Where(e => e.TenantId == tenantId && e.OrgId == orgId);

        var disciplinaryCases = db.DisciplinaryCases.AsNoTracking()
            .Where(c => c.TenantId == tenantId && c.OrgId == orgId);

        var onboardingTasks = db.OnboardingTasks.AsNoTracking()
            .Where(t => t.TenantId == tenantId && t.OrgId == orgId);

        return new DashboardSummaryDto
        {
            TotalEmployees = await employees.CountAsync(e => !e.IsDeleted, ct),
            ActiveEmployees = await employees.CountAsync(e => e.EmploymentStatus == "Active", ct),
            OnLeaveToday = await leaveRequests.CountAsync(
                r => r.Status == "Approved" && r.StartDate <= today && r.EndDate >= today, ct),
            AbsentToday = await attendance.CountAsync(
                a => a.AttendanceDate == today && a.Status == "Absent", ct),
            PendingLeaveRequests = await leaveRequests.CountAsync(r => r.Status == "Pending", ct),
            OpenJobPostings = await jobPostings.CountAsync(j => j.Status == "Open", ct),
            OverdueLifecycleEvents = await lifecycleEvents.CountAsync(e => e.Urgency == "Overdue", ct),
            OpenDisciplinaryCases = await disciplinaryCases.CountAsync(c => c.Status == "Open", ct),
            PendingOnboardingTasks = await onboardingTasks.CountAsync(t => t.Status == "Pending", ct),
        };
    }

    public async Task<IReadOnlyList<DashboardActivityItemDto>> GetRecentActivityScopedAsync(
        string tenantId, string orgId, int limit = 8, CancellationToken ct = default) =>
        await db.AuditLogs.AsNoTracking()
            .Where(a => a.TenantId == tenantId && a.OrgId == orgId)
            .OrderByDescending(a => a.OccurredAt)
            .Take(limit)
            .Select(a => new DashboardActivityItemDto
            {
                Title = a.ActionTitle,
                Description = a.ActionDescription ?? string.Empty,
                OccurredAt = a.OccurredAt,
                Category = a.Category,
            })
            .ToListAsync(ct);
}
