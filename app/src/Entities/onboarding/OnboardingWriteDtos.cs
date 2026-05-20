using System.ComponentModel.DataAnnotations;

namespace ZelosHR.Api.Entities.Onboarding;

public sealed class CreateOnboardingTaskDto
{
    [Required]
    public Guid EmployeeId { get; set; }

    [Required]
    [MaxLength(200)]
    public string? TaskName { get; set; }

    [Required]
    [MaxLength(80)]
    public string? Category { get; set; }

    [Required]
    public DateOnly DueDate { get; set; }

    [MaxLength(200)]
    public string? AssignedTo { get; set; }

    [MaxLength(50)]
    public string? Status { get; set; }
}

public sealed class UpdateOnboardingTaskDto
{
    [MaxLength(200)]
    public string? TaskName { get; set; }

    [MaxLength(80)]
    public string? Category { get; set; }

    public DateOnly? DueDate { get; set; }

    [MaxLength(50)]
    public string? Status { get; set; }

    [MaxLength(200)]
    public string? AssignedTo { get; set; }
}
