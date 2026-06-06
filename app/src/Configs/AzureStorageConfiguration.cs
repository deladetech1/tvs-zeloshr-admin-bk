using Azure.Identity;

namespace ZelosHR.Api.Configs;

/// <summary>
/// Aligns Trovesuite.Package storage settings with ZelosHR <c>AzureStorage</c> env vars.
/// </summary>
internal static class AzureStorageConfiguration
{
    private const string TrovesuiteConnectionStringKey = "Trovesuite:AzureStorage:ConnectionString";
    private const string ZelosConnectionStringKey = $"{AzureStorageOptionsSection}:ConnectionString";
    private const string AzureStorageOptionsSection = "AzureStorage";

    internal static void Apply(ConfigurationManager configuration)
    {
        var overrides = new Dictionary<string, string?>(StringComparer.Ordinal);

        var trovesuiteConnection = NullIfWhiteSpace(configuration[TrovesuiteConnectionStringKey]);
        var zelosConnection = NullIfWhiteSpace(configuration[ZelosConnectionStringKey]);
        if (trovesuiteConnection is null && zelosConnection is not null)
            overrides[TrovesuiteConnectionStringKey] = zelosConnection;

        if (overrides.Count == 0)
            return;

        configuration.AddInMemoryCollection(overrides);
    }

    internal static DefaultAzureCredential CreateCredential(IConfiguration configuration)
    {
        var managedIdentityClientId = NullIfWhiteSpace(
            configuration["Trovesuite:AzureStorage:UserAssignedManagedIdentityClientId"]);

        if (managedIdentityClientId is null)
            return new DefaultAzureCredential();

        return new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            ManagedIdentityClientId = managedIdentityClientId,
        });
    }

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
