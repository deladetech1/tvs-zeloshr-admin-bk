using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using NSubstitute;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Branches;
using ZelosHR.Api.Entities.Departments;
using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Entities.Files;
using ZelosHR.Api.Entities.OrgStructure;
using ZelosHR.Api.Persistence.Repositories;
using ZelosHR.Api.Shared.Abstractions;
using ZelosHR.Api.Shared.Infrastructure;

namespace ZelosHR.Api.Tests.OrgStructure;

public class OrgStructureServiceTests
{
    private readonly DepartmentsService _departments;
    private readonly BranchesService _branches;
    private readonly IDepartmentRepository _departmentRepo = Substitute.For<IDepartmentRepository>();
    private readonly IOrgChartRepository _orgChartRepo = Substitute.For<IOrgChartRepository>();
    private readonly IBranchRepository _branchRepo = Substitute.For<IBranchRepository>();
    private readonly ICpUserRepository _cpUsers = Substitute.For<ICpUserRepository>();
    private readonly IHttpContextAccessor _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
    private readonly OrgStructureService _sut;

    public OrgStructureServiceTests()
    {
        _cpUsers.GetByIdsAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, CpUserDto>());
        var tenant = Substitute.For<ITenantContext>();
        tenant.TenantId.Returns("t1");
        var profileUrls = new HrDocumentPresignedUrlService(
            Substitute.For<IEmployeeDocumentBlobStorage>(),
            Substitute.For<IHrDocumentPathRepository>(),
            new FileManagementStorage(new ConfigurationBuilder().Build(), Options.Create(new AzureStorageOptions())),
            tenant);
        _departments = new DepartmentsService(_departmentRepo, _cpUsers, profileUrls);
        _branches = new BranchesService(_branchRepo, _cpUsers);
        _sut = new OrgStructureService(
            _departments,
            _branches,
            _departmentRepo,
            _orgChartRepo,
            _branchRepo,
            _cpUsers,
            profileUrls,
            _httpContextAccessor);
    }

    [Fact]
    public async Task GetOrgChart_builds_reporting_tree_from_repository()
    {
        var ceoId = Guid.NewGuid();
        var headId = Guid.NewGuid();
        var deptId = Guid.NewGuid();

        _orgChartRepo.GetReportingHierarchyScopedAsync("t1", "o1", Arg.Any<CancellationToken>())
            .Returns((
                new List<OrgChartEmployeeRow>
                {
                    new(ceoId, "Kwame Asante", "Kwame", null, "Asante", "Chief Executive Officer", null, null, null),
                    new(headId, "Kwame Boateng", "Kwame", null, "Boateng", "Chief Technology Officer", ceoId, null, null),
                },
                new List<OrgChartDepartmentHeadRow>
                {
                    new(deptId, "Engineering", headId, 8, 10),
                }));

        var result = await _sut.GetOrgChartAsync("t1", "o1");

        result.Success.Should().BeTrue();
        result.Data!.Roots.Should().ContainSingle();
        result.Data.Roots[0].FullName.Should().Be("Kwame Asante");
        result.Data.Roots[0].Children.Should().ContainSingle();
        result.Data.Roots[0].Children[0].Department!.Name.Should().Be("Engineering");
    }

    [Fact]
    public async Task CreateDepartment_passes_resolved_head_id_to_repository()
    {
        var headId = Guid.NewGuid();
        _departmentRepo.CreateScopedAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<Guid?>(), headId, Arg.Any<string?>(), Arg.Any<int?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Guid.NewGuid());
        _departmentRepo.GetActiveScopedAsync(Arg.Any<Guid>(), "t1", "o1", Arg.Any<CancellationToken>())
            .Returns(new DepartmentListRow(
                Guid.NewGuid(), "Eng", null, null, null, false, headId, null, null, null, null, null, null, 0, null,
                DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null, null));

        var result = await _sut.CreateDepartmentAsync(
            new CreateDepartmentRequestDto
            {
                Name = "Eng",
                HeadOfDepartment = new DepartmentHeadReferenceDto { EmployeeId = headId.ToString() },
                Description = "Platform",
            },
            "t1",
            "o1");

        result.Success.Should().BeTrue();
        await _departmentRepo.Received(1).CreateScopedAsync(
            "t1", "o1", "Eng", null, headId, "Platform", null, Arg.Any<string?>(), Arg.Any<CancellationToken>());
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
            Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<int?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
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
            Arg.Any<string?>(), Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<bool>(), Arg.Any<string?>(), Arg.Any<bool>(),
            Arg.Any<int?>(), Arg.Any<bool>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateDepartment_clears_head_when_head_of_department_id_is_null()
    {
        var id = Guid.NewGuid();
        _departmentRepo.ExistsActiveScopedAsync(id, "t1", "o1", Arg.Any<CancellationToken>())
            .Returns(true);
        _departmentRepo.UpdateScopedAsync(
                id, "t1", "o1", "Backend Team",
                Arg.Is<Guid?>(g => g == null),
                Arg.Is<Guid?>(g => g == null),
                true,
                Arg.Is<string?>(s => s == null),
                false,
                Arg.Is<int?>(i => i == null),
                false,
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns("Backend Team");
        _departmentRepo.GetActiveScopedAsync(id, "t1", "o1", Arg.Any<CancellationToken>())
            .Returns(new DepartmentListRow(
                id, "Backend Team", null, null, null, false, null, null, null, null, null, null, null, 0, null,
                DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null, null));

        const string json = """
            {
              "name": "Backend Team",
              "head_of_department_id": null
            }
            """;
        var body = JsonSerializer.Deserialize<UpdateDepartmentRequestDto>(json, PlatformJson.SerializerOptions)!;

        var result = await _sut.UpdateDepartmentAsync(id, body, "t1", "o1");

        result.Success.Should().BeTrue();
        result.Data!.HeadOfDepartment.Should().BeNull();
        await _departmentRepo.Received(1).UpdateScopedAsync(
            id, "t1", "o1", "Backend Team",
            Arg.Is<Guid?>(g => g == null),
            Arg.Is<Guid?>(g => g == null),
            true,
            Arg.Is<string?>(s => s == null),
            false,
            Arg.Is<int?>(i => i == null),
            false,
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
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
