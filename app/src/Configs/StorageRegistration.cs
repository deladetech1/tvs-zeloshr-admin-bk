using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ZelosHR.Api.Shared.Abstractions;
using ZelosHR.Api.Shared.Infrastructure;

namespace ZelosHR.Api.Configs;

public static class StorageRegistration
{
    public static IServiceCollection AddZelosHrStorage(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.Configure<AzureStorageOptions>(configuration.GetSection(AzureStorageOptions.SectionName));

        var connectionString = configuration[$"{AzureStorageOptions.SectionName}:ConnectionString"];
        var accountName = ResolveAccountName(configuration);
        var blobServiceUri = configuration[$"{AzureStorageOptions.SectionName}:BlobServiceUri"];

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddSingleton(_ => new BlobServiceClient(connectionString.Trim()));
            services.AddScoped<IFileStorageService, AzureBlobStorageService>();
            return services;
        }

        if (!string.IsNullOrWhiteSpace(blobServiceUri)
            || !string.IsNullOrWhiteSpace(accountName))
        {
            var serviceUri = !string.IsNullOrWhiteSpace(blobServiceUri)
                ? new Uri(blobServiceUri.Trim())
                : new Uri($"https://{accountName!.Trim()}.blob.core.windows.net");

            services.AddSingleton(sp =>
            {
                var logger = sp.GetRequiredService<ILoggerFactory>()
                    .CreateLogger(typeof(StorageRegistration));
                logger.LogInformation(
                    "Azure Blob Storage: using DefaultAzureCredential against {BlobServiceUri}",
                    serviceUri);
                return new BlobServiceClient(serviceUri, new DefaultAzureCredential());
            });
            services.AddScoped<IFileStorageService, AzureBlobStorageService>();
            return services;
        }

        if (environment.IsDevelopment())
        {
            services.AddScoped<IFileStorageService, LocalDevFileStorageService>();
            return services;
        }

        throw new InvalidOperationException(
            "Azure Blob Storage is not configured. Set AzureStorage:ConnectionString (local) "
            + "or AzureStorage:AccountName / Trovesuite:AzureStorage:AccountName "
            + "(Container App managed identity + DefaultAzureCredential).");
    }

    private static string? ResolveAccountName(IConfiguration configuration)
    {
        var direct = configuration[$"{AzureStorageOptions.SectionName}:AccountName"];
        if (!string.IsNullOrWhiteSpace(direct))
            return direct.Trim();

        var trove = configuration["Trovesuite:AzureStorage:AccountName"];
        return string.IsNullOrWhiteSpace(trove) ? null : trove.Trim();
    }
}
