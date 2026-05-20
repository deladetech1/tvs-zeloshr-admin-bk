using System.ComponentModel.DataAnnotations;

namespace ZelosHR.Api.Entities.Attendance;

public sealed class CreateAttendanceDto
{
    [Required]
    public Guid EmployeeId { get; set; }

    [Required]
    public DateOnly AttendanceDate { get; set; }

    [Required]
    [MaxLength(50)]
    public string? Status { get; set; }

    public string? ClockIn { get; set; }
    public string? ClockOut { get; set; }
    public decimal? HoursWorked { get; set; }
}

public sealed class UpdateAttendanceDto
{
    [MaxLength(50)]
    public string? Status { get; set; }

    public string? ClockIn { get; set; }
    public string? ClockOut { get; set; }
    public decimal? HoursWorked { get; set; }
}
