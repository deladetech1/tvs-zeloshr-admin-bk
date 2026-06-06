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
    public void ResolveExplicitBlobPaths_single_file_keeps_commas_in_path()
    {
        var path =
            "tnt_x/org_y/bus_z/employees/ChatGPT Image May 23, 2026 at 11_04_44 PM.png";
        FileManagementService.ResolveExplicitBlobPaths(path, fileCount: 1)
            .Should().Equal([path]);
    }

    [Fact]
    public void ResolveExplicitBlobPaths_multi_file_splits_on_comma()
    {
        FileManagementService.ResolveExplicitBlobPaths("path/a.pdf,path/b.pdf", fileCount: 2)
            .Should().Equal(["path/a.pdf", "path/b.pdf"]);
    }

    [Fact]
    public void ResolveExplicitBlobPaths_multi_file_rejects_wrong_count()
    {
        FileManagementService.ResolveExplicitBlobPaths("only-one.pdf", fileCount: 2)
            .Should().BeNull();
    }
}
