using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ZelosHR.Api.Shared.Infrastructure;

internal static class PostgresUniqueViolation
{
    internal static bool IsEmployeeSystemCode(DbUpdateException ex) =>
        ex.InnerException is PostgresException pg
        && pg.SqlState == PostgresErrorCodes.UniqueViolation
        && pg.ConstraintName?.Contains("employee_code_system", StringComparison.OrdinalIgnoreCase) == true;

    internal static bool IsEmployeeCustomCode(DbUpdateException ex) =>
        ex.InnerException is PostgresException pg
        && pg.SqlState == PostgresErrorCodes.UniqueViolation
        && pg.ConstraintName?.Contains("employee_code_custom", StringComparison.OrdinalIgnoreCase) == true;

    internal static bool IsEmployeeCode(DbUpdateException ex) =>
        IsEmployeeSystemCode(ex) || IsEmployeeCustomCode(ex);

    internal static bool IsEmployeeUserId(DbUpdateException ex) =>
        ex.InnerException is PostgresException pg
        && pg.SqlState == PostgresErrorCodes.UniqueViolation
        && pg.ConstraintName?.Contains("user_id", StringComparison.OrdinalIgnoreCase) == true;

    internal static bool IsGhanaCard(DbUpdateException ex) =>
        ex.InnerException is PostgresException pg
        && pg.SqlState == PostgresErrorCodes.UniqueViolation
        && pg.ConstraintName?.Contains("ghana_card", StringComparison.OrdinalIgnoreCase) == true;

    internal static bool IsCpUserEmail(DbUpdateException ex) =>
        ex.InnerException is PostgresException pg
        && pg.SqlState == PostgresErrorCodes.UniqueViolation
        && pg.ConstraintName?.Contains("email", StringComparison.OrdinalIgnoreCase) == true;

    internal static bool IsCpUserContact(DbUpdateException ex) =>
        ex.InnerException is PostgresException pg
        && pg.SqlState == PostgresErrorCodes.UniqueViolation
        && pg.ConstraintName?.Contains("contact", StringComparison.OrdinalIgnoreCase) == true;
}
