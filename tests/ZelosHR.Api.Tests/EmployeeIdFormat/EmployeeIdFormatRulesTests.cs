using FluentAssertions;
using ZelosHR.Api.Entities.EmployeeIdFormat;
using ZelosHR.Api.Persistence.Entities;

namespace ZelosHR.Api.Tests.EmployeeIdFormat;

public class EmployeeIdFormatRulesTests
{
    private static EmployeeIdFormatEntity DefaultFormat() => new()
    {
        Prefix = "ZEL",
        DigitCount = 4,
        StartingNumber = 1,
        Separator = EmployeeIdFormatSeparator.Hyphen,
        AutoGenerate = true,
    };

    [Fact]
    public void Format_builds_prefix_separator_and_zero_padded_number()
    {
        EmployeeIdFormatRules.Format(DefaultFormat(), 103).Should().Be("ZEL-0103");
    }

    [Fact]
    public void ComputeNextSequence_uses_starting_number_when_no_existing_codes()
    {
        var next = EmployeeIdFormatRules.ComputeNextSequence(Array.Empty<string>(), DefaultFormat());
        next.Should().Be(1);
    }

    [Fact]
    public void ComputeNextSequence_uses_max_existing_plus_one()
    {
        var next = EmployeeIdFormatRules.ComputeNextSequence(
            ["ZEL-0099", "ZEL-0102", "OTHER-9999"],
            DefaultFormat());
        next.Should().Be(103);
    }

    [Fact]
    public void ComputeNextSequence_respects_starting_number_floor()
    {
        var format = DefaultFormat();
        format.StartingNumber = 100;
        var next = EmployeeIdFormatRules.ComputeNextSequence(["ZEL-0050"], format);
        next.Should().Be(100);
    }

    [Theory]
    [InlineData("none", "ZEL0103")]
    [InlineData("underscore", "ZEL_0103")]
    [InlineData("slash", "ZEL/0103")]
    public void Format_supports_configured_separators(string separator, string expected)
    {
        var format = DefaultFormat();
        format.Separator = separator;
        EmployeeIdFormatRules.Format(format, 103).Should().Be(expected);
    }
}
