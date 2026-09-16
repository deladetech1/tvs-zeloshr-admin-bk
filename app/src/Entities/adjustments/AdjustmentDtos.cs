namespace ZelosHR.Api.Entities.Adjustments;

public sealed class AdjustmentDto
{
    public required string AdjustmentId { get; init; }
    public required string EmployeeId { get; init; }
    public string? AttendanceId { get; init; }
    public DateOnly AttendanceDate { get; init; }
    public required string Kind { get; init; }
    public string? PunchType { get; init; }
    public string? PunchTime { get; init; }
    public string? OriginalPunchId { get; init; }
    public required string Reason { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public string? CreatedById { get; init; }
    public string? UpdatedById { get; init; }
    public string? CreatedBy { get; init; }
    public string? UpdatedBy { get; init; }
}

public sealed class CreateAdjustmentDto
{
    public Guid EmployeeId { get; set; }
    public DateOnly AttendanceDate { get; set; }
    public string Kind { get; set; } = default!;
    public string? PunchType { get; set; }
    public string? PunchTime { get; set; }
    public Guid? OriginalPunchId { get; set; }
    public string Reason { get; set; } = default!;
}
