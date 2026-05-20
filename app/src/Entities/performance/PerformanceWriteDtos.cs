using System.ComponentModel.DataAnnotations;

namespace ZelosHR.Api.Entities.Performance;

public sealed class CreatePerformanceReviewDto
{
    [Required]
    public Guid EmployeeId { get; set; }

    [Required]
    [MaxLength(100)]
    public string? ReviewPeriod { get; set; }

    [Required]
    public DateOnly DueDate { get; set; }

    [MaxLength(200)]
    public string? ReviewerName { get; set; }

    [MaxLength(50)]
    public string? Status { get; set; }
}

public sealed class UpdatePerformanceReviewDto
{
    [MaxLength(100)]
    public string? ReviewPeriod { get; set; }

    public DateOnly? DueDate { get; set; }

    [MaxLength(200)]
    public string? ReviewerName { get; set; }

    [MaxLength(50)]
    public string? OverallRating { get; set; }

    [MaxLength(50)]
    public string? Status { get; set; }
}
