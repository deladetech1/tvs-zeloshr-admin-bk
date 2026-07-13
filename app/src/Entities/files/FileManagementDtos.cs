namespace ZelosHR.Api.Entities.Files;

/// <summary>Registry ID returned after <c>POST /api/v1/file/post/multiple</c>. Pass to employee <c>document_ids</c>.</summary>
public sealed class FileUploadMultipleReadDto
{
    /// <summary>Document registry ID (<c>core_platform.cp_document_paths.id</c>). String, not UUID.</summary>
    public required string Id { get; init; }
}

/// <summary>
/// Embedded document on entity read (MyStoreGuard <c>DocumentReadDto</c> — e.g. product or employee <c>documents[]</c>).
/// </summary>
public sealed class DocumentReadDto
{
    /// <summary>Document registry ID (<c>cp_document_paths.id</c>).</summary>
    public required string DocId { get; init; }

    /// <summary>Optional label from upload <c>descriptions</c> or file update.</summary>
    public string? Description { get; init; }

    /// <summary>Original filename from multipart upload.</summary>
    public string? Name { get; init; }

    /// <summary>Azure Blob presigned URL (24h expiry).</summary>
    public required string PresignedUrl { get; init; }
}

/// <summary>Document metadata + time-limited download URL from <c>GET /api/v1/file/list</c> or <c>PUT /file/put</c>.</summary>
public sealed class FileResponseReadDto
{
    /// <summary>Registry ID (<c>cp_document_paths.id</c>).</summary>
    public required string Id { get; init; }

    /// <summary>Azure Blob presigned URL (24h expiry). Open in browser or pass to download client.</summary>
    public required string PresignedUrl { get; init; }

    /// <summary>Optional label from upload <c>descriptions</c> query or update.</summary>
    public string? Description { get; init; }

    /// <summary>Original filename from multipart upload.</summary>
    public string? FileName { get; init; }
}

/// <summary>Confirmation after <c>DELETE /api/v1/file/delete</c> — shows where the blob was stored.</summary>
public sealed class FileDeleteReadDto
{
    /// <summary>Logical blob path that was deleted (same value sent as <c>blob_paths</c> on upload).</summary>
    public required string BlobPath { get; init; }

    /// <summary>Azure container name (server config — <c>zeloshr</c>). Not sent by clients on upload.</summary>
    public required string ContainerName { get; init; }

    /// <summary>Human-readable result message.</summary>
    public required string Message { get; init; }
}
