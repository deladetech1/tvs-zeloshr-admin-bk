using Npgsql;

namespace ZelosHR.Functions.Shared;

public interface IFunctionDatabase
{
    Task<NpgsqlConnection> GetConnectionAsync(CancellationToken ct = default);
}

public class FunctionDatabase : IFunctionDatabase
{
    private readonly string _connectionString;

    public FunctionDatabase()
    {
        _connectionString = Environment.GetEnvironmentVariable("DATABASE_URL")
            ?? "Host=localhost;Port=5431;Database=zeloshrdb;Username=user;Password=password";
    }

    public async Task<NpgsqlConnection> GetConnectionAsync(CancellationToken ct = default)
    {
        var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        return conn;
    }
}
