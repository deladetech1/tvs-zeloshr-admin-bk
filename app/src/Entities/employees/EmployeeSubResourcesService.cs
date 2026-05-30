using Microsoft.Extensions.Options;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Persistence.Entities;
using ZelosHR.Api.Shared.Abstractions;
using ZelosHR.Api.Shared.Formatting;
using ZelosHR.Api.Shared.Infrastructure;

namespace ZelosHR.Api.Entities.Employees;

public sealed class EmployeeSubResourcesService
{
    private const long MaxDocumentBytes = 10 * 1024 * 1024;
    private static readonly HashSet<string> AllowedDocumentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf", "image/jpeg", "image/png",
    };

    private readonly IEmployeeEducationRepository _education;
    private readonly IEmployeeCertificationRepository _certifications;
    private readonly IEmployeeWizardDocumentRepository _documents;
    private readonly IEmployeeRepository _employees;
    private readonly IFileStorageService _files;
    private readonly AzureStorageOptions _storage;
    private readonly ITenantContext _tenant;
    private readonly ICurrentUserService _currentUser;

    public EmployeeSubResourcesService(
        IEmployeeEducationRepository education,
        IEmployeeCertificationRepository certifications,
        IEmployeeWizardDocumentRepository documents,
        IEmployeeRepository employees,
        IFileStorageService files,
        IOptions<AzureStorageOptions> storage,
        ITenantContext tenant,
        ICurrentUserService currentUser)
    {
        _education = education;
        _certifications = certifications;
        _documents = documents;
        _employees = employees;
        _files = files;
        _storage = storage.Value;
        _tenant = tenant;
        _currentUser = currentUser;
    }

    public async Task<Respons<IReadOnlyList<EmployeeEducationDto>>> ListEducationAsync(
        Guid employeeId, CancellationToken ct = default)
    {
        if (!await _education.EmployeeExistsAsync(employeeId, _tenant.TenantId, _tenant.OrgId, ct))
            return NotFoundListEducation();

        var rows = await _education.ListByEmployeeAsync(employeeId, _tenant.TenantId, _tenant.OrgId, ct);
        return Respons<IReadOnlyList<EmployeeEducationDto>>.Ok(rows.Select(ToEducationDto).ToList());
    }

    public async Task<Respons<EmployeeEducationDto>> AddEducationAsync(
        Guid employeeId, EmployeeEducationWriteDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Institution))
            return ValidationEducation("institution", "Institution is required.");

        if (!await _education.EmployeeExistsAsync(employeeId, _tenant.TenantId, _tenant.OrgId, ct))
            return Respons<EmployeeEducationDto>.Fail("Employee not found.", statusCode: 404);

        var entity = await _education.AddAsync(new EmployeeEducationEntity
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            Institution = dto.Institution.Trim(),
            Degree = dto.Degree,
            FieldOfStudy = dto.FieldOfStudy,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            IsCurrent = dto.IsCurrent,
        }, ct);

        return Respons<EmployeeEducationDto>.Ok(ToEducationDto(entity));
    }

    public async Task<Respons<EmployeeEducationDto>> UpdateEducationAsync(
        Guid employeeId, Guid educationId, EmployeeEducationWriteDto dto, CancellationToken ct = default)
    {
        var existing = await _education.GetByIdAsync(educationId, employeeId, _tenant.TenantId, _tenant.OrgId, ct);
        if (existing is null)
            return Respons<EmployeeEducationDto>.Fail("Education record not found.", statusCode: 404);

        existing.Institution = dto.Institution.Trim();
        existing.Degree = dto.Degree;
        existing.FieldOfStudy = dto.FieldOfStudy;
        existing.StartDate = dto.StartDate;
        existing.EndDate = dto.EndDate;
        existing.IsCurrent = dto.IsCurrent;
        await _education.UpdateAsync(existing, ct);
        return Respons<EmployeeEducationDto>.Ok(ToEducationDto(existing));
    }

    public async Task<Respons<object>> DeleteEducationAsync(
        Guid employeeId, Guid educationId, CancellationToken ct = default)
    {
        if (!await _education.DeleteAsync(educationId, employeeId, _tenant.TenantId, _tenant.OrgId, ct))
            return Respons<object>.Fail("Education record not found.", statusCode: 404);
        return Respons<object>.Ok(new { id = educationId });
    }

    public async Task<Respons<IReadOnlyList<EmployeeCertificationDto>>> ListCertificationsAsync(
        Guid employeeId, CancellationToken ct = default)
    {
        if (!await _certifications.EmployeeExistsAsync(employeeId, _tenant.TenantId, _tenant.OrgId, ct))
            return Respons<IReadOnlyList<EmployeeCertificationDto>>.Fail("Employee not found.", statusCode: 404);

        var rows = await _certifications.ListByEmployeeAsync(employeeId, _tenant.TenantId, _tenant.OrgId, ct);
        return Respons<IReadOnlyList<EmployeeCertificationDto>>.Ok(rows.Select(ToCertificationDto).ToList());
    }

    public async Task<Respons<EmployeeCertificationDto>> AddCertificationAsync(
        Guid employeeId, EmployeeCertificationWriteDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return Respons<EmployeeCertificationDto>.ValidationError(
                new Dictionary<string, string> { ["name"] = "Name is required." });

        if (!await _certifications.EmployeeExistsAsync(employeeId, _tenant.TenantId, _tenant.OrgId, ct))
            return Respons<EmployeeCertificationDto>.Fail("Employee not found.", statusCode: 404);

        var entity = await _certifications.AddAsync(new EmployeeCertificationEntity
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            Name = dto.Name.Trim(),
            IssuingBody = dto.IssuingBody,
            IssueDate = dto.IssueDate,
            ExpiryDate = dto.ExpiryDate,
            CredentialUrl = dto.CredentialUrl,
        }, ct);

        return Respons<EmployeeCertificationDto>.Ok(ToCertificationDto(entity));
    }

    public async Task<Respons<EmployeeCertificationDto>> UpdateCertificationAsync(
        Guid employeeId, Guid certId, EmployeeCertificationWriteDto dto, CancellationToken ct = default)
    {
        var existing = await _certifications.GetByIdAsync(certId, employeeId, _tenant.TenantId, _tenant.OrgId, ct);
        if (existing is null)
            return Respons<EmployeeCertificationDto>.Fail("Certification not found.", statusCode: 404);

        existing.Name = dto.Name.Trim();
        existing.IssuingBody = dto.IssuingBody;
        existing.IssueDate = dto.IssueDate;
        existing.ExpiryDate = dto.ExpiryDate;
        existing.CredentialUrl = dto.CredentialUrl;
        await _certifications.UpdateAsync(existing, ct);
        return Respons<EmployeeCertificationDto>.Ok(ToCertificationDto(existing));
    }

    public async Task<Respons<object>> DeleteCertificationAsync(
        Guid employeeId, Guid certId, CancellationToken ct = default)
    {
        if (!await _certifications.DeleteAsync(certId, employeeId, _tenant.TenantId, _tenant.OrgId, ct))
            return Respons<object>.Fail("Certification not found.", statusCode: 404);
        return Respons<object>.Ok(new { id = certId });
    }

    public async Task<Respons<IReadOnlyList<EmployeeWizardDocumentDto>>> ListDocumentsAsync(
        Guid employeeId, string? category, CancellationToken ct = default)
    {
        if (!await _documents.EmployeeExistsAsync(employeeId, _tenant.TenantId, _tenant.OrgId, ct))
            return Respons<IReadOnlyList<EmployeeWizardDocumentDto>>.Fail("Employee not found.", statusCode: 404);

        var rows = await _documents.ListByEmployeeAsync(
            employeeId, _tenant.TenantId, _tenant.OrgId, category, ct);
        return Respons<IReadOnlyList<EmployeeWizardDocumentDto>>.Ok(rows.Select(ToDocumentDto).ToList());
    }

    public async Task<Respons<EmployeeWizardDocumentDto>> UploadDocumentAsync(
        Guid employeeId,
        string category,
        Stream fileStream,
        string fileName,
        string contentType,
        long fileSize,
        CancellationToken ct = default)
    {
        if (fileSize > MaxDocumentBytes)
            return Respons<EmployeeWizardDocumentDto>.ValidationError(
                new Dictionary<string, string> { ["file"] = "File must not exceed 10 MB." });

        if (!AllowedDocumentTypes.Contains(contentType))
            return Respons<EmployeeWizardDocumentDto>.ValidationError(
                new Dictionary<string, string> { ["contentType"] = "Allowed types: PDF, JPEG, PNG." });

        var emp = await _employees.GetByIdScopedAsync(employeeId, _tenant.TenantId, _tenant.OrgId, ct);
        if (emp is null)
            return Respons<EmployeeWizardDocumentDto>.Fail("Employee not found.", statusCode: 404);

        var blobUrl = await _files.UploadAsync(
            fileStream,
            fileName,
            contentType,
            _storage.DocumentsContainer,
            _tenant.TenantId,
            employeeId,
            ct);

        var fullName = NameFormatting.ResolveFullName(emp.FullName, emp.FirstName, emp.MiddleName, emp.LastName);
        var entity = await _documents.AddAsync(new EmployeeDocumentEntity
        {
            Id = Guid.NewGuid(),
            TenantId = _tenant.TenantId,
            OrgId = _tenant.OrgId,
            EmployeeId = employeeId,
            EmployeeFullName = fullName,
            Category = category.Trim(),
            FileName = fileName,
            FileSizeBytes = fileSize,
            BlobUrl = blobUrl,
            ContentType = contentType,
            UploadedBy = _currentUser.UserId?.ToString(),
        }, ct);

        return Respons<EmployeeWizardDocumentDto>.Ok(ToDocumentDto(entity));
    }

    public async Task<Respons<object>> DeleteDocumentAsync(
        Guid employeeId, Guid documentId, CancellationToken ct = default)
    {
        if (!await _documents.SoftDeleteAsync(documentId, employeeId, _tenant.TenantId, _tenant.OrgId, ct))
            return Respons<object>.Fail("Document not found.", statusCode: 404);
        return Respons<object>.Ok(new { id = documentId });
    }

    private static EmployeeEducationDto ToEducationDto(EmployeeEducationEntity e) => new(
        e.Id, e.EmployeeId, e.Institution, e.Degree, e.FieldOfStudy, e.StartDate, e.EndDate, e.IsCurrent);

    private static EmployeeCertificationDto ToCertificationDto(EmployeeCertificationEntity c) => new(
        c.Id, c.EmployeeId, c.Name, c.IssuingBody, c.IssueDate, c.ExpiryDate, c.CredentialUrl);

    private static EmployeeWizardDocumentDto ToDocumentDto(EmployeeDocumentEntity d) => new(
        d.Id, d.EmployeeId, d.Category, d.FileName, d.FileSizeBytes, d.BlobUrl, d.ContentType, d.UploadedAt);

    private static Respons<IReadOnlyList<EmployeeEducationDto>> NotFoundListEducation() =>
        Respons<IReadOnlyList<EmployeeEducationDto>>.Fail("Employee not found.", statusCode: 404);

    private static Respons<EmployeeEducationDto> ValidationEducation(string key, string message) =>
        Respons<EmployeeEducationDto>.ValidationError(new Dictionary<string, string> { [key] = message });
}
