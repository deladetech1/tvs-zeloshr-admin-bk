using Microsoft.AspNetCore.Mvc;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Shared;

namespace ZelosHR.Api.Entities.EmployeePortal;

/// <summary>Employee portal forgot-password — no authentication.</summary>
[ApiController]
[ApiExplorerSettings(GroupName = SwaggerGroups.EmployeePortal)]
[Route("api/v1/employee-portal/password-reset")]
[Produces("application/json")]
public sealed class EmployeePortalPasswordResetController : ControllerBase
{
    private readonly EmployeePortalPasswordResetService _passwordReset;

    public EmployeePortalPasswordResetController(EmployeePortalPasswordResetService passwordReset) =>
        _passwordReset = passwordReset;

    /// <summary>Request a password reset link for an activated employee account.</summary>
    [HttpPost("request")]
    [ProducesResponseType(typeof(Respons<EmployeePasswordResetRequestResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<EmployeePasswordResetRequestResultDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Respons<EmployeePasswordResetRequestResultDto>), StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<Respons<EmployeePasswordResetRequestResultDto>>> Request(
        [FromBody] EmployeePasswordResetRequestDto body,
        CancellationToken ct)
    {
        var result = await _passwordReset.RequestAsync(body, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Validate a password reset token before showing the reset form.</summary>
    [HttpGet("validate")]
    [ProducesResponseType(typeof(Respons<EmployeePasswordResetValidateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<EmployeePasswordResetValidateDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Respons<EmployeePasswordResetValidateDto>>> Validate(
        [FromQuery(Name = "token")] string? token,
        CancellationToken ct)
    {
        var result = await _passwordReset.ValidateTokenAsync(token, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Reset password using a valid token.</summary>
    [HttpPost("set-password")]
    [ProducesResponseType(typeof(Respons<EmployeePasswordResetSetPasswordResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<EmployeePasswordResetSetPasswordResultDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Respons<EmployeePasswordResetSetPasswordResultDto>>> SetPassword(
        [FromBody] EmployeePasswordResetSetPasswordDto body,
        CancellationToken ct)
    {
        var result = await _passwordReset.SetPasswordAsync(body, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Resend a password reset link (same as request).</summary>
    [HttpPost("resend")]
    [ProducesResponseType(typeof(Respons<EmployeePasswordResetRequestResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<EmployeePasswordResetRequestResultDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Respons<EmployeePasswordResetRequestResultDto>), StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<Respons<EmployeePasswordResetRequestResultDto>>> Resend(
        [FromBody] EmployeePasswordResetRequestDto body,
        CancellationToken ct)
    {
        var result = await _passwordReset.ResendAsync(body, ct);
        return StatusCode(result.StatusCode, result);
    }
}
