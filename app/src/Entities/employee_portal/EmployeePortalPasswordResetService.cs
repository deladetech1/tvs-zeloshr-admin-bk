using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Trovesuite.Package.Utils;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Persistence.Entities;
using ZelosHR.Api.Persistence.Repositories;

namespace ZelosHR.Api.Entities.EmployeePortal;

public sealed class EmployeePortalPasswordResetService
{
    private const string GenericRequestMessage =
        "If an account exists for that email address, we've sent a password reset link.";

    private const string RateLimitMessage =
        "For security, we limit the number of password reset requests. Please wait a while before trying again.";

    private readonly IEmployeeActivationRepository _tokens;
    private readonly IEmployeePortalSubdomainRepository _subdomains;
    private readonly IEmployeeRepository _employees;
    private readonly ICpUserRepository _cpUsers;
    private readonly ICompanyProfileRepository _companyProfiles;
    private readonly ICpBusinessRepository _businesses;
    private readonly IHelper _helper;
    private readonly AppSettings _appSettings;
    private readonly ILogger<EmployeePortalPasswordResetService> _logger;

    public EmployeePortalPasswordResetService(
        IEmployeeActivationRepository tokens,
        IEmployeePortalSubdomainRepository subdomains,
        IEmployeeRepository employees,
        ICpUserRepository cpUsers,
        ICompanyProfileRepository companyProfiles,
        ICpBusinessRepository businesses,
        IHelper helper,
        IOptions<AppSettings> appSettings,
        ILogger<EmployeePortalPasswordResetService> logger)
    {
        _tokens = tokens;
        _subdomains = subdomains;
        _employees = employees;
        _cpUsers = cpUsers;
        _companyProfiles = companyProfiles;
        _businesses = businesses;
        _helper = helper;
        _appSettings = appSettings.Value;
        _logger = logger;
    }

    public async Task<Respons<EmployeePasswordResetValidateDto>> ValidateTokenAsync(
        string? token,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return Respons<EmployeePasswordResetValidateDto>.ValidationError(new Dictionary<string, string>
            {
                ["token"] = "Reset token is required.",
            });
        }

        var row = await _tokens.FindActivePasswordResetTokenAsync(token, ct);
        if (row is null)
        {
            return Respons<EmployeePasswordResetValidateDto>.Ok(new EmployeePasswordResetValidateDto
            {
                IsValid = false,
                IsExpired = false,
                IsNotActivated = false,
            });
        }

        var expiresAt = ResolveExpiresAt(row.Otp.Cdatetime);
        var isExpired = DateTimeOffset.UtcNow > expiresAt;

        if (!row.HasPassword)
        {
            return Respons<EmployeePasswordResetValidateDto>.Ok(new EmployeePasswordResetValidateDto
            {
                IsValid = false,
                IsExpired = isExpired,
                IsNotActivated = true,
                ExpiresAt = expiresAt,
            });
        }

        if (isExpired)
        {
            return Respons<EmployeePasswordResetValidateDto>.Ok(new EmployeePasswordResetValidateDto
            {
                IsValid = false,
                IsExpired = true,
                IsNotActivated = false,
                ExpiresAt = expiresAt,
            });
        }

        var employee = await _employees.GetByPlatformUserIdTenantScopedAsync(row.User.Id, row.User.TenantId, ct);
        var orgId = employee?.OrgId;
        var branding = await ResolveBrandingAsync(row.User.TenantId, orgId, ct);
        var (firstName, _) = SplitName(row.User.Fullname);

