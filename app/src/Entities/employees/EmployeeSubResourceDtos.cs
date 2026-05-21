namespace ZelosHR.Api.Entities.Employees;

public sealed record EmployeeEducationDto(
    Guid Id,
    Guid EmployeeId,
    string Institution,
    string? Degree,
    string? FieldOfStudy,
    int? StartYear,
    int? EndYear,
    bool IsCurrent);

public sealed record EmployeeEducationWriteDto(
    string Institution,
    string? Degree,
    string? FieldOfStudy,
    int? StartYear,
    int? EndYear,
    bool IsCurrent);

public sealed record EmployeeCertificationDto(
    Guid Id,
    Guid EmployeeId,
    string Name,
    string? IssuingBody,
    DateOnly? IssueDate,
    DateOnly? ExpiryDate,
    string? CredentialId);

public sealed record EmployeeCertificationWriteDto(
    string Name,
    string? IssuingBody,
    DateOnly? IssueDate,
    DateOnly? ExpiryDate,
    string? CredentialId);

public sealed record EmployeeWizardDocumentDto(
    Guid Id,
    Guid EmployeeId,
    string Category,
    string FileName,
    long FileSizeBytes,
    string BlobUrl,
    string ContentType,
    DateTimeOffset UploadedAt);
