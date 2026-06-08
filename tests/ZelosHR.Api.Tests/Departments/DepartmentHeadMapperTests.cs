using FluentAssertions;
using ZelosHR.Api.Entities.Departments;
using ZelosHR.Api.Entities.Employees;

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

        var head = DepartmentHeadMapper.Map(row, users);

        head.Should().NotBeNull();
        head!.FullName.Should().Be("Ada Lovelace");
        head.Initials.Should().Be("AL");
        head.JobTitle.Should().Be("Director");
    }
}
