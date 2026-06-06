namespace ZelosHR.Api.Entities.Employees;

public sealed record EmployeeEducationDto(
    Guid Id,
    Guid EmployeeId,
    string Institution,
    string? Degree,
    string? FieldOfStudy,
    DateOnly? StartDate,
    DateOnly? EndDate,
    bool IsCurrent,
    Dictionary<string, string?>? CustomFields = null);

public sealed record EmployeeEducationWriteDto(
    string Institution,
    string? Degree,
    string? FieldOfStudy,
    DateOnly? StartDate,
    DateOnly? EndDate,
    bool IsCurrent,
    Dictionary<string, string?>? CustomFields = null);

public sealed record EmployeeCertificationDto(
    Guid Id,
    Guid EmployeeId,
    string Name,
    string? IssuingBody,
    DateOnly? IssueDate,
    DateOnly? ExpiryDate,
    string? CredentialUrl,
    Dictionary<string, string?>? CustomFields = null);

public sealed record EmployeeCertificationWriteDto(
    string Name,
    string? IssuingBody,
    DateOnly? IssueDate,
    DateOnly? ExpiryDate,
    string? CredentialUrl,
    Dictionary<string, string?>? CustomFields = null);

/// <summary>Include <c>id</c> from GET to update; omit only for a new row. Use <c>sync_certifications: true</c> on PUT for full-array replace.</summary>
public sealed record EmployeeCertificationUpsertDto(
    Guid? Id,
    string Name,
    string? IssuingBody,
    DateOnly? IssueDate,
    DateOnly? ExpiryDate,
    string? CredentialUrl,
    Dictionary<string, string?>? CustomFields = null);

public sealed record EmployeeWizardDocumentDto(
    Guid Id,
    Guid EmployeeId,
    string Category,
    string FileName,
    long FileSizeBytes,
    string BlobUrl,
    string ContentType,
    DateTimeOffset UploadedAt);
