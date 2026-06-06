using FluentAssertions;
using ZelosHR.Api.Entities.Files;

namespace ZelosHR.Api.Tests.Files;

public sealed class FileManagementBlobPathTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("undefined")]
    [InlineData("NULL")]
    public void ShouldAutoGenerateBlobPaths_when_empty_or_placeholder(string? blobPaths)
    {
        FileManagementService.ShouldAutoGenerateBlobPaths(blobPaths).Should().BeTrue();
    }

    [Fact]
    public void ShouldAutoGenerateBlobPaths_false_when_explicit_path()
    {
        FileManagementService.ShouldAutoGenerateBlobPaths("tenant/org/bus/employees/documents/a-file.pdf")
            .Should().BeFalse();
    }
}
