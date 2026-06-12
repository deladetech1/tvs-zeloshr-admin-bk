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

    public static readonly IReadOnlyList<string> DefaultLeaveTypeNames =
    [
        "Annual Leave",
        "Sick Leave",
        "Compassionate Leave",
        "Maternity Leave",
        "Paternity Leave",
        "Unpaid Leave",
    ];
}

public static class LeaveRequestStatuses
{
    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
    public const string Cancelled = "Cancelled";
}
