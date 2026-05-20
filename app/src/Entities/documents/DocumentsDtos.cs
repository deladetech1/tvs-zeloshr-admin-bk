namespace ZelosHR.Api.Entities.Documents;

public sealed class DocumentsSummaryDto
{
    public int TotalDocuments { get; init; }
    public int ContractDocuments { get; init; }
    public int NationalIdDocuments { get; init; }
    public int CertificateDocuments { get; init; }
}

public sealed class DocumentListItemDto
{
    public required string DocumentId { get; init; }
    public required string EmployeeId { get; init; }
    public required string EmployeeFullName { get; init; }
    public required string DocumentName { get; init; }
    public required string Category { get; init; }
    public int FileSizeKb { get; init; }
    public required string UploadedBy { get; init; }
    public DateTimeOffset UploadedAt { get; init; }
    public required string Status { get; init; }
}

public sealed class DocumentListDto
{
    public DocumentsSummaryDto Summary { get; init; } = new();
    public IReadOnlyList<DocumentListItemDto> Items { get; init; } = [];
}
