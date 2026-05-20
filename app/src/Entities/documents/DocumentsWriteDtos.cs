using System.ComponentModel.DataAnnotations;

namespace ZelosHR.Api.Entities.Documents;

public sealed class CreateDocumentDto
{
    [Required]
    public Guid EmployeeId { get; set; }

    [Required]
    [MaxLength(255)]
    public string? DocumentName { get; set; }

    [Required]
    [MaxLength(80)]
    public string? Category { get; set; }

    [Range(1, int.MaxValue)]
    public int FileSizeKb { get; set; } = 1;

    [Required]
    [MaxLength(200)]
    public string? UploadedBy { get; set; }

    [MaxLength(50)]
    public string? Status { get; set; }
}

public sealed class UpdateDocumentDto
{
    [MaxLength(255)]
    public string? DocumentName { get; set; }

    [MaxLength(80)]
    public string? Category { get; set; }

    [Range(1, int.MaxValue)]
    public int? FileSizeKb { get; set; }

    [MaxLength(200)]
    public string? UploadedBy { get; set; }

    [MaxLength(50)]
    public string? Status { get; set; }
}
