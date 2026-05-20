using FluentAssertions;
using NSubstitute;
using ZelosHR.Api.Entities.Branches;
using ZelosHR.Api.Entities.Departments;
using ZelosHR.Api.Entities.OrgStructure;

namespace ZelosHR.Api.Tests.OrgStructure;

public class OrgStructureServiceTests
{
    private readonly DepartmentsService _departments = new(Substitute.For<IDepartmentRepository>());
    private readonly BranchesService _branches = new(Substitute.For<IBranchRepository>());
    private readonly IDepartmentRepository _departmentRepo = Substitute.For<IDepartmentRepository>();
    private readonly IBranchRepository _branchRepo = Substitute.For<IBranchRepository>();
    private readonly OrgStructureService _sut;

    public OrgStructureServiceTests() =>
        _sut = new OrgStructureService(_departments, _branches, _departmentRepo, _branchRepo);

    [Fact]
    public async Task CreateDepartment_requires_name()
    {
        var result = await _sut.CreateDepartmentAsync(
            new CreateDepartmentRequestDto { Name = "  " }, "t1", "o1");

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        await _departmentRepo.DidNotReceive().CreateScopedAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
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
            Arg.Any<string?>(), Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }
}
