using System.ComponentModel.DataAnnotations;

namespace ZelosHR.Api.Entities.LifecycleEvents;

public sealed class CreateLifecycleEventDto
{
    [Required]
    public Guid EmployeeId { get; set; }

    [Required]
    [MaxLength(80)]
    public string? EventType { get; set; }

    [Required]
    public DateOnly DueDate { get; set; }

    [MaxLength(50)]
    public string? Status { get; set; }

    [MaxLength(50)]
    public string? Urgency { get; set; }
}

public sealed class UpdateLifecycleEventDto
{
    [MaxLength(80)]
    public string? EventType { get; set; }

    public DateOnly? DueDate { get; set; }

    [MaxLength(50)]
    public string? Status { get; set; }

    [MaxLength(50)]
    public string? Urgency { get; set; }
}
