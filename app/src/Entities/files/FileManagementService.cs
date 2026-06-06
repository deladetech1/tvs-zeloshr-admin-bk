using Trovesuite.Package.Storage;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Persistence.Entities;
using ZelosHR.Api.Persistence.Repositories;
using ZelosHR.Api.Shared.Abstractions;

namespace ZelosHR.Api.Entities.Files;

public sealed class FileManagementService
{
    private readonly IStorageService _storage;
    private readonly IHrDocumentPathRepository _documents;
    private readonly FileManagementStorage _storageConfig;
    private readonly HrDocumentPresignedUrlService _presignedUrls;
    private readonly ITenantContext _tenant;
    private readonly ICurrentUserService _currentUser;

    public FileManagementService(
        IStorageService storage,
        IHrDocumentPathRepository documents,
        FileManagementStorage storageConfig,
        HrDocumentPresignedUrlService presignedUrls,
        ITenantContext tenant,
        ICurrentUserService currentUser)
    {
        _storage = storage;
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

        if (string.IsNullOrWhiteSpace(blobPaths))
        {
            return Respons<IReadOnlyList<FileUploadMultipleReadDto>>.ValidationError(
                new Dictionary<string, string> { ["blob_paths"] = "blob_paths is required." });
        }

        var paths = blobPaths.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (paths.Length != 1 && paths.Length != files.Count)
        {
            return Respons<IReadOnlyList<FileUploadMultipleReadDto>>.ValidationError(
                new Dictionary<string, string>
                {
                    ["blob_paths"] = "Provide one path for all files or one path per file (comma-separated).",
                });
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

            await using var stream = file.OpenReadStream();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms, ct);

            var upload = await _storage.UploadFileAsync(new StorageFileUploadServiceWriteDto
            {
                StorageAccountUrl = _storageConfig.StorageAccountUrl,
                ContainerName = _storageConfig.ContainerName,
                BlobName = documentPath,
                FileContent = ms.ToArray(),
                ContentType = string.IsNullOrWhiteSpace(file.ContentType)
                    ? "application/octet-stream"
                    : file.ContentType,
            }, ct);

            if (!upload.Success || upload.Data is null)
            {
                return Respons<IReadOnlyList<FileUploadMultipleReadDto>>.Fail(
                    upload.Error ?? upload.Detail ?? "File upload failed.",
                    statusCode: upload.StatusCode > 0 ? upload.StatusCode : 502);
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

        await using var stream = file.OpenReadStream();
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, ct);

        var updated = await _storage.UpdateFileAsync(new StorageFileUpdateServiceWriteDto
        {
            StorageAccountUrl = _storageConfig.StorageAccountUrl,
            ContainerName = _storageConfig.ContainerName,
            BlobName = targetPath,
            FileContent = ms.ToArray(),
            ContentType = string.IsNullOrWhiteSpace(file.ContentType)
                ? "application/octet-stream"
                : file.ContentType,
        }, ct);

        if (!updated.Success)
        {
            return Respons<FileResponseReadDto>.Fail(
                updated.Error ?? updated.Detail ?? "File update failed.",
                statusCode: updated.StatusCode > 0 ? updated.StatusCode : 502);
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

        var deleted = await _storage.DeleteFileAsync(new StorageFileDeleteServiceWriteDto
        {
            StorageAccountUrl = _storageConfig.StorageAccountUrl,
            ContainerName = _storageConfig.ContainerName,
            BlobName = existing.DocumentPath,
        }, ct);

        if (!deleted.Success)
        {
            return Respons<FileDeleteReadDto>.Fail(
                deleted.Error ?? deleted.Detail ?? "File delete failed.",
                statusCode: deleted.StatusCode > 0 ? deleted.StatusCode : 502);
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
        var rows = await _documents.GetByIdsAsync(ids, _tenant.TenantId, ct);
        var byId = rows.ToDictionary(x => x.Id, StringComparer.Ordinal);

        var items = new List<FileResponseReadDto>();
        foreach (var id in ids)
        {
            if (!byId.TryGetValue(id, out var row))
                continue;

            var url = await ResolvePresignedUrlAsync(row.DocumentPath, ct);
            if (!url.Success || url.Data is null)
                continue;

            items.Add(new FileResponseReadDto
            {
                Id = row.Id,
                PresignedUrl = url.Data.PresignedUrl,
                Description = row.Description,
                FileName = row.FileName,
            });
        }

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
