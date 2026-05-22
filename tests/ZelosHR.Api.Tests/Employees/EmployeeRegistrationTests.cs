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
    public async Task UpdateCompensation_AutoCalculatesAnnualizedCost_BiWeekly()
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
            id, new CreateEmployeeRequest { GrossSalary = 1000m, PayFrequency = "bi-weekly" });

        result.Data!.AnnualizedCost.Should().Be(26000m);
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
            UserId = "u-existing",
            IsDraft = true,
        };
        _employees.GetByIdScopedForUpdateAsync(id, "demo-tenant", "demo-org", Arg.Any<CancellationToken>())
            .Returns(entity);
        _cpUsers.IsLinkedToEmployeeAsync("u-existing", "demo-tenant", Arg.Any<CancellationToken>())
            .Returns(false);
        _cpUsers.EnsureHrMembershipAsync("u-existing", "demo-tenant", Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _cpUsers.UpdateIdentityAsync("u-existing", "demo-tenant", Arg.Any<CpUserIdentityData>(), Arg.Any<CancellationToken>())
            .Returns(new CpUserDto("u-existing", "Ada", "ada@test.com", null, true));
        _cpUsers.EnsureUserLocationAsync("u-existing", "demo-tenant", "demo-org", Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var result = await _sut.FinaliseAsync(id);

        result.Data!.IsDraft.Should().BeFalse();
        result.Data.LifecycleStatus.Should().Be("pre_hire");
        result.Data.UserId.Should().Be("u-existing");
        entity.IsDraft.Should().BeFalse();
    }

    [Fact]
    public async Task FinaliseEmployee_LinksCpUser_ByWorkEmail_WhenUserIdUnset()
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
            WorkEmail = "ada@corp.com",
            IsDraft = true,
        };
        _employees.GetByIdScopedForUpdateAsync(id, "demo-tenant", "demo-org", Arg.Any<CancellationToken>())
            .Returns(entity);
        _cpUsers.FindByEmailAsync("ada@corp.com", "demo-tenant", Arg.Any<CancellationToken>())
            .Returns(new CpUserDto("u-ada", "Ada", "ada@corp.com", null, true));
        _cpUsers.IsLinkedToEmployeeAsync("u-ada", "demo-tenant", Arg.Any<CancellationToken>())
            .Returns(false);
        _cpUsers.EnsureHrMembershipAsync("u-ada", "demo-tenant", Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _cpUsers.UpdateIdentityAsync("u-ada", "demo-tenant", Arg.Any<CpUserIdentityData>(), Arg.Any<CancellationToken>())
            .Returns(new CpUserDto("u-ada", "Ada", "ada@corp.com", null, true));
        _cpUsers.EnsureUserLocationAsync("u-ada", "demo-tenant", "demo-org", Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _cpUsers.GetByIdAsync("u-ada", "demo-tenant", Arg.Any<CancellationToken>())
            .Returns(new CpUserDto("u-ada", "Ada", "ada@corp.com", null, true));

        var result = await _sut.FinaliseAsync(id);

        result.Success.Should().BeTrue();
        result.Data!.UserId.Should().Be("u-ada");
        entity.UserId.Should().Be("u-ada");
        await _cpUsers.Received(1).UpdateIdentityAsync(
            "u-ada", "demo-tenant", Arg.Any<CpUserIdentityData>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FinaliseEmployee_ProvisionsCpUser_WhenEmailNotInPlatform()
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
            WorkEmail = "new@corp.com",
            Phone = "+233201111111",
            IsDraft = true,
        };
        _employees.GetByIdScopedForUpdateAsync(id, "demo-tenant", "demo-org", Arg.Any<CancellationToken>())
            .Returns(entity);
        _cpUsers.FindByEmailAsync("new@corp.com", "demo-tenant", Arg.Any<CancellationToken>())
            .Returns((CpUserDto?)null);
        _cpUsers.ProvisionEmployeeUserAsync(Arg.Any<ProvisionCpUserRequest>(), Arg.Any<CancellationToken>())
            .Returns(new CpUserDto("u-new", "Ada", "new@corp.com", "+233201111111", true));
        _cpUsers.GetByIdAsync("u-new", "demo-tenant", Arg.Any<CancellationToken>())
            .Returns(new CpUserDto("u-new", "Ada", "new@corp.com", "+233201111111", true));

        var result = await _sut.FinaliseAsync(id);

        result.Success.Should().BeTrue();
        result.Data!.UserId.Should().Be("u-new");
        entity.UserId.Should().Be("u-new");
        entity.FullName.Should().BeEmpty();
        entity.WorkEmail.Should().BeNull();
        entity.Phone.Should().BeNull();
        await _cpUsers.Received(1).ProvisionEmployeeUserAsync(
            Arg.Is<ProvisionCpUserRequest>(r => r.Email == "new@corp.com" && r.FullName == "Ada"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdatePersonalContact_WithWorkEmail_ProvisionsCpUser_AndClearsIdentityOnEmployee()
    {
        var id = Guid.NewGuid();
        var entity = new EmployeeEntity
        {
            Id = id,
            TenantId = "demo-tenant",
            OrgId = "demo-org",
            FullName = "Isaac Kumi",
            IsDraft = true,
        };
        _employees.GetByIdScopedForUpdateAsync(id, "demo-tenant", "demo-org", Arg.Any<CancellationToken>())
            .Returns(entity);
        _cpUsers.FindByEmailAsync("isaac@corp.com", "demo-tenant", Arg.Any<CancellationToken>())
            .Returns((CpUserDto?)null);
        _cpUsers.ProvisionEmployeeUserAsync(Arg.Any<ProvisionCpUserRequest>(), Arg.Any<CancellationToken>())
            .Returns(new CpUserDto("u-new", "Isaac Kumi", "isaac@corp.com", "+233201111111", true));
        _cpUsers.GetByIdAsync("u-new", "demo-tenant", Arg.Any<CancellationToken>())
            .Returns(new CpUserDto("u-new", "Isaac Kumi", "isaac@corp.com", "+233201111111", true));

        var result = await _sut.UpdatePersonalContactAsync(
            id,
            new CreateEmployeeRequest
            {
                FullName = "Isaac Kumi",
                WorkEmail = "isaac@corp.com",
                Phone = "+233201111111",
                Gender = "Female",
                DateOfBirth = new DateOnly(1990, 5, 1),
                ResidentialAddress = "Accra",
            });

        result.Success.Should().BeTrue();
        entity.UserId.Should().Be("u-new");
        entity.FullName.Should().BeEmpty();
        entity.WorkEmail.Should().BeNull();
        entity.Gender.Should().BeNull();
        entity.ResidentialAddress.Should().BeNull();
        await _cpUsers.Received(1).ProvisionEmployeeUserAsync(
            Arg.Is<ProvisionCpUserRequest>(r =>
                r.Email == "isaac@corp.com"
                && r.FullName == "Isaac Kumi"
                && r.Gender == "FEMALE"
                && r.Dob == "1990-05-01"),
            Arg.Any<CancellationToken>());
    }
}
