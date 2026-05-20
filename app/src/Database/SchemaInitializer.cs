using System.Reflection;
using Npgsql;
using ZelosHR.Api.Configs;

namespace ZelosHR.Api.Database;

public interface ISchemaInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}

public sealed class SchemaInitializer : ISchemaInitializer
{
    private readonly IDatabaseManager _database;
    private readonly ILogger<SchemaInitializer> _logger;

    public SchemaInitializer(IDatabaseManager database, ILogger<SchemaInitializer> logger)
    {
        _database = database;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _database.GetConnectionAsync(cancellationToken);
        var assembly = Assembly.GetExecutingAssembly();
        var resourceNames = assembly.GetManifestResourceNames()
            .Where(n => n.Contains(".Database.Migrations.", StringComparison.Ordinal))
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

        foreach (var resourceName in resourceNames)
        {
            await using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream is null) continue;

            using var reader = new StreamReader(stream);
            var sql = await reader.ReadToEndAsync(cancellationToken);
            await using var command = new NpgsqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync(cancellationToken);
            _logger.LogInformation("Applied migration {Migration}", resourceName);
        }
    }
}
