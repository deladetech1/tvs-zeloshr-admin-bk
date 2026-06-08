using NSubstitute;
using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Persistence.Entities;
using ZelosHR.Api.Persistence.Repositories;
using ZelosHR.Api.Shared.Abstractions;

namespace ZelosHR.Api.Tests.Employees;

public class EmployeeExportServiceTests
{
    private readonly IEmployeeRepository _employees = Substitute.For<IEmployeeRepository>();
    private readonly ICpUserRepository _cpUsers = Substitute.For<ICpUserRepository>();
    private readonly ITenantContext _tenant = Substitute.For<ITenantContext>();
    private readonly EmployeeExportService _sut;

    public EmployeeExportServiceTests()
    {
        _tenant.TenantId.Returns("tenant-1");
        _tenant.OrgId.Returns("org-1");
        _sut = new EmployeeExportService(_employees, _cpUsers, _tenant);
    }

    [Fact]
    public async Task ExportCsvAsync_rejects_start_date_after_end_date()
    {
        var result = await _sut.ExportCsvAsync(new EmployeeExportQuery
        {
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2025, 1, 1),
        });

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Contains("start_date", result.FieldErrors!.Keys);
        await _employees.DidNotReceive().ExportListScopedAsync(
            Arg.Any<EmployeeExportQuery>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExportCsvAsync_returns_csv_bytes()
    {
        _employees.ExportListScopedAsync(
                Arg.Any<EmployeeExportQuery>(), "tenant-1", "org-1", Arg.Any<CancellationToken>())
            .Returns(new List<EmployeeEntity>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    EmployeeCode = "EMP-002",
                    FullName = "Test User",
                    FirstName = "Test",
                    LastName = "User",
                    TenantId = "tenant-1",
                    OrgId = "org-1",
                    LifecycleState = EmployeeLifecycleStates.Active,
                    LifecycleStatus = "active",
                },
            });
        _cpUsers.GetByIdsAsync(Arg.Any<IEnumerable<string>>(), "tenant-1", Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, CpUserDto>());

        var result = await _sut.ExportCsvAsync(new EmployeeExportQuery
        {
            StartDate = new DateOnly(2025, 1, 1),
            EndDate = new DateOnly(2025, 12, 31),
        });

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        var text = System.Text.Encoding.UTF8.GetString(result.Data);
        Assert.StartsWith("employee_id,employee_code", text);
        Assert.Contains("Test User", text);
    }
}
