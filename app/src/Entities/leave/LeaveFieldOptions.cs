namespace ZelosHR.Api.Entities.Leave;

public static class LeaveFieldOptions
{
    public static readonly IReadOnlyList<string> RequestStatuses =
    [
        LeaveRequestStatuses.Pending,
        LeaveRequestStatuses.Approved,
        LeaveRequestStatuses.Rejected,
        LeaveRequestStatuses.Cancelled,
    ];

    public static readonly IReadOnlyList<string> ApprovalStages =
    [
        LeaveApprovalStages.PendingLineManager,
        LeaveApprovalStages.PendingHeadOfDepartment,
        LeaveApprovalStages.PendingFinal,
        LeaveApprovalStages.Approved,
        LeaveApprovalStages.Rejected,
    ];

    public static readonly IReadOnlyList<string> DefaultLeaveTypeNames =
    [
        "Annual Leave",
        "Sick Leave",
        "Compassionate Leave",
        "Maternity Leave",
        "Paternity Leave",
        "Unpaid Leave",
    ];

    public static readonly IReadOnlyList<string> AccrualMethods =
    [
        LeaveAccrualMethods.FrontLoaded,
        LeaveAccrualMethods.Monthly,
    ];

    /// <summary>Configure leave type screen — maps to employee employment_type (Contract → Contractor).</summary>
    public static readonly IReadOnlyList<string> AppliesToEmploymentTypes =
    [
        "Full-time",
        "Part-time",
        "Contract",
    ];

    /// <summary>Leave Approvals screen tabs.</summary>
    public static readonly IReadOnlyList<string> ApprovalListTabs =
    [
        LeaveApprovalListTabs.Pending,
        LeaveApprovalListTabs.History,
    ];

    public static readonly IReadOnlyList<string> ApprovalListSortBy =
    [
        "name",
    ];

    public static readonly IReadOnlyList<string> ApprovalListSortOrder =
    [
        "asc",
        "desc",
    ];

    /// <summary>Leave Calendar visible window.</summary>
    public static readonly IReadOnlyList<string> CalendarViews =
    [
        LeaveCalendarViews.Week,
        LeaveCalendarViews.Month,
    ];
}

public static class LeaveCalendarViews
{
    public const string Week = "week";
    public const string Month = "month";
}

public static class LeaveApprovalListTabs
{
    public const string Pending = "pending";
    public const string History = "history";
}

public static class LeaveRequestStatuses
{
    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
    public const string Cancelled = "Cancelled";
}

public static class LeaveApprovalStages
{
    public const string PendingLineManager = "pending_line_manager";
    public const string PendingHeadOfDepartment = "pending_head_of_department";
    public const string PendingFinal = "pending_final";
    public const string Approved = "approved";
    public const string Rejected = "rejected";
}

public static class LeaveApprovalStepLabels
{
    public const string LineManager = "line_manager";
    public const string HeadOfDepartment = "head_of_department";
    public const string Final = "final";
}

public sealed record LeaveRequestListQuery
{
    /// <summary>Free-text: employee name, employee code, job title, or leave type name (min 2 chars).</summary>
    public string? Search { get; init; }
    public string? Status { get; init; }
    public string? ApprovalStage { get; init; }
    public Guid? LeaveRequestId { get; init; }
    public Guid? LeaveTypeId { get; init; }
    public Guid? EmployeeId { get; init; }
    public string? EmployeeCode { get; init; }
    public Guid? DepartmentId { get; init; }
    public Guid? BranchId { get; init; }
    /// <summary>Leave period overlap: request end_date on or after this date.</summary>
    public DateOnly? FromDate { get; init; }
    /// <summary>Leave period overlap: request start_date on or before this date.</summary>
    public DateOnly? ToDate { get; init; }
    /// <summary>Filter by submitted_at on or after start of this day (UTC).</summary>
    public DateOnly? SubmittedFromDate { get; init; }
    /// <summary>Filter by submitted_at on or before end of this day (UTC).</summary>
    public DateOnly? SubmittedToDate { get; init; }
    /// <summary>Leave Approvals screen: <c>pending</c> (final queue) or <c>history</c>.</summary>
    public string? Tab { get; init; }
    public string? SortBy { get; init; }
    public string? SortOrder { get; init; }
    public int Page { get; init; } = 1;
    public int Size { get; init; } = 20;
}

