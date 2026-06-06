using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Logging;
using ZelosHR.Api.Shared.Abstractions;

namespace ZelosHR.Api.Shared.Infrastructure;

public sealed class EmployeeDocumentBlobStorage : IEmployeeDocumentBlobStorage
{
    private readonly BlobServiceClient _client;
    private readonly ILogger<EmployeeDocumentBlobStorage> _logger;

    public EmployeeDocumentBlobStorage(
        BlobServiceClient client,
        ILogger<EmployeeDocumentBlobStorage> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task UploadAsync(
        string containerName,
        string blobPath,
        byte[] content,
        string contentType,
        CancellationToken ct = default)
    {
        var blob = await PrepareBlobAsync(containerName, blobPath, ct);
        await blob.UploadAsync(
            BinaryData.FromBytes(content),
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = contentType },
            },
            ct);
    }

    public async Task UpdateAsync(
        string containerName,
        string blobPath,
        byte[] content,
        string contentType,
        CancellationToken ct = default)
    {
        var blob = await PrepareBlobAsync(containerName, blobPath, ct);
        await blob.UploadAsync(
            BinaryData.FromBytes(content),
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = contentType },
            },
            ct);
    }

    public async Task DeleteAsync(string containerName, string blobPath, CancellationToken ct = default)
    {
        await _client.GetBlobContainerClient(containerName)
            .GetBlobClient(blobPath)
            .DeleteIfExistsAsync(cancellationToken: ct);
    }

    public async Task<string?> GetPresignedReadUrlAsync(
        string containerName,
        string blobPath,
        int expiryHours,
        CancellationToken ct = default)
    {
        var blob = _client.GetBlobContainerClient(containerName).GetBlobClient(blobPath);
        if (!await blob.ExistsAsync(ct))
            return null;

        var expiresOn = DateTimeOffset.UtcNow.AddHours(expiryHours);

        if (blob.CanGenerateSasUri)
            return blob.GenerateSasUri(BlobSasPermissions.Read, expiresOn).ToString();

        var startsOn = DateTimeOffset.UtcNow.AddMinutes(-5);
        var delegationKey = await _client.GetUserDelegationKeyAsync(startsOn, expiresOn, ct);
        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = containerName,
            BlobName = blobPath,
            Resource = "b",
            StartsOn = startsOn,
            ExpiresOn = expiresOn,
        };
        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        var sas = sasBuilder.ToSasQueryParameters(delegationKey.Value, _client.AccountName);
        return new UriBuilder(blob.Uri) { Query = sas.ToString() }.Uri.ToString();
    }

    private async Task<BlobClient> PrepareBlobAsync(
        string containerName, string blobPath, CancellationToken ct)
    {
        var container = _client.GetBlobContainerClient(containerName);
        if (await container.ExistsAsync(ct))
            return container.GetBlobClient(blobPath);

        try
        {
            await container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: ct);
        }
        catch (RequestFailedException ex) when (ex.Status is 403 or 409)
        {
            if (await container.ExistsAsync(ct))
                return container.GetBlobClient(blobPath);

            _logger.LogError(
                ex,
                "Blob container {Container} is missing and could not be created (status {Status}).",
                containerName,
                ex.Status);
            throw;
        }

        return container.GetBlobClient(blobPath);
    }
}
