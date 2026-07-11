using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ZelosHR.Api.Shared.Infrastructure;

internal static class PostgresSchemaErrors
{
    internal static bool ReferencesIdentificationsStorage(Exception ex) =>
        ReferencesMissingTable(ex, "zhr_employee_identifications")
        || ReferencesMissingColumn(ex, "zhr_employee_identifications");

    internal static bool ReferencesMissingTable(Exception ex, string tableName) =>
        TryGetPostgres(ex) is { SqlState: PostgresErrorCodes.UndefinedTable } pg
        && pg.MessageText.Contains(tableName, StringComparison.OrdinalIgnoreCase);

    internal static bool ReferencesMissingColumn(Exception ex, string tableName) =>
        TryGetPostgres(ex) is { SqlState: PostgresErrorCodes.UndefinedColumn } pg
        && pg.MessageText.Contains(tableName, StringComparison.OrdinalIgnoreCase);

    private static PostgresException? TryGetPostgres(Exception ex) =>
        ex switch
        {
            PostgresException pg => pg,
            DbUpdateException { InnerException: PostgresException inner } => inner,
            _ => ex.InnerException is not null ? TryGetPostgres(ex.InnerException) : null,
        };
}
