using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.EmployeePortal;
using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Persistence.Entities;
using ZelosHR.Api.Shared.Abstractions;

namespace ZelosHR.Api.Tests.Employees;

public class EmployeeOnboardingInviteServiceTests
{
    private readonly ICpUserRepository _cpUsers = Substitute.For<ICpUserRepository>();
    private readonly IEmployeeRepository _employees = Substitute.For<IEmployeeRepository>();
    private readonly IEmployeeActivationInviteSender _activation = Substitute.For<IEmployeeActivationInviteSender>();
    private readonly ITenantContext _tenant = Substitute.For<ITenantContext>();

    public EmployeeOnboardingInviteServiceTests()
    {
        _tenant.TenantId.Returns("demo-tenant");
        _tenant.OrgId.Returns("demo-org");
        _tenant.BusId.Returns("demo-bus");
    }

    [Fact]
    public async Task TrySendAfterFinaliseAsync_triggers_activation_for_active_finalised_employee()
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
        var cp = new CpUserDto("user-1", "Ama Mensah", "ama@company.com", null, true, null, null, null, null);
        _cpUsers.GetByIdAsync("user-1", "demo-tenant", Arg.Any<CancellationToken>()).Returns(cp);

        await CreateService().TrySendAfterFinaliseAsync(employeeId);

        await _activation.Received(1).IssueAndSendActivationAsync(
            entity,
            cp,
            "demo-tenant",
            "demo-org",
            "demo-bus",
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

        await _activation.DidNotReceiveWithAnyArgs().IssueAndSendActivationAsync(
            default!, default!, default!, default!, default!, default);
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

        await _activation.DidNotReceiveWithAnyArgs().IssueAndSendActivationAsync(
            default!, default!, default!, default!, default!, default);
    }

    private EmployeeOnboardingInviteService CreateService() =>
        new(
            _cpUsers,
            _employees,
            _activation,
            _tenant,
            Options.Create(new AppSettings { EmployeePortalDomain = "dev.zeloshr.com" }),
            NullLogger<EmployeeOnboardingInviteService>.Instance);
}
