using Microsoft.AspNetCore.Mvc;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Shared;

namespace ZelosHR.Api.Entities.EmployeePortal;

/// <summary>Employee portal activation — no authentication.</summary>
[ApiController]
[ApiExplorerSettings(GroupName = SwaggerGroups.EmployeePortal)]
[Route("api/v1/employee-portal/activation")]
[Produces("application/json")]
public sealed class EmployeePortalActivationController : ControllerBase
{
    private readonly EmployeePortalActivationService _activation;

    public EmployeePortalActivationController(EmployeePortalActivationService activation) =>
        _activation = activation;

    /// <summary>Validate an employee activation token before showing the set-password form.</summary>
    [HttpGet("validate")]
    [ProducesResponseType(typeof(Respons<EmployeeActivationValidateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<EmployeeActivationValidateDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Respons<EmployeeActivationValidateDto>>> Validate(
        [FromQuery(Name = "token")] string? token,
        CancellationToken ct)
    {
        var result = await _activation.ValidateTokenAsync(token, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Set password and complete employee portal activation.</summary>
    [HttpPost("set-password")]
    [ProducesResponseType(typeof(Respons<EmployeeActivationSetPasswordResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<EmployeeActivationSetPasswordResultDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Respons<EmployeeActivationSetPasswordResultDto>>> SetPassword(
        [FromBody] EmployeeActivationSetPasswordDto body,
        CancellationToken ct)
    {
        var result = await _activation.SetPasswordAsync(body, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Request a new activation link (expired or missing email).</summary>
    [HttpPost("resend")]
    [ProducesResponseType(typeof(Respons<EmployeeActivationResendResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<EmployeeActivationResendResultDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Respons<EmployeeActivationResendResultDto>>> Resend(
        [FromBody] EmployeeActivationResendDto body,
        CancellationToken ct)
    {
        var result = await _activation.ResendAsync(body, ct);
        return StatusCode(result.StatusCode, result);
    }
}
