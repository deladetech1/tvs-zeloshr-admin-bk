using ZelosHR.Api.Persistence.Repositories;
using ZelosHR.Api.Shared.Abstractions;

namespace ZelosHR.Api.Entities.Files;

/// <summary>Result of normalizing <c>profile_url</c> on write (document id vs presigned round-trip).</summary>
public readonly record struct ProfileUrlWriteResolution(
    bool ShouldApply,
    string? Value,
    Dictionary<string, string>? Error);

/// <summary>
/// Resolves <c>human_resource.hr_document_paths</c> IDs to Azure presigned URLs (MyStoreGuard file/list pattern).
/// </summary>
public sealed class HrDocumentPresignedUrlService
{
    private const int DefaultPresignedExpiryHours = 24;

    private readonly IEmployeeDocumentBlobStorage _blobs;
    private readonly IHrDocumentPathRepository _documents;
    private readonly FileManagementStorage _storageConfig;
    private readonly ITenantContext _tenant;

    public HrDocumentPresignedUrlService(
        IEmployeeDocumentBlobStorage blobs,
        IHrDocumentPathRepository documents,
        FileManagementStorage storageConfig,
        ITenantContext tenant)
    {
        _blobs = blobs;
        _documents = documents;
        _storageConfig = storageConfig;
        _tenant = tenant;
    }

    internal static bool IsLegacyHttpUrl(string value) =>
        value.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
        || value.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

    /// <summary>Validate write payload: document registry id (from file upload), not a direct blob URL.</summary>
    public async Task<Dictionary<string, string>?> ValidateDocumentReferenceAsync(
        string fieldPath,
        string? value,
        CancellationToken ct = default)
    {
        if (value is null)
            return null;

        var trimmed = value.Trim();
        if (trimmed.Length == 0)
            return null;

        if (IsLegacyHttpUrl(trimmed))
        {
            return new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [fieldPath] =
                    "Send a document id from POST /api/v1/file/post/multiple (not the presigned URL from GET). "
                    + "Omit profile_url to leave unchanged, or pass \"\" to clear.",
            };
        }

