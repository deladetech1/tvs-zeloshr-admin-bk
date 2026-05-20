namespace ZelosHR.Api.Entities.Dashboard;

public sealed class DashboardSummaryDto
{
    public int TotalEmployees { get; init; }
    public int ActiveEmployees { get; init; }
    public int OnLeaveToday { get; init; }
    public int AbsentToday { get; init; }
    public int PendingLeaveRequests { get; init; }
    public int OpenJobPostings { get; init; }
    public int OverdueLifecycleEvents { get; init; }
    public int OpenDisciplinaryCases { get; init; }
    public int PendingOnboardingTasks { get; init; }
}

public sealed class DashboardActivityItemDto
{
    public required string Title { get; init; }
    public required string Description { get; init; }
    public DateTimeOffset OccurredAt { get; init; }
    public required string Category { get; init; }
}

public sealed class DashboardDto
{
    public DashboardSummaryDto Summary { get; init; } = new();
    public IReadOnlyList<DashboardActivityItemDto> RecentActivity { get; init; } = [];
}
