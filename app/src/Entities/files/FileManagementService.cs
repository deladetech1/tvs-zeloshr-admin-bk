using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Persistence.Entities;
using ZelosHR.Api.Persistence.Repositories;
using ZelosHR.Api.Shared.Abstractions;

namespace ZelosHR.Api.Entities.Files;

public sealed class FileManagementService
{
    private readonly IEmployeeDocumentBlobStorage _blobs;
    private readonly IHrDocumentPathRepository _documents;
    private readonly FileManagementStorage _storageConfig;
    private readonly HrDocumentPresignedUrlService _presignedUrls;
    private readonly ITenantContext _tenant;
    private readonly ICurrentUserService _currentUser;

    public FileManagementService(
        IEmployeeDocumentBlobStorage blobs,
        IHrDocumentPathRepository documents,
        FileManagementStorage storageConfig,
        HrDocumentPresignedUrlService presignedUrls,
        ITenantContext tenant,
        ICurrentUserService currentUser)
    {
        _blobs = blobs;
        _documents = documents;
        _storageConfig = storageConfig;
        _presignedUrls = presignedUrls;
        _tenant = tenant;
        _currentUser = currentUser;
    }

    public async Task<Respons<IReadOnlyList<FileUploadMultipleReadDto>>> UploadMultipleAsync(
        IReadOnlyList<IFormFile> files,
        string blobPaths,
        string? descriptions,
        CancellationToken ct = default)
    {
        if (files.Count == 0)
        {
            return Respons<IReadOnlyList<FileUploadMultipleReadDto>>.ValidationError(
                new Dictionary<string, string> { ["files"] = "At least one file is required." });
        }

        string[] paths;
        if (ShouldAutoGenerateBlobPaths(blobPaths))
        {
            paths = files
                .Select(f => EmployeeBlobPathBuilder.BuildDocumentPath(
                    _tenant.TenantId, _tenant.OrgId, _tenant.BusId, f.FileName))
                .ToArray();
        }
        else
        {
            paths = blobPaths.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (paths.Length != 1 && paths.Length != files.Count)
            {
                return Respons<IReadOnlyList<FileUploadMultipleReadDto>>.ValidationError(
                    new Dictionary<string, string>
                    {
                        ["blob_paths"] =
                            $"You sent {paths.Length} blob path(s) for {files.Count} file(s). "
                            + "Omit blob_paths to auto-generate under {tenant}/{org}/{bus}/employees/documents/, "
                            + "send one path for all files, or send exactly one path per file.",
                    });
            }
        }

        if (paths.Length == 0)
        {
            return Respons<IReadOnlyList<FileUploadMultipleReadDto>>.ValidationError(
                new Dictionary<string, string> { ["blob_paths"] = "Could not resolve blob path(s) for upload." });
        }

        var descriptionList = (descriptions ?? string.Empty)
            .Split(',', StringSplitOptions.TrimEntries);

        var uploaded = new List<FileUploadMultipleReadDto>();
        for (var i = 0; i < files.Count; i++)
        {
            var file = files[i];
            if (file.Length == 0)
            {
                return Respons<IReadOnlyList<FileUploadMultipleReadDto>>.ValidationError(
                    new Dictionary<string, string> { [$"files[{i}]"] = "File is empty." });
            }

            var documentPath = paths.Length == 1 ? paths[0] : paths[i];
            var description = i < descriptionList.Length ? descriptionList[i] : null;
            var contentType = string.IsNullOrWhiteSpace(file.ContentType)
                ? "application/octet-stream"
                : file.ContentType;

            await using var stream = file.OpenReadStream();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms, ct);

            try
            {
                await _blobs.UploadAsync(
                    _storageConfig.ContainerName,
                    documentPath,
                    ms.ToArray(),
                    contentType,
                    ct);
            }
            catch (Exception ex)
            {
                return Respons<IReadOnlyList<FileUploadMultipleReadDto>>.Fail(
                    BlobStorageErrors.Map(ex),
                    statusCode: 502);
            }

            var id = Guid.NewGuid().ToString();
            await _documents.AddAsync(new HrDocumentPathEntity
            {
                Id = id,
                TenantId = _tenant.TenantId,
                DocumentPath = documentPath,
                FileName = file.FileName,
                Description = description,
                CreatedBy = _currentUser.UserId?.ToString(),
                Cdatetime = DateTimeOffset.UtcNow,
            }, ct);

            uploaded.Add(new FileUploadMultipleReadDto { Id = id });
        }

