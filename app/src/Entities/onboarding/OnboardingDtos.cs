namespace ZelosHR.Api.Entities.Onboarding;

public sealed class OnboardingSummaryDto
{
    public int PendingTasks { get; init; }
    public int InProgress { get; init; }
    public int Completed { get; init; }
    public int Overdue { get; init; }
}

public sealed class OnboardingTaskListItemDto
{
    public required string TaskId { get; init; }
    public required string EmployeeId { get; init; }
    public required string EmployeeFullName { get; init; }
    public required string TaskName { get; init; }
    public required string Category { get; init; }
    public DateOnly DueDate { get; init; }
    public required string Status { get; init; }
    public string? AssignedTo { get; init; }
}

public sealed class OnboardingListDto
{
    public OnboardingSummaryDto Summary { get; init; } = new();
    public IReadOnlyList<OnboardingTaskListItemDto> Items { get; init; } = [];
}
