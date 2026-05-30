namespace ZelosHR.Api.Entities.Files;

/// <summary>Registry ID returned after <c>POST /api/v1/file/post/multiple</c>. Pass to employee <c>document_ids</c>.</summary>
public sealed class FileUploadMultipleReadDto
{
    /// <summary>Document registry ID (<c>human_resource.hr_document_paths.id</c>). String, not UUID.</summary>
    public required string Id { get; init; }
}

/// <summary>Document metadata + time-limited download URL from <c>GET /api/v1/file/list</c> or <c>PUT /file/put</c>.</summary>
public sealed class FileResponseReadDto
{
    /// <summary>Registry ID (<c>hr_document_paths.id</c>).</summary>
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

    /// <summary>Azure container name (server config — e.g. <c>employee-documents</c>). Not sent by clients on upload.</summary>
    public required string ContainerName { get; init; }

    /// <summary>Human-readable result message.</summary>
    public required string Message { get; init; }
}
