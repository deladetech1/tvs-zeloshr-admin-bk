namespace ZelosHR.Api.Entities.LifecycleEvents;

public sealed class LifecycleEventSummaryDto
{
    public int OverdueCount { get; init; }
    public int CriticalCount { get; init; }
    public int PendingActionCount { get; init; }
    public int TotalEventsCount { get; init; }
}

public sealed class LifecycleEventListItemDto
{
    public required string LifecycleEventId { get; init; }
    public required string EmployeeId { get; init; }
    public required string EmployeeFullName { get; init; }
    public required string EventType { get; init; }
    public string? DepartmentName { get; init; }
    public string? BranchName { get; init; }
    public DateOnly DueDate { get; init; }
    public required string Status { get; init; }
    public required string Urgency { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public string? CreatedById { get; init; }
    public string? UpdatedById { get; init; }
    public string? CreatedBy { get; init; }
    public string? UpdatedBy { get; init; }
}

public sealed class LifecycleEventListDto
{
    public LifecycleEventSummaryDto Summary { get; init; } = new();
    public IReadOnlyList<LifecycleEventListItemDto> Items { get; init; } = [];
}
