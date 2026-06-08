using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Npgsql;
using NSubstitute;
using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Entities.Files;
using ZelosHR.Api.Persistence.Entities;
using ZelosHR.Api.Persistence.Repositories;
using ZelosHR.Api.Shared.Abstractions;
using ZelosHR.Api.Shared.Infrastructure;
using ZelosHR.Api.Shared.Validation;

namespace ZelosHR.Api.Tests.Employees;

public class EmployeeRegistrationTests
{
    private readonly IEmployeeRepository _employees = Substitute.For<IEmployeeRepository>();
    private readonly ICpUserRepository _cpUsers = Substitute.For<ICpUserRepository>();
    private readonly ICpCurrencyRepository _currencies = Substitute.For<ICpCurrencyRepository>();
    private readonly IEmployeeDocumentBlobStorage _blobs = Substitute.For<IEmployeeDocumentBlobStorage>();
    private readonly IHrDocumentPathRepository _documents = Substitute.For<IHrDocumentPathRepository>();
    private readonly ITenantContext _tenant = Substitute.For<ITenantContext>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly EmployeeRegistrationService _sut;

    public EmployeeRegistrationTests()
    {
        _tenant.TenantId.Returns(TestDefaults.TenantId);
        _tenant.OrgId.Returns(TestDefaults.OrgId);
        _tenant.BusId.Returns(TestDefaults.BusId);
        _tenant.LocId.Returns(TestDefaults.LocId);
        _tenant.AppId.Returns(TestDefaults.AppId);
        _currentUser.UserId.Returns(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        var storageConfig = new FileManagementStorage(
            new ConfigurationBuilder().Build(),
            Options.Create(new AzureStorageOptions()));
        var profileUrls = new HrDocumentPresignedUrlService(
            _blobs, _documents, storageConfig, _tenant);
        _sut = new EmployeeRegistrationService(
            _employees,
            _cpUsers,
            _currencies,
            _blobs,
            _documents,
            storageConfig,
            profileUrls,
            _tenant,
            _currentUser);
    }

    [Fact]
    public async Task CheckUser_WhenEmailExistsInCpUsers_ReturnsExistsTrue()
    {
        _cpUsers.FindByEmailAsync("a@b.com", TestDefaults.TenantId, Arg.Any<CancellationToken>())
            .Returns(new CpUserDto("u1", "Ada Lovelace", "a@b.com", "+233", true));
        _cpUsers.IsLinkedToEmployeeAsync("u1", TestDefaults.TenantId, Arg.Any<CancellationToken>())
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
        await _cpUsers.Received(1).FindByEmailAsync("x@y.com", TestDefaults.TenantId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateDraft_WithFullNameOnly_SavesWithIsDraftTrue()
    {
        _employees.GetNextEmployeeSequenceAsync(TestDefaults.TenantId, TestDefaults.OrgId, Arg.Any<CancellationToken>())
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
        saved.EmploymentStatus.Should().Be(EmploymentStatusValues.Draft);
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
    public async Task CreateDraft_WhenEmployeeCodeCollides_RetriesWithNextSequence()
    {
        _employees.GetNextEmployeeSequenceAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(2L, 3L);
        var collision = new DbUpdateException(
            "duplicate",
            new PostgresException(
                "duplicate key value violates unique constraint",
                severity: "ERROR",
                invariantSeverity: "ERROR",
                sqlState: PostgresErrorCodes.UniqueViolation,
                constraintName: "ix_zhr_employees_tenant_id_employee_code"));
        _employees.AddAsync(Arg.Any<EmployeeEntity>(), Arg.Any<CancellationToken>())
            .Returns(
                _ => Task.FromException<EmployeeEntity>(collision),
                ci => Task.FromResult(ci.Arg<EmployeeEntity>()));

        var result = await _sut.CreateDraftAsync("Test User", null);

        result.Success.Should().BeTrue();
        result.Data!.EmployeeCode.Should().Be("ZEL-0003");
        await _employees.Received(2).AddAsync(Arg.Any<EmployeeEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateEmploymentDetails_WithSelfAsReportingLine_ReturnsError()
    {
        var id = Guid.NewGuid();
        _employees.GetByIdScopedForUpdateAsync(id, TestDefaults.TenantId, TestDefaults.OrgId, Arg.Any<CancellationToken>())
            .Returns(new EmployeeEntity { Id = id, TenantId = TestDefaults.TenantId, OrgId = TestDefaults.OrgId, FullName = "X" });

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
            TenantId = TestDefaults.TenantId,
            OrgId = TestDefaults.OrgId,
            FullName = "X",
            IsDraft = true,
        };
        _employees.GetByIdScopedForUpdateAsync(id, TestDefaults.TenantId, TestDefaults.OrgId, Arg.Any<CancellationToken>())
            .Returns(entity);
        _currencies.GetDefaultAsync(TestDefaults.TenantId, Arg.Any<CancellationToken>())
            .Returns(new CpCurrencyDto("cur-ghs", "Ghana Cedi", "GHS", "₵", true));

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
            TenantId = TestDefaults.TenantId,
            OrgId = TestDefaults.OrgId,
            FullName = "X",
            SsnitNumber = "1234567890",
        };
        _employees.GetByIdScopedForUpdateAsync(id, TestDefaults.TenantId, TestDefaults.OrgId, Arg.Any<CancellationToken>())
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
            TenantId = TestDefaults.TenantId,
            OrgId = TestDefaults.OrgId,
            FullName = "X",
            IsDraft = true,
        };
        _employees.GetByIdScopedForUpdateAsync(id, TestDefaults.TenantId, TestDefaults.OrgId, Arg.Any<CancellationToken>())
            .Returns(entity);
        _currencies.GetDefaultAsync(TestDefaults.TenantId, Arg.Any<CancellationToken>())
            .Returns(new CpCurrencyDto("cur-ghs", "Ghana Cedi", "GHS", "₵", true));

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
            TenantId = TestDefaults.TenantId,
            OrgId = TestDefaults.OrgId,
            FullName = "Ada",
            JobTitle = "Engineer",
            DepartmentId = Guid.NewGuid(),
            UserId = "u-existing",
            IsDraft = true,
        };
        _employees.GetByIdScopedForUpdateAsync(id, TestDefaults.TenantId, TestDefaults.OrgId, Arg.Any<CancellationToken>())
            .Returns(entity);
        _cpUsers.IsLinkedToEmployeeAsync("u-existing", TestDefaults.TenantId, Arg.Any<CancellationToken>())
            .Returns(false);
        _cpUsers.EnsureHrMembershipAsync("u-existing", TestDefaults.TenantId, Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _cpUsers.UpdateIdentityAsync("u-existing", TestDefaults.TenantId, Arg.Any<CpUserIdentityData>(), Arg.Any<CancellationToken>())
            .Returns(new CpUserDto("u-existing", "Ada", "ada@test.com", null, true));
        _cpUsers.EnsureUserLocationAsync(
            "u-existing", TestDefaults.TenantId, TestDefaults.OrgId, TestDefaults.BusId, TestDefaults.LocId, Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var result = await _sut.FinaliseAsync(id);

        result.Data!.IsDraft.Should().BeFalse();
        result.Data.LifecycleStatus.Should().Be("pre_hire");
        result.Data.UserId.Should().Be("u-existing");
        entity.IsDraft.Should().BeFalse();
    }

    [Fact]
    public async Task FinaliseEmployee_WhenEmploymentStatusActive_PreservesActive()
    {
        var id = Guid.NewGuid();
        var entity = new EmployeeEntity
        {
            Id = id,
            TenantId = TestDefaults.TenantId,
            OrgId = TestDefaults.OrgId,
            FullName = "Ada",
            WorkEmail = "ada@corp.com",
            EmploymentStatus = EmploymentStatusValues.Active,
            IsDraft = true,
        };
        _employees.GetByIdScopedForUpdateAsync(id, TestDefaults.TenantId, TestDefaults.OrgId, Arg.Any<CancellationToken>())
            .Returns(entity);
        _cpUsers.FindByEmailAsync("ada@corp.com", TestDefaults.TenantId, Arg.Any<CancellationToken>())
            .Returns(new CpUserDto("u-ada", "Ada", "ada@corp.com", null, true));
        _cpUsers.IsLinkedToEmployeeAsync("u-ada", TestDefaults.TenantId, Arg.Any<CancellationToken>())
            .Returns(false);
        _cpUsers.EnsureHrMembershipAsync("u-ada", TestDefaults.TenantId, Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _cpUsers.UpdateIdentityAsync("u-ada", TestDefaults.TenantId, Arg.Any<CpUserIdentityData>(), Arg.Any<CancellationToken>())
            .Returns(new CpUserDto("u-ada", "Ada", "ada@corp.com", null, true));
        _cpUsers.EnsureUserLocationAsync(
            "u-ada", TestDefaults.TenantId, TestDefaults.OrgId, TestDefaults.BusId, TestDefaults.LocId, Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _cpUsers.GetByIdAsync("u-ada", TestDefaults.TenantId, Arg.Any<CancellationToken>())
            .Returns(new CpUserDto("u-ada", "Ada", "ada@corp.com", null, true));

        var result = await _sut.FinaliseAsync(id);

        result.Success.Should().BeTrue();
        entity.EmploymentStatus.Should().Be(EmploymentStatusValues.Active);
        entity.LifecycleState.Should().Be(EmployeeLifecycleStates.Active);
        entity.LifecycleStatus.Should().Be("active");
    }

    [Fact]
    public async Task FinaliseEmployee_LinksCpUser_ByWorkEmail_WhenUserIdUnset()
    {
        var id = Guid.NewGuid();
        var entity = new EmployeeEntity
        {
            Id = id,
            TenantId = TestDefaults.TenantId,
            OrgId = TestDefaults.OrgId,
            FullName = "Ada",
            JobTitle = "Engineer",
            DepartmentId = Guid.NewGuid(),
            WorkEmail = "ada@corp.com",
            IsDraft = true,
        };
        _employees.GetByIdScopedForUpdateAsync(id, TestDefaults.TenantId, TestDefaults.OrgId, Arg.Any<CancellationToken>())
            .Returns(entity);
        _cpUsers.FindByEmailAsync("ada@corp.com", TestDefaults.TenantId, Arg.Any<CancellationToken>())
            .Returns(new CpUserDto("u-ada", "Ada", "ada@corp.com", null, true));
        _cpUsers.IsLinkedToEmployeeAsync("u-ada", TestDefaults.TenantId, Arg.Any<CancellationToken>())
            .Returns(false);
        _cpUsers.EnsureHrMembershipAsync("u-ada", TestDefaults.TenantId, Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _cpUsers.UpdateIdentityAsync("u-ada", TestDefaults.TenantId, Arg.Any<CpUserIdentityData>(), Arg.Any<CancellationToken>())
            .Returns(new CpUserDto("u-ada", "Ada", "ada@corp.com", null, true));
        _cpUsers.EnsureUserLocationAsync(
            "u-ada", TestDefaults.TenantId, TestDefaults.OrgId, TestDefaults.BusId, TestDefaults.LocId, Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _cpUsers.GetByIdAsync("u-ada", TestDefaults.TenantId, Arg.Any<CancellationToken>())
            .Returns(new CpUserDto("u-ada", "Ada", "ada@corp.com", null, true));

        var result = await _sut.FinaliseAsync(id);

        result.Success.Should().BeTrue();
        result.Data!.UserId.Should().Be("u-ada");
        entity.UserId.Should().Be("u-ada");
        await _cpUsers.Received(1).UpdateIdentityAsync(
            "u-ada", TestDefaults.TenantId, Arg.Any<CpUserIdentityData>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FinaliseEmployee_ProvisionsCpUser_WhenEmailNotInPlatform()
    {
        var id = Guid.NewGuid();
        var entity = new EmployeeEntity
        {
            Id = id,
            TenantId = TestDefaults.TenantId,
            OrgId = TestDefaults.OrgId,
            FullName = "Ada",
            JobTitle = "Engineer",
            DepartmentId = Guid.NewGuid(),
            WorkEmail = "new@corp.com",
            Phone = "+233201111111",
            IsDraft = true,
        };
        _employees.GetByIdScopedForUpdateAsync(id, TestDefaults.TenantId, TestDefaults.OrgId, Arg.Any<CancellationToken>())
            .Returns(entity);
        _cpUsers.FindByEmailAsync("new@corp.com", TestDefaults.TenantId, Arg.Any<CancellationToken>())
            .Returns((CpUserDto?)null);
        _cpUsers.ProvisionEmployeeUserAsync(Arg.Any<ProvisionCpUserRequest>(), Arg.Any<CancellationToken>())
            .Returns(new CpUserDto("u-new", "Ada", "new@corp.com", "+233201111111", true));
        _cpUsers.GetByIdAsync("u-new", TestDefaults.TenantId, Arg.Any<CancellationToken>())
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
    public async Task FinaliseEmployee_WithoutDepartment_Succeeds()
    {
        var id = Guid.NewGuid();
        var entity = new EmployeeEntity
        {
            Id = id,
            TenantId = TestDefaults.TenantId,
            OrgId = TestDefaults.OrgId,
            FullName = "Ada",
            JobTitle = "Engineer",
            UserId = "u-existing",
            IsDraft = true,
        };
        _employees.GetByIdScopedForUpdateAsync(id, TestDefaults.TenantId, TestDefaults.OrgId, Arg.Any<CancellationToken>())
            .Returns(entity);
        _cpUsers.IsLinkedToEmployeeAsync("u-existing", TestDefaults.TenantId, Arg.Any<CancellationToken>())
            .Returns(false);
        _cpUsers.EnsureHrMembershipAsync("u-existing", TestDefaults.TenantId, Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _cpUsers.UpdateIdentityAsync("u-existing", TestDefaults.TenantId, Arg.Any<CpUserIdentityData>(), Arg.Any<CancellationToken>())
            .Returns(new CpUserDto("u-existing", "Ada", "ada@test.com", null, true));
        _cpUsers.EnsureUserLocationAsync(
            "u-existing", TestDefaults.TenantId, TestDefaults.OrgId, TestDefaults.BusId, TestDefaults.LocId, Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var result = await _sut.FinaliseAsync(id);

        result.Success.Should().BeTrue();
        entity.DepartmentId.Should().BeNull();
    }

    [Fact]
    public async Task FinaliseEmployee_OnSiteWithoutBranch_Succeeds()
    {
        var id = Guid.NewGuid();
        var entity = new EmployeeEntity
        {
            Id = id,
            TenantId = TestDefaults.TenantId,
            OrgId = TestDefaults.OrgId,
            FullName = "Ada",
            JobTitle = "Engineer",
            WorkArrangement = "on_site",
            WorkEmail = "ada@corp.com",
            UserId = "u-existing",
            IsDraft = true,
        };
        _employees.GetByIdScopedForUpdateAsync(id, TestDefaults.TenantId, TestDefaults.OrgId, Arg.Any<CancellationToken>())
            .Returns(entity);
        _cpUsers.IsLinkedToEmployeeAsync("u-existing", TestDefaults.TenantId, Arg.Any<CancellationToken>())
            .Returns(false);
        _cpUsers.EnsureHrMembershipAsync("u-existing", TestDefaults.TenantId, Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _cpUsers.UpdateIdentityAsync("u-existing", TestDefaults.TenantId, Arg.Any<CpUserIdentityData>(), Arg.Any<CancellationToken>())
            .Returns(new CpUserDto("u-existing", "Ada", "ada@corp.com", null, true));
        _cpUsers.EnsureUserLocationAsync(
            "u-existing", TestDefaults.TenantId, TestDefaults.OrgId, TestDefaults.BusId, TestDefaults.LocId, Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var result = await _sut.FinaliseAsync(id);

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task FinaliseEmployee_RemoteWithBranch_ReturnsValidationError()
    {
        var id = Guid.NewGuid();
        var entity = new EmployeeEntity
        {
            Id = id,
            TenantId = TestDefaults.TenantId,
            OrgId = TestDefaults.OrgId,
            FullName = "Ada",
            WorkArrangement = "remote",
            BranchId = Guid.NewGuid(),
            WorkEmail = "ada@corp.com",
            UserId = "u-existing",
            IsDraft = true,
        };
        _employees.GetByIdScopedForUpdateAsync(id, TestDefaults.TenantId, TestDefaults.OrgId, Arg.Any<CancellationToken>())
            .Returns(entity);

        var result = await _sut.FinaliseAsync(id);

        result.Success.Should().BeFalse();
        result.FieldErrors.Should().ContainKey("employment.branch_id");
    }

    [Fact]
    public async Task UpdatePersonalContact_WithWorkEmail_ProvisionsCpUser_AndClearsIdentityOnEmployee()
    {
        var id = Guid.NewGuid();
        var entity = new EmployeeEntity
        {
            Id = id,
            TenantId = TestDefaults.TenantId,
            OrgId = TestDefaults.OrgId,
            FullName = "Isaac Kumi",
            IsDraft = true,
        };
        _employees.GetByIdScopedForUpdateAsync(id, TestDefaults.TenantId, TestDefaults.OrgId, Arg.Any<CancellationToken>())
            .Returns(entity);
        _cpUsers.FindByEmailAsync("isaac@corp.com", TestDefaults.TenantId, Arg.Any<CancellationToken>())
            .Returns((CpUserDto?)null);
        _cpUsers.ProvisionEmployeeUserAsync(Arg.Any<ProvisionCpUserRequest>(), Arg.Any<CancellationToken>())
            .Returns(new CpUserDto("u-new", "Isaac Kumi", "isaac@corp.com", "+233201111111", true));
        _cpUsers.GetByIdAsync("u-new", TestDefaults.TenantId, Arg.Any<CancellationToken>())
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

    [Fact]
    public async Task UpdatePersonalContact_WhenPhoneAlreadyRegistered_ReturnsIdentityPhoneFieldError()
    {
        var id = Guid.NewGuid();
        var entity = new EmployeeEntity
        {
            Id = id,
            TenantId = TestDefaults.TenantId,
            OrgId = TestDefaults.OrgId,
            FullName = "Ada Lovelace",
            IsDraft = true,
        };
        _employees.GetByIdScopedForUpdateAsync(id, TestDefaults.TenantId, TestDefaults.OrgId, Arg.Any<CancellationToken>())
            .Returns(entity);
        _cpUsers.FindByEmailAsync("ada@corp.com", TestDefaults.TenantId, Arg.Any<CancellationToken>())
            .Returns((CpUserDto?)null);
        _cpUsers.ProvisionEmployeeUserAsync(Arg.Any<ProvisionCpUserRequest>(), Arg.Any<CancellationToken>())
            .Returns<Task<CpUserDto>>(_ => throw new PlatformUserConflictException(
                "identity.phone", EmployeeErrorMessages.PhoneAlreadyRegistered));

        var result = await _sut.UpdatePersonalContactAsync(
            id,
            new CreateEmployeeRequest
            {
                FullName = "Ada Lovelace",
                WorkEmail = "ada@corp.com",
                Phone = "+233201234567",
            });

        result.Success.Should().BeFalse();
        result.FieldErrors.Should().ContainKey("identity.phone")
            .WhoseValue.Should().Be(EmployeeErrorMessages.PhoneAlreadyRegistered);
    }
}
