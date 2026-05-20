using System.ComponentModel.DataAnnotations;

namespace ZelosHR.Api.Entities.Leave;

public sealed class CreateLeaveRequestDto
{
    [Required]
    public Guid EmployeeId { get; set; }

    [Required]
    [MaxLength(80)]
    public string? LeaveType { get; set; }

    [Required]
    public DateOnly StartDate { get; set; }

    [Required]
    public DateOnly EndDate { get; set; }

    [Required]
    [Range(0.5, 365)]
    public decimal DaysRequested { get; set; }
}

public sealed class UpdateLeaveRequestDto
{
    [MaxLength(50)]
    public string? Status { get; set; }

    [MaxLength(200)]
    public string? ApproverName { get; set; }
}
