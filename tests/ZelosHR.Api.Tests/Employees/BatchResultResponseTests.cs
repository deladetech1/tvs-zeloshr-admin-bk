using FluentAssertions;
using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Shared.Validation;

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
    public void FromRowCounts_WhenAllFailed_UsesRowErrorAsDetail()
    {
        var result = BatchResultResponse.FromRowCounts(
            "payload",
            successCount: 0,
            failureCount: 1,
            "Import",
            [EmployeeErrorMessages.UserAlreadyLinkedToEmployee]);

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(422);
        result.Data.Should().Be("payload");
        result.Detail.Should().Be(EmployeeErrorMessages.UserAlreadyLinkedToEmployee);
    }

    [Fact]
    public void FromRowCounts_WhenAllFailedWithoutMessages_FallsBackToGenericDetail()
    {
        var result = BatchResultResponse.FromRowCounts("payload", successCount: 0, failureCount: 3, "Bulk import");

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(422);
        result.Detail.Should().Be("Bulk import completed with no successful rows.");
    }

    [Fact]
    public void FromRowCounts_WhenPartialSuccess_UsesSingleRowErrorAsDetail()
    {
        var result = BatchResultResponse.FromRowCounts(
            "payload",
            successCount: 2,
            failureCount: 1,
            "Import",
            ["Row 2: work_email is already registered for another employee."]);

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(207);
        result.Data.Should().Be("payload");
        result.Detail.Should().Be("Row 2: work_email is already registered for another employee.");
    }
}
