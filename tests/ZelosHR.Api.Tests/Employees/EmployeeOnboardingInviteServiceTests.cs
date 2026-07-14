using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Trovesuite.Package.Configuration;
using Trovesuite.Package.Utils;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Persistence.Entities;
using ZelosHR.Api.Shared.Abstractions;

namespace ZelosHR.Api.Tests.Employees;

public class EmployeeOnboardingInviteServiceTests
{
    private readonly IHelper _helper = Substitute.For<IHelper>();
    private readonly ICpUserRepository _cpUsers = Substitute.For<ICpUserRepository>();
    private readonly IEmployeeRepository _employees = Substitute.For<IEmployeeRepository>();
    private readonly ITenantContext _tenant = Substitute.For<ITenantContext>();

    public EmployeeOnboardingInviteServiceTests()
    {
        _tenant.TenantId.Returns("demo-tenant");
        _tenant.OrgId.Returns("demo-org");
    }

    [Fact]
    public async Task TrySendAfterFinaliseAsync_sends_for_active_finalised_employee()
    {
        var employeeId = Guid.NewGuid();
        var entity = new EmployeeEntity
        {
            Id = employeeId,
            TenantId = "demo-tenant",
            OrgId = "demo-org",
            EmployeeCode = "EMP-0042",
            EmploymentStatus = EmploymentStatusValues.Active,
            IsDraft = false,
            UserId = "user-1",
            FullName = "Ama Mensah",
        };

        _employees.GetByIdScopedAsync(employeeId, "demo-tenant", "demo-org", Arg.Any<CancellationToken>())
            .Returns(entity);
        _cpUsers.GetByIdAsync("user-1", "demo-tenant", Arg.Any<CancellationToken>())
            .Returns(new CpUserDto("user-1", "Ama Mensah", "ama@company.com", null, true, null, null, null, null));

        var service = CreateService();

        await service.TrySendAfterFinaliseAsync(employeeId);

        await _helper.Received(1).SendNotificationAsync(
            "ama@company.com",
            Arg.Is<string>(s => s.Contains("sign in", StringComparison.OrdinalIgnoreCase)),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Is<IDictionary<string, string?>>(v =>
                v["full_name"] == "Ama Mensah"
                && v["work_email"] == "ama@company.com"
                && v["employee_code"] == "EMP-0042"),
            "demo-tenant",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TrySendAfterFinaliseAsync_skips_draft_employee()
    {
        var employeeId = Guid.NewGuid();
        _employees.GetByIdScopedAsync(employeeId, "demo-tenant", "demo-org", Arg.Any<CancellationToken>())
            .Returns(new EmployeeEntity
            {
                Id = employeeId,
                TenantId = "demo-tenant",
                OrgId = "demo-org",
                EmployeeCode = "EMP-0001",
                EmploymentStatus = EmploymentStatusValues.Active,
                IsDraft = true,
            });

        await CreateService().TrySendAfterFinaliseAsync(employeeId);

        await _helper.DidNotReceiveWithAnyArgs().SendNotificationAsync(
            default!, default!, default!, default!, default, default, default);
    }

    [Fact]
    public async Task TrySendAfterFinaliseAsync_skips_terminated_status()
    {
        var employeeId = Guid.NewGuid();
        _employees.GetByIdScopedAsync(employeeId, "demo-tenant", "demo-org", Arg.Any<CancellationToken>())
            .Returns(new EmployeeEntity
            {
                Id = employeeId,
                TenantId = "demo-tenant",
                OrgId = "demo-org",
                EmployeeCode = "EMP-0099",
                EmploymentStatus = EmploymentStatusValues.Terminated,
                IsDraft = false,
                WorkEmail = "gone@company.com",
            });

        await CreateService().TrySendAfterFinaliseAsync(employeeId);

        await _helper.DidNotReceiveWithAnyArgs().SendNotificationAsync(
            default!, default!, default!, default!, default, default, default);
    }

    private EmployeeOnboardingInviteService CreateService() =>
        new(
            _helper,
            _cpUsers,
            _employees,
            _tenant,
            Options.Create(new AppSettings { AppName = "ZelosHR", AppUrl = "https://app.zeloshr.test" }),
            Options.Create(new TrovesuiteOptions()),
            NullLogger<EmployeeOnboardingInviteService>.Instance);
}
