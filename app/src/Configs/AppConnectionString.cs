using Npgsql;

namespace ZelosHR.Api.Configs;

internal static class AppConnectionString
{
    private const string ConfigureHint =
        "Set App__DatabaseUrl (postgresql://… with sslmode=require) or App__DbHost, App__DbName, App__DbUser, and App__DbPassword on the Container App. See docs/PRODUCTION_CONFIG.md.";

    internal static string Build(AppSettings settings)
    {
        var databaseUrl = NullIfWhiteSpace(settings.DatabaseUrl);
        if (databaseUrl is not null)
            return databaseUrl;

        var host = NullIfWhiteSpace(settings.DbHost);
        var database = NullIfWhiteSpace(settings.DbName);
        var username = NullIfWhiteSpace(settings.DbUser);
        var password = settings.DbPassword;

        if (host is null && database is null && username is null && string.IsNullOrEmpty(password))
        {
            throw new InvalidOperationException(
                "Database is not configured for Production. " + ConfigureHint);
        }

        return new NpgsqlConnectionStringBuilder
        {
            Host = host ?? "localhost",
            Port = int.TryParse(settings.DbPort, out var port) ? port : 5432,
            Database = database ?? "zeloshrdb",
            Username = username ?? "user",
            Password = password ?? "password",
        }.ConnectionString;
    }

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
