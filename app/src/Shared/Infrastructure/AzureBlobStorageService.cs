using Azure.Storage.Blobs;
using Microsoft.Extensions.Options;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Shared.Abstractions;

namespace ZelosHR.Api.Shared.Infrastructure;

public sealed class AzureStorageOptions
{
    public const string SectionName = "AzureStorage";
    public string ConnectionString { get; set; } = "";
    public string ProfilePhotosContainer { get; set; } = "profile-photos";
    public string DocumentsContainer { get; set; } = "employee-documents";
}

public sealed class AzureBlobStorageService : IFileStorageService
{
    private readonly BlobServiceClient _client;
    private readonly AzureStorageOptions _options;

    public AzureBlobStorageService(BlobServiceClient client, IOptions<AzureStorageOptions> options)
    {
        _client = client;
        _options = options.Value;
    }

    public async Task<string> UploadAsync(
        Stream stream,
        string fileName,
        string contentType,
        string containerName,
        string tenantId,
        Guid employeeId,
        CancellationToken ct = default)
    {
        var container = _client.GetBlobContainerClient(containerName);
        await container.CreateIfNotExistsAsync(cancellationToken: ct);

        var blobName = $"{tenantId}/{employeeId}/{Guid.NewGuid()}/{fileName}";
        var blob = container.GetBlobClient(blobName);
        await blob.UploadAsync(stream, new Azure.Storage.Blobs.Models.BlobHttpHeaders
        {
            ContentType = contentType,
        }, cancellationToken: ct);

        return blob.Uri.ToString();
    }

    public async Task DeleteAsync(string blobUrl, CancellationToken ct = default)
    {
        if (!Uri.TryCreate(blobUrl, UriKind.Absolute, out var uri))
            return;

        var path = uri.AbsolutePath.TrimStart('/');
        var slash = path.IndexOf('/');
        if (slash <= 0)
            return;

        var containerName = path[..slash];
        var blobName = path[(slash + 1)..];
        await _client.GetBlobContainerClient(containerName)
            .GetBlobClient(blobName)
            .DeleteIfExistsAsync(cancellationToken: ct);
    }

    public async Task<Stream> DownloadAsync(string blobUrl, CancellationToken ct = default)
    {
        if (!Uri.TryCreate(blobUrl, UriKind.Absolute, out var uri))
            throw new ArgumentException("Invalid blob URL.", nameof(blobUrl));

        var path = uri.AbsolutePath.TrimStart('/');
        var slash = path.IndexOf('/');
        var containerName = path[..slash];
        var blobName = path[(slash + 1)..];
        var response = await _client.GetBlobContainerClient(containerName)
            .GetBlobClient(blobName)
            .DownloadStreamingAsync(cancellationToken: ct);
        return response.Value.Content;
    }
}
