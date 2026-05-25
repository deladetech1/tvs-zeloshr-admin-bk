using Npgsql;

namespace ZelosHR.Api.Configs;

internal static class AppConnectionString
{
    private const string ConfigureHint =
        "Set App__ConnectionString to a PostgreSQL URI, e.g. postgresql://user:password@host:5432/dbname?sslmode=require. See docs/PRODUCTION_CONFIG.md.";

    internal static string Build(AppSettings settings)
    {
        var connectionString = NullIfWhiteSpace(settings.ConnectionString);
        if (connectionString is null)
        {
            throw new InvalidOperationException(
                "Database is not configured. " + ConfigureHint);
        }

        return connectionString;
    }

    /// <summary>
    /// Copies <see cref="AppSettings.ConnectionString"/> into Trovesuite:Database:* for Trovesuite.Package.
    /// </summary>
    internal static void SyncTrovesuiteDatabase(ConfigurationManager configuration)
    {
        var connectionString = NullIfWhiteSpace(
            configuration[$"{AppSettings.SectionName}:ConnectionString"]);
        if (connectionString is null)
            return;

        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Trovesuite:Database:Host"] = builder.Host,
            ["Trovesuite:Database:Port"] = builder.Port.ToString(),
            ["Trovesuite:Database:Database"] = builder.Database,
            ["Trovesuite:Database:Username"] = builder.Username,
            ["Trovesuite:Database:Password"] = builder.Password,
        });
    }

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
