using FluentAssertions;
using NSubstitute;
using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Persistence.Entities;
using ZelosHR.Api.Shared.Abstractions;

namespace ZelosHR.Api.Tests.Employees;

public class EmployeeRegistrationTests
{
    private readonly IEmployeeRepository _employees = Substitute.For<IEmployeeRepository>();
    private readonly ICpUserRepository _cpUsers = Substitute.For<ICpUserRepository>();
    private readonly IFileStorageService _files = Substitute.For<IFileStorageService>();
    private readonly ITenantContext _tenant = Substitute.For<ITenantContext>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly EmployeeRegistrationService _sut;

    public EmployeeRegistrationTests()
    {
        _tenant.TenantId.Returns("demo-tenant");
        _tenant.OrgId.Returns("demo-org");
        _currentUser.UserId.Returns(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        _sut = new EmployeeRegistrationService(_employees, _cpUsers, _files, _tenant, _currentUser);
    }

    [Fact]
    public async Task CheckUser_WhenEmailExistsInCpUsers_ReturnsExistsTrue()
    {
        _cpUsers.FindByEmailAsync("a@b.com", "demo-tenant", Arg.Any<CancellationToken>())
            .Returns(new CpUserDto("u1", "Ada Lovelace", "a@b.com", "+233", true));
        _cpUsers.IsLinkedToEmployeeAsync("u1", "demo-tenant", Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await _sut.CheckCpUserAsync("a@b.com");

        result.Success.Should().BeTrue();
        result.Data!.Exists.Should().BeTrue();
        result.Data.UserId.Should().Be("u1");
        result.Data.CanImport.Should().BeTrue();
    }

    [Fact]
    public async Task CheckUser_WhenEmailNotInCpUsers_ReturnsExistsFalse()
    {
        _cpUsers.FindByEmailAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((CpUserDto?)null);

        var result = await _sut.CheckCpUserAsync("missing@b.com");

        result.Data!.Exists.Should().BeFalse();
    }

    [Fact]
    public async Task CheckUser_AlwaysScopedToTenant()
    {
        await _sut.CheckCpUserAsync("x@y.com");
        await _cpUsers.Received(1).FindByEmailAsync("x@y.com", "demo-tenant", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateDraft_WithFullNameOnly_SavesWithIsDraftTrue()
    {
        _employees.GetNextEmployeeSequenceAsync("demo-tenant", "demo-org", Arg.Any<CancellationToken>())
            .Returns(7L);
        EmployeeEntity? saved = null;
        _employees.AddAsync(Arg.Any<EmployeeEntity>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                saved = ci.Arg<EmployeeEntity>();
                return Task.FromResult(saved);
            });

        var result = await _sut.CreateDraftAsync("Kwame Asare", null);

        result.Success.Should().BeTrue();
        saved!.IsDraft.Should().BeTrue();
        saved.LifecycleStatus.Should().Be("draft");
        saved.FullName.Should().Be("Kwame Asare");
    }

    [Fact]
    public async Task CreateDraft_GeneratesEmployeeCode_InZelFormat()
    {
        _employees.GetNextEmployeeSequenceAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(289L);
        EmployeeEntity? saved = null;
        _employees.AddAsync(Arg.Any<EmployeeEntity>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                saved = ci.Arg<EmployeeEntity>();
                return Task.FromResult(saved);
            });

        var result = await _sut.CreateDraftAsync("Test User", null);

        result.Data!.EmployeeCode.Should().Be("ZEL-0289");
        saved!.EmployeeCode.Should().Be("ZEL-0289");
    }

    [Fact]
    public async Task UpdateEmploymentDetails_WithSelfAsReportingLine_ReturnsError()
    {
        var id = Guid.NewGuid();
        _employees.GetByIdScopedForUpdateAsync(id, "demo-tenant", "demo-org", Arg.Any<CancellationToken>())
            .Returns(new EmployeeEntity { Id = id, TenantId = "demo-tenant", OrgId = "demo-org", FullName = "X" });

        var result = await _sut.UpdateEmploymentDetailsAsync(
            id, new CreateEmployeeRequest { ReportsToId = id });

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateCompensation_AutoCalculatesAnnualizedCost_Monthly()
    {
        var id = Guid.NewGuid();
        var entity = new EmployeeEntity
        {
            Id = id,
            TenantId = "demo-tenant",
            OrgId = "demo-org",
            FullName = "X",
            IsDraft = true,
        };
        _employees.GetByIdScopedForUpdateAsync(id, "demo-tenant", "demo-org", Arg.Any<CancellationToken>())
            .Returns(entity);

        var result = await _sut.UpdateCompensationAsync(
            id, new CreateEmployeeRequest { GrossSalary = 1000m, PayFrequency = "Monthly" });

        result.Data!.AnnualizedCost.Should().Be(12000m);
    }

    [Fact]
    public async Task UpdateCompensation_MasksSSNIT_InReadResponse()
    {
        var id = Guid.NewGuid();
        var entity = new EmployeeEntity
        {
            Id = id,
            TenantId = "demo-tenant",
            OrgId = "demo-org",
            FullName = "X",
            SsnitNumber = "1234567890",
        };
        _employees.GetByIdScopedForUpdateAsync(id, "demo-tenant", "demo-org", Arg.Any<CancellationToken>())
            .Returns(entity);

        var result = await _sut.UpdateCompensationAsync(
            id, new CreateEmployeeRequest { SsnitNumber = "1234567890" });

        result.Data!.MaskedSsnitNumber.Should().Be("12XXXXX90");
    }

    [Fact]
    public async Task FinaliseEmployee_WithDraft_SetsIsDraftFalse_AndLifecycleToPreHire()
    {
        var id = Guid.NewGuid();
        var entity = new EmployeeEntity
        {
            Id = id,
            TenantId = "demo-tenant",
            OrgId = "demo-org",
            FullName = "Ada",
            JobTitle = "Engineer",
            DepartmentId = Guid.NewGuid(),
            IsDraft = true,
        };
        _employees.GetByIdScopedForUpdateAsync(id, "demo-tenant", "demo-org", Arg.Any<CancellationToken>())
            .Returns(entity);

        var result = await _sut.FinaliseAsync(id);

        result.Data!.IsDraft.Should().BeFalse();
        result.Data.LifecycleStatus.Should().Be("pre_hire");
        entity.IsDraft.Should().BeFalse();
    }
}
