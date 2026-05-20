namespace ZelosHR.Api.Entities.Performance;

public sealed class PerformanceSummaryDto
{
    public int Pending { get; init; }
    public int InProgress { get; init; }
    public int Completed { get; init; }
    public int Overdue { get; init; }
}

public sealed class PerformanceReviewListItemDto
{
    public required string ReviewId { get; init; }
    public required string EmployeeId { get; init; }
    public required string EmployeeFullName { get; init; }
    public required string ReviewPeriod { get; init; }
    public string? ReviewerName { get; init; }
    public string? OverallRating { get; init; }
    public required string Status { get; init; }
    public DateOnly DueDate { get; init; }
}

public sealed class PerformanceListDto
{
    public PerformanceSummaryDto Summary { get; init; } = new();
    public IReadOnlyList<PerformanceReviewListItemDto> Items { get; init; } = [];
}
