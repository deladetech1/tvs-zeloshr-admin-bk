using FluentAssertions;
using ZelosHR.Api.Shared.Validation;

namespace ZelosHR.Api.Tests.Shared;

public class ClientSafeErrorsTests
{
    [Fact]
    public void SanitizeInvalidOperationMessage_ReplacesEfTranslationDump()
    {
        const string efDump =
            "The LINQ expression 'DbSet<EmployeeEntity>()...could not be translated.";

        ClientSafeErrors.SanitizeInvalidOperationMessage(efDump)
            .Should().Be(ClientSafeErrors.QueryFilterFailed);
    }

    [Fact]
    public void SanitizeInvalidOperationMessage_PreservesShortBusinessMessage()
    {
        ClientSafeErrors.SanitizeInvalidOperationMessage("EmployeeCode must be set before insert")
            .Should().Be("EmployeeCode must be set before insert.");
    }

    [Fact]
    public void SanitizeInvalidOperationMessage_ReplacesEfNullableRuntimeError()
    {
        ClientSafeErrors.SanitizeInvalidOperationMessage("Nullable object must have a value.")
            .Should().Be(ClientSafeErrors.QueryFilterFailed);
    }
}
