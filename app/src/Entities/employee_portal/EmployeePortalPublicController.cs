using Microsoft.AspNetCore.Mvc;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Shared;

namespace ZelosHR.Api.Entities.EmployeePortal;

/// <summary>Public employee portal bootstrap and activation — no authentication.</summary>
[ApiController]
[ApiExplorerSettings(GroupName = SwaggerGroups.EmployeePortal)]
[Route("api/v1/public/employee-portal")]
[Produces("application/json")]
public sealed class EmployeePortalPublicController : ControllerBase
{
    private readonly EmployeePortalSubdomainService _subdomains;
    private readonly EmployeePortalActivationService _activation;

    public EmployeePortalPublicController(
        EmployeePortalSubdomainService subdomains,
        EmployeePortalActivationService activation)
    {
        _subdomains = subdomains;
        _activation = activation;
    }

    /// <summary>Resolve a portal subdomain to tenant/org context.</summary>
    /// <remarks>
    /// Called by the employee portal app on load (e.g. <c>btl.zeloshr.com</c> → subdomain <c>btl</c>).
    /// Returns Trove headers the portal should use after login. No JWT required.
    /// </remarks>
    [HttpGet("resolve")]
    [ProducesResponseType(typeof(Respons<EmployeePortalResolveDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<EmployeePortalResolveDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Respons<EmployeePortalResolveDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Respons<EmployeePortalResolveDto>>> Resolve(
        [FromQuery(Name = "subdomain")] string? subdomain,
        CancellationToken ct)
    {
        var result = await _subdomains.ResolveBySubdomainAsync(subdomain ?? string.Empty, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Validate an employee activation token before showing the set-password form.</summary>
    [HttpGet("activation/validate")]
    [ProducesResponseType(typeof(Respons<EmployeeActivationValidateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<EmployeeActivationValidateDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Respons<EmployeeActivationValidateDto>>> ValidateActivation(
        [FromQuery(Name = "token")] string? token,
        CancellationToken ct)
    {
        var result = await _activation.ValidateTokenAsync(token, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Set password and complete employee portal activation.</summary>
    [HttpPost("activation/set-password")]
    [ProducesResponseType(typeof(Respons<EmployeeActivationSetPasswordResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<EmployeeActivationSetPasswordResultDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Respons<EmployeeActivationSetPasswordResultDto>>> SetActivationPassword(
        [FromBody] EmployeeActivationSetPasswordDto body,
        CancellationToken ct)
    {
        var result = await _activation.SetPasswordAsync(body, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Request a new activation link (expired or missing email).</summary>
    [HttpPost("activation/resend")]
    [ProducesResponseType(typeof(Respons<EmployeeActivationResendResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<EmployeeActivationResendResultDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Respons<EmployeeActivationResendResultDto>>> ResendActivation(
        [FromBody] EmployeeActivationResendDto body,
        CancellationToken ct)
    {
        var result = await _activation.ResendAsync(body, ct);
        return StatusCode(result.StatusCode, result);
    }
}
