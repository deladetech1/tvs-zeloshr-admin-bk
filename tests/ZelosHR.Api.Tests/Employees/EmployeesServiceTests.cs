using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ZelosHR.Api.Entities.Branches;
using ZelosHR.Api.Entities.Departments;
using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Persistence.Entities;
using ZelosHR.Api.Shared.Abstractions;

namespace ZelosHR.Api.Tests.Employees;

public class EmployeesServiceTests
{
    private readonly IEmployeeRepository _repo = Substitute.For<IEmployeeRepository>();
    private readonly ICpUserRepository _cpUsers = Substitute.For<ICpUserRepository>();
    private readonly IDepartmentRepository _departments = Substitute.For<IDepartmentRepository>();
    private readonly IBranchRepository _branches = Substitute.For<IBranchRepository>();
    private readonly ITenantContext _tenant = Substitute.For<ITenantContext>();
    private readonly EmployeesService _sut;

    private static readonly Guid EmployeeId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public EmployeesServiceTests()
    {
        _tenant.TenantId.Returns("demo-tenant");
        _tenant.OrgId.Returns("demo-org");

        _cpUsers.GetByIdsAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, CpUserDto>());

        _sut = new EmployeesService(
            Substitute.For<ILogger<EmployeesService>>(),
            _repo,
            _cpUsers,
            _departments,
            _branches,
            _tenant);
    }

    private static EmployeeEntity SampleEmployee() => new()
    {
        Id = EmployeeId,
        EmployeeCode = "ZEL-0001",
        TenantId = "demo-tenant",
        OrgId = "demo-org",
        FullName = "Ama Mensah",
        FirstName = "Ama",
        LastName = "Mensah",
        DateOfBirth = new DateOnly(1990, 1, 1),
        Gender = "Female",
        Nationality = "Ghanaian",
        GhanaCardNumber = "GHA-123456789-0",
        PersonalEmail = "ama@example.com",
        PersonalPhone = "+233200000001",
        ResidentialAddress = "Accra",
        GhanaPostGps = "GA-123-4567",
        LifecycleState = EmployeeLifecycleStates.Active,
        EmploymentStatus = "Active",
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow,
    };

    [Fact]
    public async Task GetById_WhenEmployeeExists_ReturnsSuccessRespons()
    {
        var entity = SampleEmployee();
        _repo.GetByIdScopedAsync(EmployeeId, "demo-tenant", "demo-org", Arg.Any<CancellationToken>())
            .Returns(entity);

        var result = await _sut.GetByIdAsync(EmployeeId);

        result.Success.Should().BeTrue();
        result.Data!.Id.Should().Be(EmployeeId);
        result.Data.FullName.Should().Contain("Ama");
    }

    [Fact]
    public async Task GetById_WhenEmployeeNotFound_ReturnsNotFoundRespons()
    {
        _repo.GetByIdScopedAsync(EmployeeId, "demo-tenant", "demo-org", Arg.Any<CancellationToken>())
            .Returns((EmployeeEntity?)null);

        var result = await _sut.GetByIdAsync(EmployeeId);

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetAll_ReturnsPaginatedRespons()
    {
        var entity = SampleEmployee();
        _repo.GetPagedScopedAsync("demo-tenant", "demo-org", 1, 20, Arg.Any<CancellationToken>())
            .Returns((new[] { entity }, 1));

        var result = await _sut.GetAllAsync(1, 20);

        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(1);
        result.Pagination!.Total.Should().Be(1);
    }

    [Fact]
    public async Task Create_WithValidData_CallsRepositoryAddAndReturnsCreated()
    {
        _repo.ExistsByGhanaCardAsync(Arg.Any<string>(), "demo-tenant", null, Arg.Any<CancellationToken>())
            .Returns(false);
        _repo.GetNextEmployeeSequenceAsync("demo-tenant", "demo-org", Arg.Any<CancellationToken>())
            .Returns(1L);
        _repo.AddAsync(Arg.Any<EmployeeEntity>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<EmployeeEntity>());

        var dto = new EmployeeWriteDto
        {
            FirstName = "Kofi",
            LastName = "Asante",
            GhanaCardNumber = "GHA-999888777-1",
            Email = "kofi@example.com",
            Phone = "+233200000002",
        };

        var result = await _sut.CreateAsync(dto);

        result.Success.Should().BeTrue();
        result.StatusCode.Should().Be(201);
        await _repo.Received(1).AddAsync(
            Arg.Is<EmployeeEntity>(e => e.TenantId == "demo-tenant" && e.OrgId == "demo-org"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_WithDuplicateGhanaCard_ReturnsConflictRespons()
    {
        _repo.ExistsByGhanaCardAsync(Arg.Any<string>(), "demo-tenant", null, Arg.Any<CancellationToken>())
            .Returns(true);

        var dto = new EmployeeWriteDto
        {
            FirstName = "Kofi",
            LastName = "Asante",
            GhanaCardNumber = "GHA-999888777-1",
        };

        var result = await _sut.CreateAsync(dto);

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task Update_WhenEmployeeExists_CallsRepositoryUpdate()
    {
        var entity = SampleEmployee();
        _repo.GetByIdScopedForUpdateAsync(EmployeeId, "demo-tenant", "demo-org", Arg.Any<CancellationToken>())
            .Returns(entity);

        var dto = new EmployeeWriteDto { FirstName = "Ama", LastName = "Updated" };
        var result = await _sut.UpdateAsync(EmployeeId, dto);

        result.Success.Should().BeTrue();
        await _repo.Received(1).UpdateAsync(entity, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_WhenEmployeeNotFound_ReturnsNotFoundRespons()
    {
        _repo.GetByIdScopedForUpdateAsync(EmployeeId, "demo-tenant", "demo-org", Arg.Any<CancellationToken>())
            .Returns((EmployeeEntity?)null);

        var result = await _sut.UpdateAsync(EmployeeId, new EmployeeWriteDto { FirstName = "X", LastName = "Y" });

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task Delete_WhenEmployeeExists_ReturnsSuccess()
    {
        _repo.SoftDeleteScopedAsync(EmployeeId, "demo-tenant", "demo-org", Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _sut.DeleteAsync(EmployeeId);

        result.Success.Should().BeTrue();
        result.Data.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_WhenEmployeeNotFound_ReturnsNotFoundRespons()
    {
        _repo.SoftDeleteScopedAsync(EmployeeId, "demo-tenant", "demo-org", Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await _sut.DeleteAsync(EmployeeId);

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetAll_QueryScopedToCurrentTenant()
    {
        _repo.GetPagedScopedAsync("demo-tenant", "demo-org", 1, 10, Arg.Any<CancellationToken>())
            .Returns((Array.Empty<EmployeeEntity>(), 0));

        await _sut.GetAllAsync(1, 10);

        await _repo.Received(1).GetPagedScopedAsync(
            "demo-tenant",
            "demo-org",
            1,
            10,
            Arg.Any<CancellationToken>());
    }
}
