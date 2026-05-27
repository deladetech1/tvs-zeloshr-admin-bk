using FluentAssertions;
using NSubstitute;
using ZelosHR.Api.Entities.Branches;

namespace ZelosHR.Api.Tests.Branches;

public class BranchesServiceTests
{
    private readonly IBranchRepository _repo = Substitute.For<IBranchRepository>();
    private readonly BranchesService _sut;

    public BranchesServiceTests() => _sut = new BranchesService(_repo);

    [Fact]
    public async Task ListBranches_maps_repository_rows()
    {
        var branchId = Guid.NewGuid();
        _repo.ListPagedScopedAsync("t1", "o1", null, false, 1, 20, Arg.Any<CancellationToken>())
            .Returns((new List<BranchListRow>
            {
                new(branchId, "Accra", 5, false),
            }, 1));

        var result = await _sut.ListBranchesAsync("t1", "o1", null, false, 1, 20);

        result.Success.Should().BeTrue();
        result.Data!.Items.Should().ContainSingle(i =>
            i.BranchId == branchId.ToString() && i.Name == "Accra" && i.EmployeeCount == 5);
    }
}