        var row = await _documents.GetByIdAsync(trimmed, _tenant.TenantId, ct);
        if (row is null)
        {
            return new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [fieldPath] = "Document not found for this tenant.",
            };
        }

        return null;
    }

    /// <summary>
    /// Accept document ids on write; treat an unchanged presigned URL from GET as a no-op on update.
    /// </summary>
    public async Task<ProfileUrlWriteResolution> ResolveProfileUrlForWriteAsync(
        string fieldPath,
        string submittedValue,
        string? currentStoredReference,
        CancellationToken ct = default)
    {
        var trimmed = submittedValue.Trim();
        if (trimmed.Length == 0)
            return new ProfileUrlWriteResolution(true, null, null);

        if (!IsLegacyHttpUrl(trimmed))
        {
            if (!string.IsNullOrWhiteSpace(currentStoredReference)
                && string.Equals(trimmed, currentStoredReference.Trim(), StringComparison.Ordinal))
            {
                return new ProfileUrlWriteResolution(false, null, null);
            }

            var validationError = await ValidateDocumentReferenceAsync(fieldPath, trimmed, ct);
            return validationError is null
                ? new ProfileUrlWriteResolution(true, trimmed, null)
                : new ProfileUrlWriteResolution(false, null, validationError);
        }

        if (!string.IsNullOrWhiteSpace(currentStoredReference))
        {
            if (IsLegacyHttpUrl(currentStoredReference)
                && string.Equals(trimmed, currentStoredReference.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return new ProfileUrlWriteResolution(false, null, null);
            }

            if (!IsLegacyHttpUrl(currentStoredReference))
            {
                var currentDisplay = await ResolveDisplayUrlAsync(currentStoredReference, ct);
                if (currentDisplay is not null && PresignedUrlsReferToSameBlob(trimmed, currentDisplay))
                    return new ProfileUrlWriteResolution(false, null, null);
            }
        }

        var error = await ValidateDocumentReferenceAsync(fieldPath, trimmed, ct);
        return new ProfileUrlWriteResolution(false, null, error);
    }

    internal static bool PresignedUrlsReferToSameBlob(string left, string right)
    {
        if (string.Equals(left, right, StringComparison.OrdinalIgnoreCase))
            return true;

        if (!Uri.TryCreate(left, UriKind.Absolute, out var leftUri)
            || !Uri.TryCreate(right, UriKind.Absolute, out var rightUri))
        {
            return false;
        }

        return leftUri.Host.Equals(rightUri.Host, StringComparison.OrdinalIgnoreCase)
            && leftUri.AbsolutePath.Equals(rightUri.AbsolutePath, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Maps stored reference to a client-facing URL: presigned (24h) for document ids; legacy https kept as-is.
    /// </summary>
    public async Task<string?> ResolveDisplayUrlAsync(string? storedReference, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(storedReference))
            return null;

        var trimmed = storedReference.Trim();
        if (IsLegacyHttpUrl(trimmed))
            return trimmed;

        return await ResolvePresignedUrlForDocumentIdAsync(trimmed, ct);
    }

    public async Task<IReadOnlyDictionary<string, string?>> ResolveDisplayUrlsAsync(
        IEnumerable<string?> storedReferences,
        CancellationToken ct = default)
    {
        var distinct = storedReferences
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(r => r!.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var result = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var reference in distinct)
        {
            result[reference] = await ResolveDisplayUrlAsync(reference, ct);
        }

        return result;
    }

    public async Task<string?> ResolvePresignedUrlForDocumentIdAsync(string documentId, CancellationToken ct = default)
    {
        var row = await _documents.GetByIdAsync(documentId, _tenant.TenantId, ct);
        if (row is null)
            return null;

        return await ResolvePresignedUrlForBlobPathAsync(row.DocumentPath, ct);
    }

    public async Task<string?> ResolvePresignedUrlForBlobPathAsync(string blobPath, CancellationToken ct = default)
    {
        try
        {
            return await _blobs.GetPresignedReadUrlAsync(
                _storageConfig.ContainerName,
                blobPath,
                DefaultPresignedExpiryHours,
                ct);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Resolve registry IDs to metadata + presigned URLs (same shape as <c>GET /file/list</c>).</summary>
    public async Task<IReadOnlyList<FileResponseReadDto>> ResolveDocumentsAsync(
        IEnumerable<string> documentIds,
        CancellationToken ct = default)
    {
        var ids = documentIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .ToList();
        if (ids.Count == 0)
            return [];

        var rows = await _documents.GetByIdsAsync(ids, _tenant.TenantId, ct);
        var byId = rows.ToDictionary(x => x.Id, StringComparer.Ordinal);

        var items = new List<FileResponseReadDto>();
        foreach (var id in ids)
        {
            if (!byId.TryGetValue(id, out var row))
                continue;

            var presignedUrl = await ResolvePresignedUrlForBlobPathAsync(row.DocumentPath, ct);
            if (presignedUrl is null)
                continue;

            items.Add(new FileResponseReadDto
            {
                Id = row.Id,
                PresignedUrl = presignedUrl,
                Description = row.Description,
                FileName = row.FileName,
            });
        }

        return items;
    }

    internal static DocumentReadDto ToEmbeddedDocument(FileResponseReadDto file) =>
        new()
        {
            DocId = file.Id,
            PresignedUrl = file.PresignedUrl,
            Description = file.Description,
            Name = file.FileName,
        };

    /// <summary>Resolve one stored registry id (or legacy URL) to MyStoreGuard <c>DocumentReadDto</c>.</summary>
    public async Task<DocumentReadDto?> ResolveDocumentReadAsync(
        string? storedReference,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(storedReference))
            return null;

        var trimmed = storedReference.Trim();
        if (IsLegacyHttpUrl(trimmed))
        {
            return new DocumentReadDto
            {
                DocId = trimmed,
                PresignedUrl = trimmed,
                Description = null,
                Name = null,
            };
        }

        var docs = await ResolveDocumentsAsync([trimmed], ct);
        var doc = docs.FirstOrDefault();
        return doc is null ? null : ToEmbeddedDocument(doc);
    }

    public async Task<IReadOnlyDictionary<string, DocumentReadDto?>> ResolveDocumentReadsAsync(
        IEnumerable<string?> storedReferences,
        CancellationToken ct = default)
    {
        var distinct = storedReferences
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(r => r!.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var result = new Dictionary<string, DocumentReadDto?>(StringComparer.Ordinal);
        foreach (var reference in distinct)
            result[reference] = await ResolveDocumentReadAsync(reference, ct);

        return result;
    }
}
