using Microsoft.Extensions.Options;
using Npgsql;

namespace ZelosHR.Api.Configs;

/// <summary>
/// Single owner of the database connection pool. Do not create duplicate pools elsewhere.
/// </summary>
public interface IDatabaseManager
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task<NpgsqlConnection> GetConnectionAsync(CancellationToken cancellationToken = default);
    Task<DatabaseHealthResult> HealthCheckAsync(CancellationToken cancellationToken = default);
}

public sealed record DatabaseHealthResult(string Status, string? Message = null);

public class DatabaseManager : IDatabaseManager
{
    private readonly AppSettings _settings;
    private readonly ILogger<DatabaseManager> _logger;
    private NpgsqlDataSource? _dataSource;

    public DatabaseManager(IOptions<AppSettings> settings, ILogger<DatabaseManager> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var connectionString = BuildConnectionString();
        _dataSource = NpgsqlDataSource.Create(connectionString);
        _logger.LogInformation("Database connection pool initialized");
        return Task.CompletedTask;
    }

    public async Task<NpgsqlConnection> GetConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (_dataSource is null)
            throw new InvalidOperationException("Database not initialized. Call InitializeAsync first.");

        return await _dataSource.OpenConnectionAsync(cancellationToken);
    }

    public async Task<DatabaseHealthResult> HealthCheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var conn = await GetConnectionAsync(cancellationToken);
            await using var cmd = new NpgsqlCommand("SELECT 1", conn);
            await cmd.ExecuteScalarAsync(cancellationToken);
            return new DatabaseHealthResult("healthy");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");
            return new DatabaseHealthResult("unhealthy", ex.Message);
        }
    }

    private string BuildConnectionString()
    {
        if (!string.IsNullOrWhiteSpace(_settings.DatabaseUrl))
            return _settings.DatabaseUrl;

        return new NpgsqlConnectionStringBuilder
        {
            Host = _settings.DbHost ?? "localhost",
            Port = int.TryParse(_settings.DbPort, out var port) ? port : 5431,
            Database = _settings.DbName ?? "zeloshrdb",
            Username = _settings.DbUser ?? "user",
            Password = _settings.DbPassword ?? "password",
        }.ConnectionString;
    }
}