        return Respons<EmployeePasswordResetValidateDto>.Ok(new EmployeePasswordResetValidateDto
        {
            IsValid = true,
            IsExpired = false,
            IsNotActivated = false,
            FirstName = firstName,
            CompanyName = branding.CompanyName,
            Subdomain = branding.Subdomain,
            ExpiresAt = expiresAt,
        });
    }

    public async Task<Respons<EmployeePasswordResetSetPasswordResultDto>> SetPasswordAsync(
        EmployeePasswordResetSetPasswordDto body,
        CancellationToken ct = default)
    {
        var errors = ValidateSetPasswordBody(body);
        if (errors is not null)
            return Respons<EmployeePasswordResetSetPasswordResultDto>.ValidationError(errors);

        var row = await _tokens.FindActivePasswordResetTokenAsync(body.Token!, ct);
        if (row is null)
        {
            return Respons<EmployeePasswordResetSetPasswordResultDto>.Fail(
                "Invalid or expired reset link.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (!row.HasPassword)
        {
            return Respons<EmployeePasswordResetSetPasswordResultDto>.ValidationError(new Dictionary<string, string>
            {
                ["token"] = "This account has not been activated yet. Use the activation link from your welcome email.",
            });
        }

        var expiresAt = ResolveExpiresAt(row.Otp.Cdatetime);
        if (DateTimeOffset.UtcNow > expiresAt)
        {
            return Respons<EmployeePasswordResetSetPasswordResultDto>.ValidationError(new Dictionary<string, string>
            {
                ["token"] = "This reset link has expired. Request a new link from the employee portal.",
            });
        }

        var policy = await _tokens.GetActivePasswordPolicyAsync(row.User.TenantId, ct);
        var passwordErrors = EmployeePortalPasswordValidator.Validate(body.Password!, policy);
        if (passwordErrors.Count > 0)
        {
            return Respons<EmployeePasswordResetSetPasswordResultDto>.ValidationError(
                passwordErrors.Select((msg, i) => new KeyValuePair<string, string>($"password[{i}]", msg))
                    .ToDictionary(kv => kv.Key, kv => kv.Value));
        }

        if (EmployeePortalPasswordValidator.MatchesCurrentPassword(body.Password!, row.User.LoginPassword))
        {
            return Respons<EmployeePasswordResetSetPasswordResultDto>.ValidationError(new Dictionary<string, string>
            {
                ["password"] = "Must not match your current password.",
            });
        }

        var hashed = EmployeePortalPasswordHasher.Hash(body.Password!);
        await _tokens.SetUserPasswordAsync(row.User.TenantId, row.User.Id, hashed, ct);
        await _cpUsers.EnsureCpMemberAsync(row.User.Id, row.User.TenantId, createdBy: row.User.Id, ct);
        await _tokens.ConsumeTokenAsync(row.Otp.Id, row.Otp.TenantId, ct);

        var branding = await ResolveBrandingAsync(row.User.TenantId, null, ct);
        var portalUrl = branding.Subdomain is not null
            ? EmployeePortalSubdomainRules.PortalHost(branding.Subdomain, _appSettings.EmployeePortalDomain)
            : _appSettings.AppUrl.Trim().TrimEnd('/');

        _logger.LogInformation(
            "Employee password reset completed for user {UserId} in tenant {TenantId}.",
            row.User.Id,
            row.User.TenantId);

        return Respons<EmployeePasswordResetSetPasswordResultDto>.Ok(new EmployeePasswordResetSetPasswordResultDto
        {
            PortalUrl = portalUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? portalUrl
                : $"https://{portalUrl}",
            WorkEmail = row.User.Email,
        });
    }

    public Task<Respons<EmployeePasswordResetRequestResultDto>> RequestAsync(
        EmployeePasswordResetRequestDto body,
        CancellationToken ct = default) =>
        SendResetLinkAsync(body, ct);

    public Task<Respons<EmployeePasswordResetRequestResultDto>> ResendAsync(
        EmployeePasswordResetRequestDto body,
        CancellationToken ct = default) =>
        SendResetLinkAsync(body, ct);

    private async Task<Respons<EmployeePasswordResetRequestResultDto>> SendResetLinkAsync(
        EmployeePasswordResetRequestDto body,
        CancellationToken ct)
    {
        var errors = ValidateRequestBody(body);
        if (errors is not null)
            return Respons<EmployeePasswordResetRequestResultDto>.ValidationError(errors);

        var normalizedSubdomain = EmployeePortalSubdomainRules.Normalize(body.Subdomain!);
        var portal = await _subdomains.GetBySubdomainAsync(normalizedSubdomain, ct);
        if (portal is null)
        {
            return Respons<EmployeePasswordResetRequestResultDto>.Ok(new EmployeePasswordResetRequestResultDto
            {
                Message = GenericRequestMessage,
            });
        }

        var workEmail = body.WorkEmail!.Trim().ToLowerInvariant();
        var cpUser = await _cpUsers.FindByEmailAsync(workEmail, portal.TenantId, ct);
        if (cpUser is null)
        {
            return Respons<EmployeePasswordResetRequestResultDto>.Ok(new EmployeePasswordResetRequestResultDto
            {
                Message = GenericRequestMessage,
            });
        }

        var employee = await _employees.GetByPlatformUserIdScopedAsync(
            cpUser.Id, portal.TenantId, portal.OrgId, ct);

        if (employee is null
            || employee.IsDraft
            || string.IsNullOrWhiteSpace(employee.UserId)
            || !EmployeeOnboardingInviteEligibility.IsEligible(employee.EmploymentStatus))
        {
            return Respons<EmployeePasswordResetRequestResultDto>.Ok(new EmployeePasswordResetRequestResultDto
            {
                Message = GenericRequestMessage,
            });
        }

        if (!await _tokens.UserHasPasswordAsync(portal.TenantId, employee.UserId, ct))
        {
            return Respons<EmployeePasswordResetRequestResultDto>.Ok(new EmployeePasswordResetRequestResultDto
            {
                Message = GenericRequestMessage,
            });
        }

        if (await IsRateLimitedAsync(portal.TenantId, cpUser.Id, ct))
        {
            return Respons<EmployeePasswordResetRequestResultDto>.Fail(
                RateLimitMessage,
                statusCode: StatusCodes.Status429TooManyRequests);
        }

        try
        {
            await SendResetEmailAsync(
                employee,
                cpUser,
                portal.Subdomain,
                portal.TenantId,
                portal.OrgId,
                portal.BusId,
                ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Password reset email failed for employee {EmployeeId}.",
                employee.Id);
        }

        return Respons<EmployeePasswordResetRequestResultDto>.Ok(new EmployeePasswordResetRequestResultDto
        {
            Message = GenericRequestMessage,
        });
    }

    private async Task SendResetEmailAsync(
        EmployeeEntity employee,
        CpUserDto cpUser,
        string subdomain,
        string tenantId,
        string orgId,
        string busId,
        CancellationToken ct)
    {
        var workEmail = EmployeeIdentityResolver.ResolveWorkEmail(employee, cpUser);
        if (string.IsNullOrWhiteSpace(workEmail))
            return;

        var token = GenerateToken();
        var time = _helper.CurrentDateTime();
        await _tokens.CreatePasswordResetTokenAsync(
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
        var resetUrl = EmployeePortalSubdomainRules.BuildPasswordResetUrl(
            subdomain, token, _appSettings.EmployeePortalDomain);
        var expiryHours = Math.Max(1, _appSettings.EmployeePasswordResetExpiryHours).ToString();

        var variables = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["company_name"] = branding.CompanyName,
            ["company_short_name"] = branding.CompanyShortName,
            ["first_name"] = firstName,
            ["reset_url"] = resetUrl,
            ["expiry_hours"] = expiryHours,
        };

        await _helper.SendNotificationAsync(
            workEmail.Trim(),
            $"Reset your {branding.CompanyShortName} employee portal password",
            EmployeePortalPasswordResetTemplates.TextTemplate,
            EmployeePortalPasswordResetTemplates.HtmlTemplate,
            variables,
            tenantId,
            ct);

        _logger.LogInformation(
            "Password reset email sent to {WorkEmail} for employee {EmployeeId}.",
            workEmail,
            employee.Id);
    }

    private async Task<bool> IsRateLimitedAsync(string tenantId, string userId, CancellationToken ct)
    {
        var windowMinutes = Math.Max(1, _appSettings.EmployeePasswordResetRateLimitWindowMinutes);
        var maxRequests = Math.Max(1, _appSettings.EmployeePasswordResetRateLimitMax);
        var since = DateTimeOffset.UtcNow.AddMinutes(-windowMinutes);
        var count = await _tokens.CountRecentPasswordResetTokensAsync(tenantId, userId, since, ct);
        return count >= maxRequests;
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
        var hours = Math.Max(1, _appSettings.EmployeePasswordResetExpiryHours);
        return created.AddHours(hours);
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

    private static Dictionary<string, string>? ValidateSetPasswordBody(EmployeePasswordResetSetPasswordDto body)
    {
        var errors = new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(body.Token))
            errors["token"] = "Reset token is required.";
        if (string.IsNullOrWhiteSpace(body.Password))
            errors["password"] = "Password is required.";
        if (string.IsNullOrWhiteSpace(body.ConfirmPassword))
            errors["confirm_password"] = "Confirm password is required.";
        else if (!string.Equals(body.Password, body.ConfirmPassword, StringComparison.Ordinal))
            errors["confirm_password"] = "Passwords do not match.";

        return errors.Count > 0 ? errors : null;
    }

    private static Dictionary<string, string>? ValidateRequestBody(EmployeePasswordResetRequestDto body)
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
