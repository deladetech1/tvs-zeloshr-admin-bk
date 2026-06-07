using FluentAssertions;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using ZelosHR.Api.Entities.Branches;
using ZelosHR.Api.Entities.Departments;
using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Entities.OrgStructure;

namespace ZelosHR.Api.Tests.OrgStructure;

public class OrgStructureServiceTests
{
    private readonly DepartmentsService _departments;
    private readonly BranchesService _branches;
    private readonly IDepartmentRepository _departmentRepo = Substitute.For<IDepartmentRepository>();
    private readonly IBranchRepository _branchRepo = Substitute.For<IBranchRepository>();
    private readonly ICpUserRepository _cpUsers = Substitute.For<ICpUserRepository>();
    private readonly IHttpContextAccessor _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
    private readonly OrgStructureService _sut;

    public OrgStructureServiceTests()
    {
        _cpUsers.GetByIdsAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, CpUserDto>());
        _departments = new DepartmentsService(_departmentRepo, _cpUsers);
        _branches = new BranchesService(_branchRepo, _cpUsers);
        _sut = new OrgStructureService(
            _departments, _branches, _departmentRepo, _branchRepo, _cpUsers, _httpContextAccessor);
    }

    [Fact]
    public async Task CreateDepartment_requires_name()
    {
        var result = await _sut.CreateDepartmentAsync(
            new CreateDepartmentRequestDto { Name = "  " }, "t1", "o1");

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        await _departmentRepo.DidNotReceive().CreateScopedAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateDepartment_rejects_self_parent()
    {
        var id = Guid.NewGuid();
        _departmentRepo.ExistsActiveScopedAsync(id, "t1", "o1", Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _sut.UpdateDepartmentAsync(
            id,
            new UpdateDepartmentRequestDto { ParentDepartmentId = id },
            "t1",
            "o1");

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        await _departmentRepo.DidNotReceive().UpdateScopedAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string?>(), Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<bool>(),
            Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteDepartment_returns_404_when_missing()
    {
        var id = Guid.NewGuid();
        _departmentRepo.DeleteScopedAsync(id, "t1", "o1", Arg.Any<CancellationToken>())
            .Returns(OrgStructureDeleteResult.NotFound);

        var result = await _sut.DeleteDepartmentAsync(id, "t1", "o1");

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task DeleteDepartment_returns_409_when_employees_assigned()
    {
        var id = Guid.NewGuid();
        _departmentRepo.DeleteScopedAsync(id, "t1", "o1", Arg.Any<CancellationToken>())
            .Returns(OrgStructureDeleteResult.InUseByEmployees);

        var result = await _sut.DeleteDepartmentAsync(id, "t1", "o1");

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task DeleteBranch_returns_ok_when_deleted()
    {
        var id = Guid.NewGuid();
        _branchRepo.DeleteScopedAsync(id, "t1", "o1", Arg.Any<CancellationToken>())
            .Returns(OrgStructureDeleteResult.Deleted);

        var result = await _sut.DeleteBranchAsync(id, "t1", "o1");

        result.Success.Should().BeTrue();
        result.StatusCode.Should().Be(200);
    }
}
