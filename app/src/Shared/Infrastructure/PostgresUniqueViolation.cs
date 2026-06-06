using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ZelosHR.Api.Shared.Infrastructure;

internal static class PostgresUniqueViolation
{
    internal static bool IsEmployeeCode(DbUpdateException ex) =>
        ex.InnerException is PostgresException pg
        && pg.SqlState == PostgresErrorCodes.UniqueViolation
        && pg.ConstraintName?.Contains("employee_code", StringComparison.OrdinalIgnoreCase) == true;

    internal static bool IsGhanaCard(DbUpdateException ex) =>
        ex.InnerException is PostgresException pg
        && pg.SqlState == PostgresErrorCodes.UniqueViolation
        && pg.ConstraintName?.Contains("ghana_card", StringComparison.OrdinalIgnoreCase) == true;

    internal static bool IsCpUserEmail(DbUpdateException ex) =>
        ex.InnerException is PostgresException pg
        && pg.SqlState == PostgresErrorCodes.UniqueViolation
        && pg.ConstraintName?.Contains("email", StringComparison.OrdinalIgnoreCase) == true;
}
