namespace ZelosHR.Api.Entities.Leave;

using ZelosHR.Api.Configs;
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
    public string? FullName { get; init; }
}

/// <summary>LM / HOD approver on the flat Leave Approvals table row.</summary>
public sealed class LeaveApprovalApproverDto
{
    public required string Name { get; init; }

    /// <summary>Profile photo (<c>DocumentReadDto</c>): <c>doc_id</c>, <c>name</c>, <c>presigned_url</c> (~24h), <c>description</c>.</summary>
    public DocumentReadDto? ProfileUrl { get; init; }
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

public sealed class LeavePersonalSummaryDto
{
    public decimal TotalRemainingDays { get; init; }
    public int PendingRequests { get; init; }
    public int ApprovedThisYear { get; init; }

    /// <summary>Balance rows for the logged-in employee with nested <c>employee</c> and <c>leave_type</c> refs.</summary>
    public IReadOnlyList<LeaveBalanceListItemDto> Balances { get; init; } = [];
}

public sealed class LeaveRequestListItemDto
{
    public required string LeaveRequestId { get; init; }
    public LeaveEmployeeRefDto? Employee { get; init; }
    public LeaveTypeRefDto? LeaveType { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public decimal DaysRequested { get; init; }
    public required string Status { get; init; }
    public required string ApprovalStage { get; init; }
    public LeaveApproverRefDto? Approver { get; init; }
    /// <summary>Line manager and head of department who already approved (LM → HOD). Leave Approvals table.</summary>
    public IReadOnlyList<LeaveApproverRefDto> ApprovedBy { get; init; } = [];
    public IReadOnlyList<LeaveApprovalStepDto> PriorApprovers { get; init; } = [];
    public string? Notes { get; init; }
    public decimal? RemainingDays { get; init; }
    public int? WaitingHours { get; init; }
    /// <summary>First day back after leave (<c>end_date + 1</c>) — dashboard “Returns …” label.</summary>
    public DateOnly? ReturnsOn { get; init; }
    /// <summary>Days since the latest line-manager or HOD approval — dashboard “approved 5d” label.</summary>
    public int? DaysSinceLastApproval { get; init; }
    public DateTimeOffset SubmittedAt { get; init; }
    public DateTimeOffset? DecidedAt { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public string? CreatedById { get; init; }
    public string? UpdatedById { get; init; }
    public string? CreatedBy { get; init; }
    public string? UpdatedBy { get; init; }
}

public sealed class LeaveRequestDetailDto
{
    public required string LeaveRequestId { get; init; }
    public LeaveEmployeeRefDto? Employee { get; init; }
    public LeaveTypeRefDto? LeaveType { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public decimal DaysRequested { get; init; }
    public decimal WorkingDays { get; init; }
    public int PublicHolidaysInRange { get; init; }
    public required string Status { get; init; }
    public required string ApprovalStage { get; init; }
    public LeaveApproverRefDto? Approver { get; init; }
    public IReadOnlyList<LeaveApprovalStepDto> ApprovalTrail { get; init; } = [];
    public string? Notes { get; init; }
    public decimal? RemainingDays { get; init; }
    public LeaveBalanceImpactDto? BalanceImpact { get; init; }
    public int? WaitingHours { get; init; }
    public DateTimeOffset SubmittedAt { get; init; }
    public DateTimeOffset? DecidedAt { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public string? CreatedById { get; init; }
    public string? UpdatedById { get; init; }
    public string? CreatedBy { get; init; }
    public string? UpdatedBy { get; init; }
}

public sealed class LeaveBalanceListItemDto
{
    public required string LeaveBalanceId { get; init; }
    public LeaveEmployeeRefDto? Employee { get; init; }
    public LeaveTypeRefDto? LeaveType { get; init; }
    public decimal EntitledDays { get; init; }
    public decimal UsedDays { get; init; }
    public decimal RemainingDays { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public string? CreatedById { get; init; }
    public string? UpdatedById { get; init; }
    public string? CreatedBy { get; init; }
    public string? UpdatedBy { get; init; }
}

public sealed class LeaveListDto
{
    public LeaveSummaryDto Summary { get; init; } = new();
    public IReadOnlyList<LeaveRequestListItemDto> Items { get; init; } = [];
}

/// <summary>Leave Approvals table — flat employee rows with leave context.</summary>
public sealed class LeaveApprovalListDto
{
    public int PendingCount { get; init; }
    public IReadOnlyList<LeaveApprovalListItemDto> Items { get; init; } = [];
}

public sealed class LeaveApprovalListItemDto
{
    public required string LeaveRequestId { get; init; }
    public required string EmployeeId { get; init; }
    public required string EmployeeName { get; init; }
    public string? EmployeeCode { get; init; }
    public string? Title { get; init; }

    /// <summary>Profile photo (<c>DocumentReadDto</c>): <c>doc_id</c>, <c>name</c>, <c>presigned_url</c> (~24h), <c>description</c>.</summary>
    public DocumentReadDto? ProfileUrl { get; init; }
    public required string LeaveType { get; init; }
    public DateOnly LeaveFrom { get; init; }
    public DateOnly LeaveTo { get; init; }
    public decimal LeaveDays { get; init; }

    /// <summary>Hours since request reached final approval queue (<c>submitted_at</c>).</summary>
    public int? Waiting { get; init; }

    /// <summary>Line manager and head of department who already approved (LM → HOD).</summary>
    public IReadOnlyList<LeaveApprovalApproverDto> ApprovedBy { get; init; } = [];
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public string? CreatedById { get; init; }
    public string? UpdatedById { get; init; }
    public string? CreatedBy { get; init; }
    public string? UpdatedBy { get; init; }
}

public sealed class LeaveMyRequestListDto
{
    public IReadOnlyList<LeaveRequestListItemDto> Items { get; init; } = [];
}

public sealed class LeaveBalanceListDto
{
    /// <summary>Balance rows; each item includes nested <c>employee</c> and <c>leave_type</c> for display.</summary>
    public IReadOnlyList<LeaveBalanceListItemDto> Items { get; init; } = [];
}

public sealed class LeaveDashboardSummaryDto
{
    public int OnLeaveToday { get; init; }
    public int PendingApprovals { get; init; }
    public int LeavingThisWeek { get; init; }
    public int LowBalanceAlert { get; init; }
}

/// <summary>On leave today widget — flat employee row.</summary>
public sealed class LeaveDashboardOnLeaveItemDto
{
    public required string LeaveRequestId { get; init; }
    public required string EmployeeId { get; init; }
    public required string EmployeeName { get; init; }
    public DocumentReadDto? ProfileUrl { get; init; }
    public required string LeaveType { get; init; }

    /// <summary>First day back after leave (<c>end_date + 1</c>) — “Returns 24 Sept”.</summary>
    public DateOnly ReturnsOn { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public string? CreatedById { get; init; }
    public string? UpdatedById { get; init; }
    public string? CreatedBy { get; init; }
    public string? UpdatedBy { get; init; }
}

/// <summary>Pending approvals widget — flat employee row (oldest first).</summary>
public sealed class LeaveDashboardPendingItemDto
{
    public required string LeaveRequestId { get; init; }
    public required string EmployeeId { get; init; }
    public required string EmployeeName { get; init; }
    public DocumentReadDto? ProfileUrl { get; init; }
    public required string LeaveType { get; init; }
    public decimal LeaveDays { get; init; }

    /// <summary>Hours waiting when no prior LM/HOD approval — “waiting 53h” / “pending 24h”.</summary>
    public int? Waiting { get; init; }

    /// <summary>Hours since latest LM/HOD approval when under 24h — “approved 12h”.</summary>
    public int? HoursSinceLastApproval { get; init; }

    /// <summary>Days since latest LM/HOD approval when 24h or more — “approved 5d”.</summary>
    public int? DaysSinceLastApproval { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public string? CreatedById { get; init; }
    public string? UpdatedById { get; init; }
    public string? CreatedBy { get; init; }
    public string? UpdatedBy { get; init; }
}

/// <summary>Leaving this week widget — flat employee row.</summary>
public sealed class LeaveDashboardLeavingItemDto
{
    public required string LeaveRequestId { get; init; }
    public required string EmployeeId { get; init; }
    public required string EmployeeName { get; init; }
    public DocumentReadDto? ProfileUrl { get; init; }
    public required string LeaveType { get; init; }
    public DateOnly StartsOn { get; init; }
    public decimal LeaveDays { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public string? CreatedById { get; init; }
    public string? UpdatedById { get; init; }
    public string? CreatedBy { get; init; }
    public string? UpdatedBy { get; init; }
}

public sealed class LeaveDashboardDto
{
    public LeaveDashboardSummaryDto Summary { get; init; } = new();
    public IReadOnlyList<LeaveDashboardOnLeaveItemDto> OnLeaveToday { get; init; } = [];
    public IReadOnlyList<LeaveDashboardPendingItemDto> PendingApprovals { get; init; } = [];
    public IReadOnlyList<LeaveDashboardLeavingItemDto> LeavingThisWeek { get; init; } = [];
}

/// <summary>Leave Calendar — flat employee + leave-range rows for the visible window.</summary>
public sealed class LeaveCalendarDto
{
    [SwaggerAllowedValues(typeof(LeaveFieldOptions), nameof(LeaveFieldOptions.CalendarViews))]
    public required string View { get; init; }

    public DateOnly AnchorDate { get; init; }
    public DateOnly FromDate { get; init; }
    public DateOnly ToDate { get; init; }
    public IReadOnlyList<LeaveCalendarListItemDto> Items { get; init; } = [];
}

/// <summary>One calendar grid row — employee identity plus one leave range (or null leave fields when none).</summary>
public sealed class LeaveCalendarListItemDto
{
    public required string EmployeeId { get; init; }
    public required string EmployeeName { get; init; }
    public string? EmployeeCode { get; init; }
    public string? Title { get; init; }

    /// <summary>Profile photo (<c>DocumentReadDto</c>): <c>doc_id</c>, <c>name</c>, <c>presigned_url</c> (~24h), <c>description</c>.</summary>
    public DocumentReadDto? ProfileUrl { get; init; }
    public string? LeaveRequestId { get; init; }
    public string? LeaveTypeId { get; init; }
    public string? LeaveType { get; init; }
    public string? Status { get; init; }
    public DateOnly? LeaveFrom { get; init; }
    public DateOnly? LeaveTo { get; init; }
    public decimal? LeaveDays { get; init; }
}

public sealed class LeaveTypeListItemDto
{
    public required string LeaveTypeId { get; init; }
    public required string Name { get; init; }
    public decimal DefaultEntitledDays { get; init; }
    public bool IsPaid { get; init; }

    [SwaggerAllowedValues(typeof(LeaveFieldOptions), nameof(LeaveFieldOptions.AccrualMethods))]
    public required string AccrualMethod { get; init; }
    public bool CarryOverAllowed { get; init; }
    public IReadOnlyList<string>? AppliesToEmploymentTypes { get; init; }
    public int? MinNoticeWorkingDays { get; init; }
    public int? MaxConsecutiveDays { get; init; }
    public bool RequiresSupportingDocument { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public string? CreatedById { get; init; }
    public string? UpdatedById { get; init; }
    public string? CreatedBy { get; init; }
    public string? UpdatedBy { get; init; }
}

public sealed class LeaveTypeListDto
{
    public IReadOnlyList<LeaveTypeListItemDto> Items { get; init; } = [];
}

public sealed class PublicHolidayListItemDto
{
    public required string HolidayId { get; init; }
    public required string HolidayName { get; init; }

    /// <summary>Stored calendar date. For recurring rows this is the anchor month/day (year is not significant).</summary>
    public DateOnly Date { get; init; }

    /// <summary>When <c>is_recurring_annually</c> is true, the API does not insert a new row each year — leave working-day logic projects this anchor into each calendar year. Populated on list when <c>year</c> query is set.</summary>
    public bool IsRecurringAnnually { get; init; }

    /// <summary>Projected date in the requested list <c>year</c> (recurring) or same as <c>date</c> when non-recurring.</summary>
    public DateOnly? OccurrenceDate { get; init; }

    /// <summary>Country catalog id from <c>GET /countries/list</c> (use returned <c>id</c>). Not an ISO code.</summary>
    public required string CountryId { get; init; }

    /// <summary>Joined on read — ISO 3166-1 alpha-2.</summary>
    public string? CountryCode { get; init; }

    /// <summary>Joined on read — display name.</summary>
    public string? CountryName { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public string? CreatedById { get; init; }
    public string? UpdatedById { get; init; }
    public string? CreatedBy { get; init; }
    public string? UpdatedBy { get; init; }
}

public sealed class PublicHolidayListDto
{
    public IReadOnlyList<PublicHolidayListItemDto> Items { get; init; } = [];
}
