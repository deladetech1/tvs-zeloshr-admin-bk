namespace ZelosHR.Api.Entities.Leave;

using ZelosHR.Api.Entities.Files;

public sealed class LeaveEmployeeRefDto
{
    public required string EmployeeId { get; init; }
    public required string FullName { get; init; }
    public string? EmployeeCode { get; init; }
    public string? JobTitle { get; init; }
    public string? DepartmentId { get; init; }
    public string? DepartmentName { get; init; }

    /// <summary>Profile photo (<c>DocumentReadDto</c>): <c>doc_id</c>, <c>name</c>, <c>presigned_url</c> (~24h), <c>description</c>.</summary>
    public DocumentReadDto? ProfileUrl { get; init; }
}

public sealed class LeaveTypeRefDto
{
    public required string LeaveTypeId { get; init; }
    public required string Name { get; init; }
}

public sealed class LeaveApproverRefDto
{
    public required string ApproverId { get; init; }
    public required string FullName { get; init; }
}

public sealed class LeaveApprovalStepDto
{
    public required string Stage { get; init; }
    public required string Status { get; init; }
    public LeaveApproverRefDto? Approver { get; init; }
    public DateTimeOffset? DecidedAt { get; init; }
}

public sealed class LeaveBalanceImpactDto
{
    public decimal Current { get; init; }
    public decimal After { get; init; }
    public decimal Deduction { get; init; }
}

public sealed class LeaveSummaryDto
{
    public int PendingRequests { get; init; }
    public int PendingFinalApprovals { get; init; }
    public int ApprovedThisMonth { get; init; }
    public int OnLeaveToday { get; init; }
    public int LeavingThisWeek { get; init; }
    public int LowBalanceAlert { get; init; }
    public int TotalRequests { get; init; }
}

public sealed class LeaveMySummaryDto
{
    public decimal TotalRemainingDays { get; init; }
    public int PendingRequests { get; init; }
    public int ApprovedThisYear { get; init; }
    public IReadOnlyList<LeaveBalanceListItemDto> Balances { get; init; } = [];
}

public sealed class LeaveRequestListItemDto
{
    public required string LeaveRequestId { get; init; }
    public required string EmployeeId { get; init; }
    public required string LeaveTypeId { get; init; }
    public LeaveEmployeeRefDto? Employee { get; init; }
    public LeaveTypeRefDto? LeaveType { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public decimal DaysRequested { get; init; }
    public required string Status { get; init; }
    public required string ApprovalStage { get; init; }
    public string? ApproverId { get; init; }
    public LeaveApproverRefDto? Approver { get; init; }
    public IReadOnlyList<LeaveApprovalStepDto> PriorApprovers { get; init; } = [];
    public string? Notes { get; init; }
    public decimal? RemainingDays { get; init; }
    public int? WaitingHours { get; init; }
    public DateTimeOffset SubmittedAt { get; init; }
    public DateTimeOffset? DecidedAt { get; init; }
}

public sealed class LeaveRequestDetailDto
{
    public required string LeaveRequestId { get; init; }
    public required string EmployeeId { get; init; }
    public required string LeaveTypeId { get; init; }
    public LeaveEmployeeRefDto? Employee { get; init; }
    public LeaveTypeRefDto? LeaveType { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public decimal DaysRequested { get; init; }
    public decimal WorkingDays { get; init; }
    public int PublicHolidaysInRange { get; init; }
    public required string Status { get; init; }
    public required string ApprovalStage { get; init; }
    public string? ApproverId { get; init; }
    public LeaveApproverRefDto? Approver { get; init; }
    public IReadOnlyList<LeaveApprovalStepDto> ApprovalTrail { get; init; } = [];
    public string? Notes { get; init; }
    public decimal? RemainingDays { get; init; }
    public LeaveBalanceImpactDto? BalanceImpact { get; init; }
    public int? WaitingHours { get; init; }
    public DateTimeOffset SubmittedAt { get; init; }
    public DateTimeOffset? DecidedAt { get; init; }
}

public sealed class LeaveBalanceListItemDto
{
    public required string LeaveBalanceId { get; init; }
    public required string EmployeeId { get; init; }
    public required string LeaveTypeId { get; init; }
    public LeaveTypeRefDto? LeaveType { get; init; }
    public decimal EntitledDays { get; init; }
    public decimal UsedDays { get; init; }
    public decimal RemainingDays { get; init; }
}

public sealed class LeaveListDto
{
    public LeaveSummaryDto Summary { get; init; } = new();
    public IReadOnlyList<LeaveRequestListItemDto> Items { get; init; } = [];
}

public sealed class LeaveMyRequestListDto
{
    public IReadOnlyList<LeaveRequestListItemDto> Items { get; init; } = [];
}

public sealed class LeaveBalanceListDto
{
    public IReadOnlyList<LeaveBalanceListItemDto> Items { get; init; } = [];
}

public sealed class LeaveDashboardDto
{
    public LeaveSummaryDto Summary { get; init; } = new();
    public IReadOnlyList<LeaveRequestListItemDto> OnLeaveToday { get; init; } = [];
    public IReadOnlyList<LeaveRequestListItemDto> PendingApprovals { get; init; } = [];
    public IReadOnlyList<LeaveRequestListItemDto> LeavingThisWeek { get; init; } = [];
}

public sealed class LeaveTypeListItemDto
{
    public required string LeaveTypeId { get; init; }
    public required string Name { get; init; }
    public string? CountryCode { get; init; }
    public decimal DefaultEntitledDays { get; init; }
    public bool IsPaid { get; init; }
    public bool IsActive { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}

public sealed class LeaveTypeListDto
{
    public IReadOnlyList<LeaveTypeListItemDto> Items { get; init; } = [];
}

public sealed class PublicHolidayListItemDto
{
    public required string HolidayId { get; init; }
    public required string CountryCode { get; init; }
    public required string Name { get; init; }
    public DateOnly HolidayDate { get; init; }
    public bool IsRecurring { get; init; }
    public string? BranchId { get; init; }
    public bool IsActive { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}

public sealed class PublicHolidayListDto
{
    public IReadOnlyList<PublicHolidayListItemDto> Items { get; init; } = [];
}
