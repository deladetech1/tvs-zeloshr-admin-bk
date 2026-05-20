namespace ZelosHR.Api.Entities.Attendance;

public sealed class AttendanceSummaryDto
{
    public int TotalScheduled { get; init; }
    public int Present { get; init; }
    public int Late { get; init; }
    public int Absent { get; init; }
    public int OnLeave { get; init; }
}

public sealed class AttendanceListItemDto
{
    public required string AttendanceId { get; init; }
    public required string EmployeeId { get; init; }
    public required string EmployeeFullName { get; init; }
    public string? EmployeeCode { get; init; }
    public string? DepartmentName { get; init; }
    public string? BranchName { get; init; }
    public DateOnly AttendanceDate { get; init; }
    public string? ClockIn { get; init; }
    public string? ClockOut { get; init; }
    public required string Status { get; init; }
    public decimal? HoursWorked { get; init; }
}

public sealed class AttendanceListDto
{
    public AttendanceSummaryDto Summary { get; init; } = new();
    public IReadOnlyList<AttendanceListItemDto> Items { get; init; } = [];
}
