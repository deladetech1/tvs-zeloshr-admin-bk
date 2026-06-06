using ZelosHR.Api.Shared.Abstractions;

namespace ZelosHR.Api.Shared.Infrastructure;

/// <summary>Stub blob storage when Azure is not configured (local dev / unit tests).</summary>
public sealed class LocalDevEmployeeDocumentBlobStorage : IEmployeeDocumentBlobStorage
{
    public Task UploadAsync(
        string containerName,
        string blobPath,
        byte[] content,
        string contentType,
        CancellationToken ct = default) => Task.CompletedTask;

    public Task UpdateAsync(
        string containerName,
        string blobPath,
        byte[] content,
        string contentType,
        CancellationToken ct = default) => Task.CompletedTask;

    public Task DeleteAsync(string containerName, string blobPath, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task<string?> GetPresignedReadUrlAsync(
        string containerName,
        string blobPath,
        int expiryHours,
        CancellationToken ct = default)
    {
        var url =
            $"https://localhost/dev-storage/{containerName}/{blobPath}?expiry={expiryHours}h";
        return Task.FromResult<string?>(url);
    }
}
