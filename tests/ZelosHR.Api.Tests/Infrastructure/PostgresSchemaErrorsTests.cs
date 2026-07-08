using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;
using ZelosHR.Api.Shared.Infrastructure;

namespace ZelosHR.Api.Tests.Infrastructure;

public sealed class PostgresSchemaErrorsTests
{
    [Fact]
    public void ReferencesMissingTable_matches_undefined_table_for_identifications()
    {
        var ex = new DbUpdateException(
            "Failed",
            new PostgresException(
                "relation \"zeloshr.zhr_employee_identifications\" does not exist",
                severity: "ERROR",
                invariantSeverity: "ERROR",
                sqlState: PostgresErrorCodes.UndefinedTable));

        PostgresSchemaErrors.ReferencesMissingTable(ex, "zhr_employee_identifications").Should().BeTrue();
    }

    [Fact]
    public void ReferencesMissingTable_ignores_other_tables()
    {
        var ex = new DbUpdateException(
            "Failed",
            new PostgresException(
                "relation \"zeloshr.zhr_employees\" does not exist",
                severity: "ERROR",
                invariantSeverity: "ERROR",
                sqlState: PostgresErrorCodes.UndefinedTable));

        PostgresSchemaErrors.ReferencesMissingTable(ex, "zhr_employee_identifications").Should().BeFalse();
    }
}
