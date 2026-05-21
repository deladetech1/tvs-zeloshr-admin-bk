using Azure.Storage.Blobs;
using ZelosHR.Api.Shared.Abstractions;
using ZelosHR.Api.Shared.Infrastructure;

namespace ZelosHR.Api.Configs;

public static class StorageRegistration
{
    public static IServiceCollection AddZelosHrStorage(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AzureStorageOptions>(configuration.GetSection(AzureStorageOptions.SectionName));

        var connectionString = configuration[$"{AzureStorageOptions.SectionName}:ConnectionString"];
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddSingleton(_ => new BlobServiceClient(connectionString));
            services.AddScoped<IFileStorageService, AzureBlobStorageService>();
        }
        else
        {
            services.AddScoped<IFileStorageService, LocalDevFileStorageService>();
        }

        return services;
    }
}
