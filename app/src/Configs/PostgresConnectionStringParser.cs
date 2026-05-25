using Npgsql;

namespace ZelosHR.Api.Configs;

/// <summary>
/// Parses <c>postgresql://</c> URIs and Npgsql <c>key=value;</c> connection strings.
/// </summary>
internal static class PostgresConnectionStringParser
{
    internal static NpgsqlConnectionStringBuilder Parse(string connectionString)
    {
        var trimmed = connectionString.Trim().Trim('"', '\'');
        if (string.IsNullOrWhiteSpace(trimmed))
            throw new ArgumentException("Connection string is empty.", nameof(connectionString));

        if (IsPostgresUri(trimmed))
            return ParseUri(trimmed);

        return new NpgsqlConnectionStringBuilder(trimmed);
    }

    internal static string Normalize(string connectionString) =>
        Parse(connectionString).ConnectionString;

    private static bool IsPostgresUri(string value) =>
        value.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
        || value.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase);

    private static NpgsqlConnectionStringBuilder ParseUri(string uriString)
    {
        var uri = new Uri(uriString);
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Database = uri.AbsolutePath.TrimStart('/'),
        };

        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            var colon = uri.UserInfo.IndexOf(':');
            if (colon >= 0)
            {
                builder.Username = Uri.UnescapeDataString(uri.UserInfo[..colon]);
                builder.Password = Uri.UnescapeDataString(uri.UserInfo[(colon + 1)..]);
            }
            else
            {
                builder.Username = Uri.UnescapeDataString(uri.UserInfo);
            }
        }

        ApplyQueryParameters(uri.Query, builder);
        return builder;
    }

    private static void ApplyQueryParameters(string query, NpgsqlConnectionStringBuilder builder)
    {
        if (string.IsNullOrEmpty(query))
            return;

        foreach (var segment in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var equals = segment.IndexOf('=');
            var key = equals >= 0
                ? Uri.UnescapeDataString(segment[..equals])
                : Uri.UnescapeDataString(segment);
            var value = equals >= 0
                ? Uri.UnescapeDataString(segment[(equals + 1)..])
                : string.Empty;

            switch (key.ToLowerInvariant())
            {
                case "sslmode" when Enum.TryParse<SslMode>(value, ignoreCase: true, out var sslMode):
                    builder.SslMode = sslMode;
                    break;
                case "ssl":
                    if (string.Equals(value, "true", StringComparison.OrdinalIgnoreCase))
                        builder.SslMode = SslMode.Require;
                    break;
            }
        }
    }
}
