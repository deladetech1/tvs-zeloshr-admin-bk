using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Trovesuite.Package.Utils;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Persistence.Entities;
using ZelosHR.Api.Persistence.Repositories;

namespace ZelosHR.Api.Entities.EmployeePortal;

public sealed class EmployeePortalActivationService : IEmployeeActivationInviteSender
{
    private readonly IEmployeeActivationRepository _activation;
    private readonly IEmployeePortalSubdomainRepository _subdomains;
    private readonly IEmployeeRepository _employees;
    private readonly ICpUserRepository _cpUsers;
    private readonly ICompanyProfileRepository _companyProfiles;
    private readonly ICpBusinessRepository _businesses;
    private readonly IHelper _helper;
    private readonly AppSettings _appSettings;
    private readonly ILogger<EmployeePortalActivationService> _logger;

    public EmployeePortalActivationService(
        IEmployeeActivationRepository activation,
        IEmployeePortalSubdomainRepository subdomains,
        IEmployeeRepository employees,
        ICpUserRepository cpUsers,
        ICompanyProfileRepository companyProfiles,
        ICpBusinessRepository businesses,
        IHelper helper,
        IOptions<AppSettings> appSettings,
        ILogger<EmployeePortalActivationService> logger)
    {
        _activation = activation;
        _subdomains = subdomains;
        _employees = employees;
        _cpUsers = cpUsers;
        _companyProfiles = companyProfiles;
        _businesses = businesses;
        _helper = helper;
        _appSettings = appSettings.Value;
        _logger = logger;
    }

    public async Task<Respons<EmployeeActivationValidateDto>> ValidateTokenAsync(
        string? token,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return Respons<EmployeeActivationValidateDto>.ValidationError(new Dictionary<string, string>
            {
                ["token"] = "Activation token is required.",
            });
        }

        var row = await _activation.FindActiveTokenAsync(token, ct);
        if (row is null)
        {
            return Respons<EmployeeActivationValidateDto>.Ok(new EmployeeActivationValidateDto
            {
                IsValid = false,
                IsExpired = false,
                IsAlreadyActivated = false,
            });
        }

        var expiresAt = ResolveExpiresAt(row.Otp.Cdatetime);
        var isExpired = DateTimeOffset.UtcNow > expiresAt;

        if (row.HasPassword)
        {
            return Respons<EmployeeActivationValidateDto>.Ok(new EmployeeActivationValidateDto
            {
                IsValid = false,
                IsExpired = isExpired,
                IsAlreadyActivated = true,
                ExpiresAt = expiresAt,
            });
        }

        if (isExpired)
        {
            return Respons<EmployeeActivationValidateDto>.Ok(new EmployeeActivationValidateDto
            {
                IsValid = false,
                IsExpired = true,
                IsAlreadyActivated = false,
                ExpiresAt = expiresAt,
            });
        }

        var employee = await _employees.GetByPlatformUserIdTenantScopedAsync(row.User.Id, row.User.TenantId, ct);
        var orgId = employee?.OrgId;
        var branding = await ResolveBrandingAsync(row.User.TenantId, orgId, ct);
        var (firstName, _) = SplitName(row.User.Fullname);

