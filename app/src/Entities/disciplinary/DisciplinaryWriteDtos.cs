using System.ComponentModel.DataAnnotations;

namespace ZelosHR.Api.Entities.Disciplinary;

public sealed class CreateDisciplinaryCaseDto
{
    [Required]
    public Guid EmployeeId { get; set; }

    [Required]
    [MaxLength(100)]
    public string? CaseType { get; set; }

    [Required]
    [MaxLength(50)]
    public string? Severity { get; set; }

    public DateOnly? OpenedAt { get; set; }

    public string? Description { get; set; }

    [MaxLength(50)]
    public string? Status { get; set; }
}

public sealed class UpdateDisciplinaryCaseDto
{
    [MaxLength(100)]
    public string? CaseType { get; set; }

    [MaxLength(50)]
    public string? Severity { get; set; }

    public DateOnly? OpenedAt { get; set; }

    public string? Description { get; set; }

    [MaxLength(50)]
    public string? Status { get; set; }
}
