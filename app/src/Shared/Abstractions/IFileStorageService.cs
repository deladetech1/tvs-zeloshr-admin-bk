namespace ZelosHR.Api.Shared.Abstractions;

public interface IFileStorageService
{
    Task<string> UploadAsync(
        Stream stream,
        string fileName,
        string contentType,
        string containerName,
        string tenantId,
        Guid employeeId,
        CancellationToken ct = default);

    Task DeleteAsync(string blobUrl, CancellationToken ct = default);

    Task<Stream> DownloadAsync(string blobUrl, CancellationToken ct = default);
}
