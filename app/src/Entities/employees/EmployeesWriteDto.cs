using System.ComponentModel.DataAnnotations;

namespace ZelosHR.Api.Entities.Employees;

public class CreateEmployeeControllerWriteDto
{
    [Required]
    [MaxLength(100)]
    public string? FirstName { get; set; }

    [MaxLength(100)]
    public string? MiddleName { get; set; }

    [Required]
    [MaxLength(100)]
    public string? LastName { get; set; }

    [Required]
    public DateOnly? DateOfBirth { get; set; }

    [Required]
    [MaxLength(50)]
    public string? Gender { get; set; }

    [Required]
    [MaxLength(100)]
    public string? Nationality { get; set; }

    [Required]
    [MaxLength(50)]
    public string? GhanaCardNumber { get; set; }

    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string? PersonalEmail { get; set; }

    [Required]
    [MaxLength(50)]
    public string? PersonalPhone { get; set; }

    [Required]
    [MaxLength(500)]
    public string? ResidentialAddress { get; set; }

    [Required]
    [MaxLength(100)]
    public string? GhanaPostGps { get; set; }
}

public sealed class CreateEmployeeServiceWriteDto
{
    public required string FirstName { get; init; }
    public string? MiddleName { get; init; }
    public required string LastName { get; init; }
    public required DateOnly DateOfBirth { get; init; }
    public required string Gender { get; init; }
    public required string Nationality { get; init; }
    public required string GhanaCardNumber { get; init; }
    public required string PersonalEmail { get; init; }
    public required string PersonalPhone { get; init; }
    public required string ResidentialAddress { get; init; }
    public required string GhanaPostGps { get; init; }
}
