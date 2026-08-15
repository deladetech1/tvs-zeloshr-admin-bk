using Microsoft.Extensions.Options;
using Trovesuite.Package.Configuration;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Shared.Abstractions;

namespace ZelosHR.Api.Entities.Employees;

/// <summary>
/// Sends a Trovesuite activation email when a newly finalised employee can access the employee portal.
/// </summary>
public sealed class EmployeeOnboardingInviteService
{
    private readonly ICpUserRepository _cpUsers;
    private readonly IEmployeeRepository _employees;
    private readonly EmployeePortal.IEmployeeActivationInviteSender _activation;
    private readonly ITenantContext _tenant;
    private readonly AppSettings _appSettings;
    private readonly ILogger<EmployeeOnboardingInviteService> _logger;

    public EmployeeOnboardingInviteService(
        ICpUserRepository cpUsers,
        IEmployeeRepository employees,
        EmployeePortal.IEmployeeActivationInviteSender activation,
        ITenantContext tenant,
        IOptions<AppSettings> appSettings,
        ILogger<EmployeeOnboardingInviteService> logger)
    {
        _cpUsers = cpUsers;
        _employees = employees;
        _activation = activation;
        _tenant = tenant;
        _appSettings = appSettings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Sends the activation invite when the employee is finalised and has an eligible employment status.
    /// Failures are logged only; they do not affect the employee create/update transaction.
    /// </summary>
    public async Task TrySendAfterFinaliseAsync(Guid employeeId, CancellationToken ct = default)
    {
        try
        {
            var entity = await _employees.GetByIdScopedAsync(
                employeeId, _tenant.TenantId, _tenant.OrgId, ct);
            if (entity is null || entity.IsDraft)
                return;

            if (!EmployeeOnboardingInviteEligibility.IsEligible(entity.EmploymentStatus))
                return;

            CpUserDto? cp = null;
            if (!string.IsNullOrWhiteSpace(entity.UserId))
                cp = await _cpUsers.GetByIdAsync(entity.UserId, _tenant.TenantId, ct);

            var workEmail = EmployeeIdentityResolver.ResolveWorkEmail(entity, cp);
            if (string.IsNullOrWhiteSpace(workEmail))
            {
                _logger.LogWarning(
                    "Skipping onboarding activation for employee {EmployeeId}: no work email.",
                    employeeId);
                return;
            }

            if (cp is null)
            {
                _logger.LogWarning(
                    "Skipping onboarding activation for employee {EmployeeId}: platform user not linked.",
                    employeeId);
                return;
            }

            await _activation.IssueAndSendActivationAsync(
                entity,
                cp,
                _tenant.TenantId,
                _tenant.OrgId,
                _tenant.BusId,
                ct);

            _logger.LogInformation(
                "Onboarding activation processed for {WorkEmail}, employee {EmployeeId} ({EmploymentStatus}).",
                workEmail,
                employeeId,
                entity.EmploymentStatus);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Onboarding activation email failed for employee {EmployeeId}; employee record was saved.",
                employeeId);
        }
    }
}
