using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using ZelosHR.Api.Shared.Infrastructure;

namespace ZelosHR.Api.Entities.Files;

public sealed class FileManagementStorage
{
    private readonly IConfiguration _configuration;
    private readonly AzureStorageOptions _options;

    public FileManagementStorage(IConfiguration configuration, IOptions<AzureStorageOptions> options)
    {
        _configuration = configuration;
        _options = options.Value;
    }

    public string ContainerName =>
        string.IsNullOrWhiteSpace(_options.DocumentsContainer) ? "zeloshr" : _options.DocumentsContainer;

    public string StorageAccountUrl
    {
        get
        {
            var account = _configuration["Trovesuite:AzureStorage:AccountName"]
                ?? _options.AccountName;
            if (string.IsNullOrWhiteSpace(account))
                return "https://devstorage.blob.core.windows.net";
            return $"https://{account.Trim()}.blob.core.windows.net";
        }
    }
}