        return Respons<IReadOnlyList<FileUploadMultipleReadDto>>.Ok(uploaded);
    }

    internal static bool ShouldAutoGenerateBlobPaths(string? blobPaths)
    {
        if (string.IsNullOrWhiteSpace(blobPaths))
            return true;

        var trimmed = blobPaths.Trim();
        return trimmed.Equals("undefined", StringComparison.OrdinalIgnoreCase)
            || trimmed.Equals("null", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<Respons<FileResponseReadDto>> UpdateFileAsync(
        string documentId,
        IFormFile file,
        string? blobPath,
        string? description,
        CancellationToken ct = default)
    {
        if (file is null || file.Length == 0)
        {
            return Respons<FileResponseReadDto>.ValidationError(
                new Dictionary<string, string> { ["file"] = "File is required." });
        }

        var existing = await _documents.GetByIdAsync(documentId, _tenant.TenantId, ct);
        if (existing is null)
            return Respons<FileResponseReadDto>.NotFound("Document not found.");

        var targetPath = string.IsNullOrWhiteSpace(blobPath) ? existing.DocumentPath : blobPath.Trim();
        var contentType = string.IsNullOrWhiteSpace(file.ContentType)
            ? "application/octet-stream"
            : file.ContentType;

        await using var stream = file.OpenReadStream();
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, ct);

        try
        {
            await _blobs.UpdateAsync(
                _storageConfig.ContainerName,
                targetPath,
                ms.ToArray(),
                contentType,
                ct);
        }
        catch (Exception ex)
        {
            return Respons<FileResponseReadDto>.Fail(BlobStorageErrors.Map(ex), statusCode: 502);
        }

        existing.DocumentPath = targetPath;
        existing.FileName = file.FileName;
        if (description is not null)
            existing.Description = description;
        existing.UpdatedBy = _currentUser.UserId?.ToString();
        await _documents.UpdateAsync(existing, ct);

        var url = await ResolvePresignedUrlAsync(targetPath, ct);
        if (!url.Success || url.Data is null)
            return Respons<FileResponseReadDto>.Fail(url.Error ?? "Could not issue file URL.", statusCode: url.StatusCode);

        return Respons<FileResponseReadDto>.Ok(new FileResponseReadDto
        {
            Id = documentId,
            PresignedUrl = url.Data.PresignedUrl,
            Description = existing.Description,
            FileName = existing.FileName,
        });
    }

    public async Task<Respons<FileDeleteReadDto>> DeleteFileAsync(string documentId, CancellationToken ct = default)
    {
        var existing = await _documents.GetByIdAsync(documentId, _tenant.TenantId, ct);
        if (existing is null)
            return Respons<FileDeleteReadDto>.NotFound("Document not found.");

        try
        {
            await _blobs.DeleteAsync(_storageConfig.ContainerName, existing.DocumentPath, ct);
        }
        catch (Exception ex)
        {
            return Respons<FileDeleteReadDto>.Fail(BlobStorageErrors.Map(ex), statusCode: 502);
        }

        existing.DeleteStatus = "DELETED";
        existing.IsActive = false;
        existing.UpdatedBy = _currentUser.UserId?.ToString();
        await _documents.UpdateAsync(existing, ct);

        return Respons<FileDeleteReadDto>.Ok(new FileDeleteReadDto
        {
            BlobPath = existing.DocumentPath,
            ContainerName = _storageConfig.ContainerName,
            Message = "File deleted successfully.",
        });
    }

    public async Task<Respons<IReadOnlyList<FileResponseReadDto>>> ListDocumentsAsync(
        string documentIds, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(documentIds))
        {
            return Respons<IReadOnlyList<FileResponseReadDto>>.ValidationError(
                new Dictionary<string, string> { ["document_ids"] = "document_ids is required." });
        }

        var ids = documentIds.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var items = await _presignedUrls.ResolveDocumentsAsync(ids, ct);
        return Respons<IReadOnlyList<FileResponseReadDto>>.Ok(items);
    }

    private async Task<Respons<FileResponseReadDto>> ResolvePresignedUrlAsync(
        string blobPath, CancellationToken ct)
    {
        var presignedUrl = await _presignedUrls.ResolvePresignedUrlForBlobPathAsync(blobPath, ct);
        if (presignedUrl is null)
        {
            return Respons<FileResponseReadDto>.Fail(
                "Could not generate presigned URL.",
                statusCode: 502);
        }

        return Respons<FileResponseReadDto>.Ok(new FileResponseReadDto
        {
            Id = string.Empty,
            PresignedUrl = presignedUrl,
        });
    }
}
