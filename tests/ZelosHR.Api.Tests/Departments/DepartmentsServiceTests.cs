using FluentAssertions;
using NSubstitute;
using ZelosHR.Api.Entities.Departments;
using ZelosHR.Api.Entities.Employees;

namespace ZelosHR.Api.Tests.Departments;

public class DepartmentsServiceTests
{
    private readonly IDepartmentRepository _repo = Substitute.For<IDepartmentRepository>();
    private readonly ICpUserRepository _cpUsers = Substitute.For<ICpUserRepository>();
    private readonly DepartmentsService _sut;

    public DepartmentsServiceTests()
    {
        _cpUsers.GetByIdsAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, CpUserDto>());
        _sut = new DepartmentsService(_repo, _cpUsers);
    }

    [Fact]
    public async Task GetSummary_delegates_to_repository_with_tenant_scope()
    {
        var expected = new OrganisationSummaryDto
        {
            DepartmentCount = 9,
            BranchCount = 4,
            ArchivedCount = 1,
        };
        _repo.GetSummaryScopedAsync("t1", "o1", Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _sut.GetSummaryAsync("t1", "o1");

        result.Success.Should().BeTrue();
        result.Data.Should().BeEquivalentTo(expected);
        await _repo.Received(1).GetSummaryScopedAsync("t1", "o1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListDepartments_maps_rows_and_embeds_summary()
    {
        var deptId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        _repo.ListScopedAsync(
                "t1", "o1", null, "name", "asc", false, 1, 15, Arg.Any<CancellationToken>())
            .Returns((
                new List<DepartmentListRow>
                {
                    new(
                        deptId,
                        "Engineering",
                        "Builds product",
                        null,
                        null,
                        false,
                        null,
                        null,
                        null,
                        null,
                        null,
                        null,
                        12,
                        null,
                        now,
                        now,
                        null,
                        null),
                },
                1));
        _repo.GetSummaryScopedAsync("t1", "o1", Arg.Any<CancellationToken>())
            .Returns(new OrganisationSummaryDto { DepartmentCount = 1, BranchCount = 0, ArchivedCount = 0 });

        var result = await _sut.ListDepartmentsAsync(null, "name", "asc", false, 1, 15, "t1", "o1");

        result.Success.Should().BeTrue();
        result.Data!.Items.Should().ContainSingle(i =>
            i.DepartmentId == deptId.ToString()
            && i.Name == "Engineering"
            && i.Description == "Builds product"
            && i.EmployeeCount == 12);
        result.Data.Summary.DepartmentCount.Should().Be(1);
        result.Pagination!.Total.Should().Be(1);
    }
}
