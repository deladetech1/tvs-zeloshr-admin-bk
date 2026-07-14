using Microsoft.Extensions.Options;
using Trovesuite.Package.Configuration;
using Trovesuite.Package.Utils;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Shared.Tenant;

namespace ZelosHR.Api.Entities.Employees;

/// <summary>
/// Sends a Trovesuite notification email so a newly finalised employee can sign in with their work email.
/// </summary>
public sealed class EmployeeOnboardingInviteService
{
    private const string TextTemplate =
        """
        Hello {full_name},

        Your {app_name} employee account has been created with status: {employment_status}.

        Sign in with your work email ({work_email}) at {sign_in_url} to access ZelosHR.

        Employee code: {employee_code}

        If you have not set a password yet, use "Forgot password" on the sign-in page.

        {cdate} at {ctime}
        """;

    private const string HtmlTemplate =
        """
        <p>Hello <strong>{full_name}</strong>,</p>
        <p>Your <strong>{app_name}</strong> employee account has been created with status: <strong>{employment_status}</strong>.</p>
        <p>Sign in with your work email (<a href="mailto:{work_email}">{work_email}</a>) at
        <a href="{sign_in_url}">{sign_in_url}</a> to access ZelosHR.</p>
        <p>Employee code: <strong>{employee_code}</strong></p>
        <p>If you have not set a password yet, use <strong>Forgot password</strong> on the sign-in page.</p>
        <p><small>{cdate} at {ctime}</small></p>
        """;

    private readonly IHelper _helper;
    private readonly ICpUserRepository _cpUsers;
    private readonly IEmployeeRepository _employees;
    private readonly ITenantContext _tenant;
    private readonly AppSettings _appSettings;
    private readonly TrovesuiteOptions _trovesuiteOptions;
    private readonly ILogger<EmployeeOnboardingInviteService> _logger;

    public EmployeeOnboardingInviteService(
        IHelper helper,
        ICpUserRepository cpUsers,
        IEmployeeRepository employees,
        ITenantContext tenant,
        IOptions<AppSettings> appSettings,
        IOptions<TrovesuiteOptions> trovesuiteOptions,
        ILogger<EmployeeOnboardingInviteService> logger)
    {
        _helper = helper;
        _cpUsers = cpUsers;
        _employees = employees;
        _tenant = tenant;
        _appSettings = appSettings.Value;
        _trovesuiteOptions = trovesuiteOptions.Value;
        _logger = logger;
    }

    /// <summary>
    /// Sends the invite when the employee is finalised and has an eligible employment status.
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
                    "Skipping onboarding invite for employee {EmployeeId}: no work email.",
                    employeeId);
                return;
            }

            var fullName = EmployeeIdentityResolver.ResolveFullName(entity, cp);
            var appName = string.IsNullOrWhiteSpace(_appSettings.AppName)
                ? _trovesuiteOptions.App.Name
                : _appSettings.AppName;
            var signInUrl = ResolveSignInUrl();

            var variables = new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["full_name"] = fullName,
                ["app_name"] = appName,
                ["employment_status"] = entity.EmploymentStatus?.Trim(),
                ["work_email"] = workEmail.Trim(),
                ["employee_code"] = entity.EmployeeCode,
                ["sign_in_url"] = signInUrl,
            };

            var subject = $"Welcome to {appName} — sign in to your account";

            await _helper.SendNotificationAsync(
                workEmail.Trim(),
                subject,
                TextTemplate,
                HtmlTemplate,
                variables,
                _tenant.TenantId,
                ct);

            _logger.LogInformation(
                "Onboarding invite sent to {WorkEmail} for employee {EmployeeId} ({EmploymentStatus}).",
                workEmail,
                employeeId,
                entity.EmploymentStatus);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Onboarding invite email failed for employee {EmployeeId}; employee record was saved.",
                employeeId);
        }
    }

    private string ResolveSignInUrl()
    {
        if (!string.IsNullOrWhiteSpace(_appSettings.AppUrl))
            return _appSettings.AppUrl.Trim().TrimEnd('/');

        if (!string.IsNullOrWhiteSpace(_trovesuiteOptions.App.AppUrl))
            return _trovesuiteOptions.App.AppUrl.Trim().TrimEnd('/');

        return "https://zeloshr.com";
    }
}
