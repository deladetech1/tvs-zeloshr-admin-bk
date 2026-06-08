using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using ZelosHR.Api.Shared.Infrastructure;

namespace ZelosHR.Api.Tests.Infrastructure;

public class PostgresUniqueViolationTests
{
    [Fact]
    public void IsCpUserContact_WhenContactConstraint_ReturnsTrue()
    {
        var ex = new DbUpdateException(
            "duplicate",
            new PostgresException(
                "duplicate key value violates unique constraint",
                severity: "ERROR",
                invariantSeverity: "ERROR",
                sqlState: PostgresErrorCodes.UniqueViolation,
                constraintName: "ix_cp_users_contact"));

        PostgresUniqueViolation.IsCpUserContact(ex).Should().BeTrue();
        PostgresUniqueViolation.IsCpUserEmail(ex).Should().BeFalse();
    }
}
