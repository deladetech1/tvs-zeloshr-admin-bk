using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZelosHR.Api.Entities.Files;
using ZelosHR.Api.Shared.Abstractions;

namespace ZelosHR.Api.Shared.Infrastructure;

public sealed class AzureStorageOptions
{
    public const string SectionName = "AzureStorage";

    /// <summary>Local/dev only. Production uses <see cref="AccountName"/> + DefaultAzureCredential.</summary>
    public string ConnectionString { get; set; } = "";

    /// <summary>Storage account name (e.g. from Container App env / managed identity).</summary>
    public string AccountName { get; set; } = "";

    /// <summary>Optional override, e.g. Azurite or private endpoint URI.</summary>
    public string BlobServiceUri { get; set; } = "";

    public string ProfilePhotosContainer { get; set; } = "profile-photos";

    public string DocumentsContainer { get; set; } = "employee-documents";
}

public sealed class AzureBlobStorageService : IFileStorageService
{
    private readonly BlobServiceClient _client;
    private readonly AzureStorageOptions _options;
    private readonly ILogger<AzureBlobStorageService> _logger;

    public AzureBlobStorageService(
        BlobServiceClient client,
        IOptions<AzureStorageOptions> options,
        ILogger<AzureBlobStorageService> logger)
    {
        _client = client;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> UploadAsync(
        Stream stream,
        string fileName,
        string contentType,
        string containerName,
        string tenantId,
        string orgId,
        string busId,
        Guid employeeId,
        CancellationToken ct = default)
    {
        var container = _client.GetBlobContainerClient(containerName);
        await EnsureContainerReadyAsync(container, ct);

        var blobName = EmployeeBlobPathBuilder.BuildWizardDocumentPath(
            tenantId, orgId, busId, employeeId, fileName);
        var blob = container.GetBlobClient(blobName);

        await blob.UploadAsync(
            stream,
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = contentType },
            },
            ct);

        _logger.LogDebug(
            "Uploaded blob {BlobName} to container {Container}",
            blobName,
            containerName);

        return blob.Uri.ToString();
    }

    public async Task DeleteAsync(string blobUrl, CancellationToken ct = default)
    {
        if (!TryParseBlobPath(blobUrl, out var containerName, out var blobName))
            return;

        await _client.GetBlobContainerClient(containerName)
            .GetBlobClient(blobName)
            .DeleteIfExistsAsync(cancellationToken: ct);
    }

    public async Task<Stream> DownloadAsync(string blobUrl, CancellationToken ct = default)
    {
        if (!TryParseBlobPath(blobUrl, out var containerName, out var blobName))
            throw new ArgumentException("Invalid blob URL.", nameof(blobUrl));

        var response = await _client.GetBlobContainerClient(containerName)
            .GetBlobClient(blobName)
            .DownloadStreamingAsync(cancellationToken: ct);
        return response.Value.Content;
    }

    private async Task EnsureContainerReadyAsync(BlobContainerClient container, CancellationToken ct)
    {
        if (await container.ExistsAsync(ct))
            return;

        try
        {
            await container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: ct);
        }
        catch (RequestFailedException ex) when (ex.Status is 403 or 409)
        {
            if (await container.ExistsAsync(ct))
                return;

            _logger.LogError(
                ex,
                "Blob container {Container} is missing and could not be created (status {Status}). "
                + "Provision containers on the storage account or grant create permission to the managed identity.",
                container.Name,
                ex.Status);
            throw;
        }
    }

    private static bool TryParseBlobPath(string blobUrl, out string containerName, out string blobName)
    {
        containerName = "";
        blobName = "";

        if (!Uri.TryCreate(blobUrl, UriKind.Absolute, out var uri))
            return false;

        var path = uri.AbsolutePath.TrimStart('/');
        var slash = path.IndexOf('/');
        if (slash <= 0)
            return false;

        containerName = path[..slash];
        blobName = path[(slash + 1)..];
        return true;
    }
}
