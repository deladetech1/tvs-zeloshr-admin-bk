using FluentAssertions;
using ZelosHR.Api.Entities.Employees;

namespace ZelosHR.Api.Tests.Employees;

public class BatchResultResponseTests
{
    [Fact]
    public void FromRowCounts_WhenAllSucceeded_Returns200AndSuccessTrue()
    {
        var result = BatchResultResponse.FromRowCounts("payload", successCount: 3, failureCount: 0, "Bulk import");

        result.Success.Should().BeTrue();
        result.StatusCode.Should().Be(200);
        result.Data.Should().Be("payload");
        result.Detail.Should().BeNull();
    }

    [Fact]
    public void FromRowCounts_WhenAllFailed_Returns422AndSuccessFalse()
    {
        var result = BatchResultResponse.FromRowCounts("payload", successCount: 0, failureCount: 3, "Bulk import");

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(422);
        result.Data.Should().Be("payload");
        result.Detail.Should().Be("Bulk import completed with no successful rows.");
    }

    [Fact]
    public void FromRowCounts_WhenPartialSuccess_Returns207AndSuccessFalse()
    {
        var result = BatchResultResponse.FromRowCounts("payload", successCount: 2, failureCount: 1, "Import");

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(207);
        result.Data.Should().Be("payload");
        result.Detail.Should().Be("Import completed with 2 successful and 1 failed row(s).");
    }
}
