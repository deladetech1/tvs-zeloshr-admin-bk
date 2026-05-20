using System.ComponentModel.DataAnnotations;

namespace ZelosHR.Api.Entities.Recruitment;

public sealed class CreateJobPostingDto
{
    [Required]
    [MaxLength(200)]
    public string? Title { get; set; }

    [MaxLength(150)]
    public string? DepartmentName { get; set; }

    [MaxLength(150)]
    public string? BranchName { get; set; }

    [MaxLength(50)]
    public string? EmploymentType { get; set; }

    public DateOnly? PostedAt { get; set; }

    public DateOnly? ClosingDate { get; set; }

    [MaxLength(50)]
    public string? Status { get; set; }
}

public sealed class UpdateJobPostingDto
{
    [MaxLength(200)]
    public string? Title { get; set; }

    [MaxLength(150)]
    public string? DepartmentName { get; set; }

    [MaxLength(150)]
    public string? BranchName { get; set; }

    [MaxLength(50)]
    public string? EmploymentType { get; set; }

    public DateOnly? PostedAt { get; set; }

    public DateOnly? ClosingDate { get; set; }

    [MaxLength(50)]
    public string? Status { get; set; }

    [Range(0, int.MaxValue)]
    public int? ApplicantsCount { get; set; }
}
