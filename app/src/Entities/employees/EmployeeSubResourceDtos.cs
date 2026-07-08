namespace ZelosHR.Api.Entities.Employees;

/// <summary>Education row on <c>GET /employees/get</c> (<c>id</c>, row fields, <c>custom_fields</c>).</summary>
public sealed record EmployeeEducationDto(
    Guid Id,
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

/// <summary>Include <c>id</c> from GET to update; omit to add. Use <c>sync_education: true</c> on PUT for full-array replace.</summary>
public sealed record EmployeeEducationUpsertDto(
    Guid? Id,
    string Institution,
    string? Degree,
    string? FieldOfStudy,
    DateOnly? StartDate,
    DateOnly? EndDate,
    bool IsCurrent,
    Dictionary<string, string?>? CustomFields = null);

/// <summary>Certification row on <c>GET /employees/get</c> (<c>id</c>, row fields, <c>custom_fields</c>).</summary>
public sealed record EmployeeCertificationDto(
    Guid Id,
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

/// <summary>Include <c>id</c> from GET to update; omit to add. Use <c>sync_certifications: true</c> on PUT for full-array replace.</summary>
public sealed record EmployeeCertificationUpsertDto(
    Guid? Id,
    string Name,
    string? IssuingBody,
    DateOnly? IssueDate,
    DateOnly? ExpiryDate,
    string? CredentialUrl,
    Dictionary<string, string?>? CustomFields = null);

/// <summary>Identification row on <c>GET /employees/get</c> (<c>identity.identifications[]</c>).</summary>
public sealed record EmployeeIdentificationDto(
    Guid Id,
    Guid IdTypeId,
    string? IdTypeName,
    string IdNumber,
    DateOnly? IdIssueDate,
    DateOnly? IdExpiryDate,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>Government / company ID document. <c>id_type_id</c> from <c>GET /id-card-types/list</c>.</summary>
public sealed record EmployeeIdentificationWriteDto(
    Guid IdTypeId,
    string IdNumber,
    DateOnly? IdIssueDate,
    DateOnly? IdExpiryDate);

/// <summary>Include <c>id</c> from GET to update; omit to add. Use <c>sync_identifications: true</c> on PUT for full-array replace.</summary>
public sealed record EmployeeIdentificationUpsertDto(
    Guid? Id,
    Guid IdTypeId,
    string IdNumber,
    DateOnly? IdIssueDate,
    DateOnly? IdExpiryDate);

public sealed record EmployeeWizardDocumentDto(
    Guid Id,
    Guid EmployeeId,
    string Category,
    string FileName,
    long FileSizeBytes,
    string BlobUrl,
    string ContentType,
    DateTimeOffset UploadedAt);