        return Respons<EmployeeActivationValidateDto>.Ok(new EmployeeActivationValidateDto
        {
            IsValid = true,
            IsExpired = false,
            IsAlreadyActivated = false,
            FirstName = firstName,
            CompanyName = branding.CompanyName,
            Subdomain = branding.Subdomain,
            ExpiresAt = expiresAt,
        });
    }

    public async Task<Respons<EmployeeActivationSetPasswordResultDto>> SetPasswordAsync(
        EmployeeActivationSetPasswordDto body,
        CancellationToken ct = default)
    {
        var errors = ValidateSetPasswordBody(body);
        if (errors is not null)
            return Respons<EmployeeActivationSetPasswordResultDto>.ValidationError(errors);

        var row = await _activation.FindActiveTokenAsync(body.Token!, ct);
        if (row is null)
        {
            return Respons<EmployeeActivationSetPasswordResultDto>.Fail(
                "Invalid or expired activation link.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (row.HasPassword)
        {
            return Respons<EmployeeActivationSetPasswordResultDto>.ValidationError(new Dictionary<string, string>
            {
                ["token"] = "This account has already been activated. Sign in or use Forgot password.",
            });
        }

        var expiresAt = ResolveExpiresAt(row.Otp.Cdatetime);
        if (DateTimeOffset.UtcNow > expiresAt)
        {
            return Respons<EmployeeActivationSetPasswordResultDto>.ValidationError(new Dictionary<string, string>
            {
                ["token"] = "This activation link has expired. Request a new link from the employee portal.",
            });
        }

        var policy = await _activation.GetActivePasswordPolicyAsync(row.User.TenantId, ct);
        var passwordErrors = EmployeePortalPasswordValidator.Validate(body.Password!, policy);
        if (passwordErrors.Count > 0)
        {
            return Respons<EmployeeActivationSetPasswordResultDto>.ValidationError(
                passwordErrors.Select((msg, i) => new KeyValuePair<string, string>($"password[{i}]", msg))
                    .ToDictionary(kv => kv.Key, kv => kv.Value));
        }

        var hashed = EmployeePortalPasswordHasher.Hash(body.Password!);
        await _activation.SetUserPasswordAsync(row.User.TenantId, row.User.Id, hashed, ct);
        await _activation.ConsumeTokenAsync(row.Otp.Id, row.Otp.TenantId, ct);

        var branding = await ResolveBrandingAsync(row.User.TenantId, null, ct);
        var portalUrl = branding.Subdomain is not null
            ? EmployeePortalSubdomainRules.PortalHost(branding.Subdomain, _appSettings.EmployeePortalDomain)
            : _appSettings.AppUrl.Trim().TrimEnd('/');

        _logger.LogInformation(
            "Employee activation completed for user {UserId} in tenant {TenantId}.",
            row.User.Id,
            row.User.TenantId);

        return Respons<EmployeeActivationSetPasswordResultDto>.Ok(new EmployeeActivationSetPasswordResultDto
        {
            PortalUrl = portalUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? portalUrl
                : $"https://{portalUrl}",
            WorkEmail = row.User.Email,
        });
    }

    public async Task<Respons<EmployeeActivationResendResultDto>> ResendAsync(
        EmployeeActivationResendDto body,
        CancellationToken ct = default)
    {
        const string genericMessage =
            "If an eligible employee account exists, a new activation link has been sent to the work email provided.";

        var errors = ValidateResendBody(body);
        if (errors is not null)
            return Respons<EmployeeActivationResendResultDto>.ValidationError(errors);

        var normalizedSubdomain = EmployeePortalSubdomainRules.Normalize(body.Subdomain!);
        var portal = await _subdomains.GetBySubdomainAsync(normalizedSubdomain, ct);
        if (portal is null)
        {
            return Respons<EmployeeActivationResendResultDto>.Ok(new EmployeeActivationResendResultDto
            {
                Message = genericMessage,
            });
        }

        var workEmail = body.WorkEmail!.Trim().ToLowerInvariant();
        var cpUser = await _cpUsers.FindByEmailAsync(workEmail, portal.TenantId, ct);
        if (cpUser is null)
        {
            return Respons<EmployeeActivationResendResultDto>.Ok(new EmployeeActivationResendResultDto
            {
                Message = genericMessage,
            });
        }

        var employee = await _employees.GetByPlatformUserIdScopedAsync(
            cpUser.Id, portal.TenantId, portal.OrgId, ct);

        if (employee is null
            || employee.IsDraft
            || string.IsNullOrWhiteSpace(employee.UserId)
            || !EmployeeOnboardingInviteEligibility.IsEligible(employee.EmploymentStatus))
        {
            return Respons<EmployeeActivationResendResultDto>.Ok(new EmployeeActivationResendResultDto
            {
                Message = genericMessage,
            });
        }

        if (await _activation.UserHasPasswordAsync(portal.TenantId, employee.UserId, ct))
        {
            return Respons<EmployeeActivationResendResultDto>.Ok(new EmployeeActivationResendResultDto
            {
                Message = genericMessage,
            });
        }

        try
        {
            await SendActivationEmailAsync(
                employee,
                portal.Subdomain,
                portal.TenantId,
                portal.OrgId,
                portal.BusId,
                ct,
                cpUser);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Activation resend email failed for employee {EmployeeId}.",
                employee.Id);
        }

        return Respons<EmployeeActivationResendResultDto>.Ok(new EmployeeActivationResendResultDto
        {
            Message = genericMessage,
        });
    }

    /// <summary>Creates a token and sends the activation email when portal subdomain is configured.</summary>
    public async Task IssueAndSendActivationAsync(
        EmployeeEntity employee,
        CpUserDto cpUser,
        string tenantId,
        string orgId,
        string busId,
        CancellationToken ct = default)
    {
        if (await _activation.UserHasPasswordAsync(tenantId, cpUser.Id, ct))
            return;

        var portal = await _subdomains.GetEntityAsync(tenantId, orgId, ct);
        if (portal is null)
        {
            _logger.LogWarning(
                "Skipping activation email for employee {EmployeeId}: portal subdomain not configured.",
                employee.Id);
            return;
        }

        await SendActivationEmailAsync(employee, portal.Subdomain, tenantId, orgId, busId, ct, cpUser);
    }

    private async Task SendActivationEmailAsync(
        EmployeeEntity employee,
        string subdomain,
        string tenantId,
        string orgId,
        string busId,
        CancellationToken ct,
        CpUserDto? cpUser = null)
    {
        cpUser ??= new CpUserDto(
            employee.UserId!,
            employee.FullName,
            employee.WorkEmail ?? string.Empty,
            null,
            true);

        var workEmail = EmployeeIdentityResolver.ResolveWorkEmail(employee, cpUser);
        if (string.IsNullOrWhiteSpace(workEmail))
            return;

        var token = GenerateToken();
        var time = _helper.CurrentDateTime();
        await _activation.CreateActivationTokenAsync(
            tenantId,
            cpUser.Id,
            workEmail,
            token,
            createdBy: cpUser.Id,
            time.CDate,
            time.CTime,
            time.CDateTime,
            ct);

        var branding = await ResolveBrandingAsync(tenantId, orgId, busId, subdomain, ct);
        var (firstName, _) = SplitName(EmployeeIdentityResolver.ResolveFullName(employee, cpUser));
        var activationUrl = EmployeePortalSubdomainRules.BuildActivationUrl(
            subdomain, token, _appSettings.EmployeePortalDomain);
        var expiryDays = Math.Max(1, _appSettings.EmployeeActivationExpiryDays).ToString();

        var variables = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["company_name"] = branding.CompanyName,
            ["company_short_name"] = branding.CompanyShortName,
            ["first_name"] = firstName,
            ["activation_url"] = activationUrl,
            ["expiry_days"] = expiryDays,
        };

        await _helper.SendNotificationAsync(
            workEmail.Trim(),
            $"Activate your {branding.CompanyShortName} employee account",
            EmployeePortalActivationTemplates.TextTemplate,
            EmployeePortalActivationTemplates.HtmlTemplate,
            variables,
            tenantId,
            ct);

        _logger.LogInformation(
            "Activation email sent to {WorkEmail} for employee {EmployeeId}.",
            workEmail,
            employee.Id);
    }

    private async Task<(string CompanyName, string CompanyShortName, string? Subdomain)> ResolveBrandingAsync(
        string tenantId,
        string? orgId,
        CancellationToken ct)
    {
        EmployeePortalSubdomainEntity? portal = null;
        if (!string.IsNullOrWhiteSpace(orgId))
            portal = await _subdomains.GetEntityAsync(tenantId, orgId, ct);

        var busId = portal?.BusId ?? string.Empty;
        return await ResolveBrandingAsync(tenantId, orgId ?? string.Empty, busId, portal?.Subdomain, ct);
    }

    private async Task<(string CompanyName, string CompanyShortName, string? Subdomain)> ResolveBrandingAsync(
        string tenantId,
        string orgId,
        string busId,
        string? subdomain,
        CancellationToken ct)
    {
        var defaultName = string.IsNullOrWhiteSpace(busId)
            ? "Your company"
            : await _businesses.GetBusNameAsync(tenantId, busId, ct) ?? "Your company";

        var profile = await _companyProfiles.GetEntityAsync(tenantId, orgId, ct);
        var legal = profile?.LegalName?.Trim();
        var trading = profile?.TradingName?.Trim();
        var companyName = legal ?? trading ?? defaultName;
        var companyShort = trading ?? legal ?? defaultName;

        return (companyName, companyShort, subdomain);
    }

    private DateTimeOffset ResolveExpiresAt(DateTimeOffset? createdAt)
    {
        var created = createdAt ?? DateTimeOffset.UtcNow;
        var days = Math.Max(1, _appSettings.EmployeeActivationExpiryDays);
        return created.AddDays(days);
    }

    private static string GenerateToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static (string First, string Last) SplitName(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return ("there", string.Empty);

        var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 1
            ? (parts[0], string.Empty)
            : (parts[0], string.Join(' ', parts.Skip(1)));
    }

    private static Dictionary<string, string>? ValidateSetPasswordBody(EmployeeActivationSetPasswordDto body)
    {
        var errors = new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(body.Token))
            errors["token"] = "Activation token is required.";
        if (string.IsNullOrWhiteSpace(body.Password))
            errors["password"] = "Password is required.";
        if (string.IsNullOrWhiteSpace(body.ConfirmPassword))
            errors["confirm_password"] = "Confirm password is required.";
        else if (!string.Equals(body.Password, body.ConfirmPassword, StringComparison.Ordinal))
            errors["confirm_password"] = "Passwords do not match.";

        return errors.Count > 0 ? errors : null;
    }

    private static Dictionary<string, string>? ValidateResendBody(EmployeeActivationResendDto body)
    {
        var errors = new Dictionary<string, string>();
        var subdomainErrors = EmployeePortalSubdomainRules.Validate(body.Subdomain);
        if (subdomainErrors is not null)
        {
            foreach (var kv in subdomainErrors)
                errors[kv.Key] = kv.Value;
        }

        if (string.IsNullOrWhiteSpace(body.WorkEmail))
            errors["work_email"] = "Work email is required.";
        else if (!body.WorkEmail.Contains('@'))
            errors["work_email"] = "Work email must be a valid email address.";

        return errors.Count > 0 ? errors : null;
    }
}
