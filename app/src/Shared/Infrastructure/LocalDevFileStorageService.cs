using ZelosHR.Api.Shared.Abstractions;

namespace ZelosHR.Api.Shared.Infrastructure;

/// <summary>Used when Azure Storage is not configured (local dev / unit tests).</summary>
public sealed class LocalDevFileStorageService : IFileStorageService
{
    public Task<string> UploadAsync(
        Stream stream,
        string fileName,
        string contentType,
        string containerName,
        string tenantId,
        Guid employeeId,
        CancellationToken ct = default)
    {
        var url = $"https://localhost/dev-storage/{containerName}/{tenantId}/{employeeId}/{Guid.NewGuid()}/{fileName}";
        return Task.FromResult(url);
    }

    public Task DeleteAsync(string blobUrl, CancellationToken ct = default) => Task.CompletedTask;

    public Task<Stream> DownloadAsync(string blobUrl, CancellationToken ct = default) =>
        Task.FromResult<Stream>(new MemoryStream());
}
