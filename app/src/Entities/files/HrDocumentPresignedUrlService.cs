using ZelosHR.Api.Persistence.Repositories;
using ZelosHR.Api.Shared.Abstractions;

namespace ZelosHR.Api.Entities.Files;

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
                    "Use a document id from POST /api/v1/file/post/multiple (not a direct HTTPS URL). "
                    + "GET /api/v1/file/list returns presigned_url for display.",
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
}
