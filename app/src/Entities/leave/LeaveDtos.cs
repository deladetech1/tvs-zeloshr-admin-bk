namespace ZelosHR.Api.Entities.Leave;

public sealed class LeaveSummaryDto
{
    public int PendingRequests { get; init; }
    public int ApprovedThisMonth { get; init; }
    public int OnLeaveToday { get; init; }
    public int TotalRequests { get; init; }
}

public sealed class LeaveRequestListItemDto
{
    public required string LeaveRequestId { get; init; }
    public required string EmployeeId { get; init; }
    public required string EmployeeFullName { get; init; }
    public required string LeaveType { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public decimal DaysRequested { get; init; }
    public required string Status { get; init; }
    public string? ApproverName { get; init; }
}

public sealed class LeaveBalanceListItemDto
{
    public required string EmployeeId { get; init; }
    public required string EmployeeFullName { get; init; }
    public required string LeaveType { get; init; }
    public decimal EntitledDays { get; init; }
    public decimal UsedDays { get; init; }
    public decimal RemainingDays { get; init; }
}

public sealed class LeaveListDto
{
    public LeaveSummaryDto Summary { get; init; } = new();
    public IReadOnlyList<LeaveRequestListItemDto> Requests { get; init; } = [];
    public IReadOnlyList<LeaveBalanceListItemDto> Balances { get; init; } = [];
}
