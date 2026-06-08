using FluentAssertions;
using ZelosHR.Api.Entities.Departments;
using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Entities.Files;

namespace ZelosHR.Api.Tests.Departments;

public class DepartmentHeadMapperTests
{
    [Fact]
    public void Map_WhenHeadNamesCleared_UsesPlatformUserFullName()
    {
        var headId = Guid.NewGuid();
        var row = new DepartmentListRow(
            Guid.NewGuid(),
            "Engineering",
            null,
            null,
            null,
            false,
            headId,
            "u-head",
            string.Empty,
            null,
            null,
            "Director",
            null,
            EmployeeCount: 3,
            HeadcountCapacity: null,
            CreatedAt: DateTimeOffset.UtcNow,
            UpdatedAt: DateTimeOffset.UtcNow,
            CreatedBy: null,
            UpdatedBy: null);

        var users = new Dictionary<string, CpUserDto>
        {
            ["u-head"] = new CpUserDto("u-head", "Ada Lovelace", "ada@corp.com", "+233", true),
        };

        var profileUrl = new DocumentReadDto
        {
            DocId = "doc-profile-001",
            Name = "profile.jpg",
            PresignedUrl = "https://storage.example.com/profile.jpg",
            Description = "Employee profile photo",
        };

        var head = DepartmentHeadMapper.Map(row, users, profileUrl);

        head.Should().NotBeNull();
        head!.FullName.Should().Be("Ada Lovelace");
        head.JobTitle.Should().Be("Director");
        head.ProfileUrl.Should().BeEquivalentTo(profileUrl);
    }
}
