using FluentAssertions;
using NSubstitute;
using ZelosHR.Api.Entities.Branches;
using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Entities.OrgStructure;

namespace ZelosHR.Api.Tests.Branches;

public class BranchesServiceTests
{
    private readonly IBranchRepository _repo = Substitute.For<IBranchRepository>();
    private readonly ICpUserRepository _cpUsers = Substitute.For<ICpUserRepository>();
    private readonly BranchesService _sut;

    public BranchesServiceTests()
    {
        _cpUsers.GetByIdsAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, CpUserDto>());
        _sut = new BranchesService(_repo, _cpUsers);
    }

    [Fact]
    public async Task ListBranches_maps_repository_rows()
    {
        var branchId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        _repo.ListPagedScopedAsync(
                "t1",
                "o1",
                null,
                "name",
                "asc",
                false,
                1,
                15,
                Arg.Any<CancellationToken>())
            .Returns((new List<BranchListRow>
            {
                new(branchId, "Accra HQ", "Greater Accra, 4th Avenue", "Ghana", null, 5, false, now, now, null, null),
            }, 1));

        var result = await _sut.ListBranchesAsync("t1", "o1", new OrgStructureListQuery(), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.Items.Should().ContainSingle(i =>
            i.BranchId == branchId.ToString()
            && i.Name == "Accra HQ"
            && i.Address == "Greater Accra, 4th Avenue"
            && i.Country == "Ghana"
            && i.EmployeeCount == 5);
    }
}
