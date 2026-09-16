namespace ZelosHR.Api.Entities.Clock;

public sealed class PunchDto
{
    public required string PunchId { get; init; }
    public required string PunchType { get; init; }
    public required DateTimeOffset PunchedAt { get; init; }
    public required string Source { get; init; }
    public string? DeviceId { get; init; }
    public bool IsSuperseded { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public string? CreatedById { get; init; }
    public string? UpdatedById { get; init; }
    public string? CreatedBy { get; init; }
    public string? UpdatedBy { get; init; }
}

public sealed class ClockTodayDto
{
    public required string EmployeeId { get; init; }
    public required string EmployeeFullName { get; init; }
    public string? JobTitle { get; init; }
    public string? DepartmentName { get; init; }
    public DateOnly Date { get; init; }
    public required string State { get; init; }
    public string? ClockIn { get; init; }
    public string? ClockOut { get; init; }
    public decimal? HoursWorked { get; init; }
    public IReadOnlyList<PunchDto> Punches { get; init; } = [];
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public string? CreatedById { get; init; }
    public string? UpdatedById { get; init; }
    public string? CreatedBy { get; init; }
    public string? UpdatedBy { get; init; }
}

public sealed class ClockActionRequest
{
    public Guid EmployeeId { get; set; }
    public bool? OnCompanyNetwork { get; set; }
    public string? Source { get; set; }
    public Guid? DeviceId { get; set; }
}

public sealed class TimesheetStatsDto
{
    public int DaysPresent { get; init; }
    public int NoRecord { get; init; }
    public int Adjustments { get; init; }
    public int AutoClosed { get; init; }
    public int PeriodMinutes { get; init; }
}

public sealed class TimesheetDayDto
{
    public DateOnly Date { get; init; }
    public required string Weekday { get; init; }
    public string? ClockIn { get; init; }
    public string? ClockOut { get; init; }
    public decimal? HoursWorked { get; init; }
    public required string Status { get; init; }
    public bool HasAdjustment { get; init; }
    public bool AutoClosed { get; init; }
    public IReadOnlyList<PunchDto> Punches { get; init; } = [];
}

public sealed class TimesheetDto
{
    public required string EmployeeId { get; init; }
    public required string EmployeeFullName { get; init; }
    public DateOnly FromDate { get; init; }
    public DateOnly ToDate { get; init; }
    public TimesheetStatsDto Stats { get; init; } = new();
    public IReadOnlyList<TimesheetDayDto> Days { get; init; } = [];
}

public sealed class TeamMemberDto
{
    public required string EmployeeId { get; init; }
    public required string FullName { get; init; }
    public string? JobTitle { get; init; }
    public string? DepartmentName { get; init; }
    public string? ClockIn { get; init; }
    public string? ClockOut { get; init; }
    public decimal? HoursWorked { get; init; }
    public required string Status { get; init; }
    public bool IsLineManager { get; init; }
    public bool IsHeadOfDepartment { get; init; }
}

public sealed class TeamTabDto
{
    public required string Id { get; init; }
    public required string Label { get; init; }
    public int Count { get; init; }
}

public sealed class TeamDto
{
    public DateOnly Date { get; init; }
    public required string Scope { get; init; }
    public IReadOnlyList<TeamTabDto> Tabs { get; init; } = [];
    public IReadOnlyList<TeamMemberDto> Items { get; init; } = [];
}