public sealed record LeaveCalendarQuery
{
    /// <summary><c>week</c> or <c>month</c> — sets the visible window from <c>anchor_date</c> when dates omitted.</summary>
    public string? View { get; init; }
    /// <summary>Date inside the week or month to display (defaults to today UTC).</summary>
    public DateOnly? AnchorDate { get; init; }
    /// <summary>Free-text: employee name, employee code, or job title (min 2 chars).</summary>
    public string? Search { get; init; }
    public Guid? DepartmentId { get; init; }
    public Guid? LeaveTypeId { get; init; }
    /// <summary>Explicit window start (overrides <c>view</c> when paired with <c>to_date</c>).</summary>
    public DateOnly? FromDate { get; init; }
    /// <summary>Explicit window end (overrides <c>view</c> when paired with <c>from_date</c>).</summary>
    public DateOnly? ToDate { get; init; }
    public int Page { get; init; } = 1;
    public int Size { get; init; } = 50;
}

public sealed record LeaveCalendarWindow(
    string View,
    DateOnly AnchorDate,
    DateOnly FromDate,
    DateOnly ToDate)
{
    public static LeaveCalendarWindow Resolve(
        string? view,
        DateOnly? anchorDate,
        DateOnly? fromDate,
        DateOnly? toDate)
    {
        var anchor = anchorDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var normalizedView = NormalizeView(view);

        if (fromDate.HasValue || toDate.HasValue)
        {
            var from = fromDate ?? toDate!.Value;
            var to = toDate ?? fromDate!.Value;
            if (to < from)
                to = from;

            return new LeaveCalendarWindow(
                normalizedView ?? LeaveCalendarViews.Month,
                anchor,
                from,
                to);
        }

        if (string.Equals(normalizedView, LeaveCalendarViews.Month, StringComparison.OrdinalIgnoreCase))
        {
            var monthStart = new DateOnly(anchor.Year, anchor.Month, 1);
            var monthEnd = new DateOnly(
                anchor.Year,
                anchor.Month,
                DateTime.DaysInMonth(anchor.Year, anchor.Month));
            return new LeaveCalendarWindow(LeaveCalendarViews.Month, anchor, monthStart, monthEnd);
        }

        var (weekStart, weekEnd) = WeekContaining(anchor);
        return new LeaveCalendarWindow(LeaveCalendarViews.Week, anchor, weekStart, weekEnd);
    }

    private static (DateOnly From, DateOnly To) WeekContaining(DateOnly anchor)
    {
        var mondayOffset = ((int)anchor.DayOfWeek + 6) % 7;
        var from = anchor.AddDays(-mondayOffset);
        return (from, from.AddDays(6));
    }

    private static string? NormalizeView(string? view) =>
        string.IsNullOrWhiteSpace(view) ? null : view.Trim().ToLowerInvariant();
}

public sealed record LeaveCalendarScopedResult(
    string View,
    DateOnly AnchorDate,
    DateOnly FromDate,
    DateOnly ToDate,
    IReadOnlyList<Guid> EmployeeIds,
    IReadOnlyDictionary<Guid, IReadOnlyList<LeaveRequestRawRow>> LeaveByEmployeeId,
    int TotalEmployees);

public sealed record LeaveApprovalListQuery
{
    /// <summary>Free-text: employee name, employee code, or job title (min 2 chars).</summary>
    public string? Search { get; init; }
    public Guid? LeaveTypeId { get; init; }
    public Guid? DepartmentId { get; init; }
    public DateOnly? FromDate { get; init; }
    public DateOnly? ToDate { get; init; }
    /// <summary><c>pending</c> (final queue) or <c>history</c>.</summary>
    public string? Tab { get; init; }
    public string? SortBy { get; init; }
    public string? SortOrder { get; init; }
    public int Page { get; init; } = 1;
    public int Size { get; init; } = 20;
}
