namespace ZelosHR.Api.Shared.Abstractions;

/// <summary>Employee document blobs in Azure Storage (registry paths from file management).</summary>
public interface IEmployeeDocumentBlobStorage
{
    Task UploadAsync(
        string containerName,
        string blobPath,
        byte[] content,
        string contentType,
        CancellationToken ct = default);

    Task UpdateAsync(
        string containerName,
        string blobPath,
        byte[] content,
        string contentType,
        CancellationToken ct = default);

    Task DeleteAsync(string containerName, string blobPath, CancellationToken ct = default);

    Task<string?> GetPresignedReadUrlAsync(
        string containerName,
        string blobPath,
        int expiryHours,
        CancellationToken ct = default);
}
