using Microsoft.AspNetCore.Mvc;
using Trovesuite.Package.Auth;
using Trovesuite.Package.Notification;
using Trovesuite.Package.Storage;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Shared.Tenant;

namespace ZelosHR.Api.Entities.Platform;

/// <summary>Trovesuite auth, email, and storage integration endpoints.</summary>
[ApiController]
[ApiExplorerSettings(GroupName = SwaggerGroups.TrovesuitePlatform, IgnoreApi = true)]
[Route("api/v1/platform")]
[Produces("application/json")]
public class TrovesuitePlatformController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly INotificationService _notification;
    private readonly IStorageService _storage;

    public TrovesuitePlatformController(
        IAuthService auth,
        INotificationService notification,
        IStorageService storage)
    {
        _auth = auth;
        _notification = notification;
        _storage = storage;
    }

    /// <summary>Verify a JWT and return role/permission entries from core_platform.</summary>
    [HttpPost("auth/verify")]
    public async Task<IActionResult> VerifyToken(CancellationToken ct)
    {
        var header = Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(header))
            return BadRequest(new { error = "Authorization: Bearer {token} header is required." });

        var token = header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? header["Bearer ".Length..].Trim()
            : header;

        var result = await _auth.AuthorizeUserFromTokenAsync(token, ct);
        return StatusCode(result.StatusCode > 0 ? result.StatusCode : 200, result);
    }

    /// <summary>Authorize by user/tenant IDs (service-to-service).</summary>
    [HttpPost("auth/authorize")]
    public async Task<IActionResult> Authorize([FromBody] AuthServiceWriteDto body, CancellationToken ct)
    {
        var result = await _auth.AuthorizeAsync(body, ct);
        return StatusCode(result.StatusCode > 0 ? result.StatusCode : 200, result);
    }

    /// <summary>Current request context after Trovesuite middleware (or demo headers).</summary>
    [HttpGet("auth/context")]
    public IActionResult GetContext([FromServices] ITenantContextAccessor tenantAccessor)
    {
        var ctx = tenantAccessor.Current;
        var permissions = HttpContext.Items[TrovesuiteHttpContextKeys.Permissions] as IReadOnlyList<string>;

        return Ok(new
        {
            tenantId = ctx.TenantId,
            orgId = ctx.OrgId,
            userId = HttpContext.Items[TrovesuiteHttpContextKeys.UserId],
            permissions,
        });
    }

    /// <summary>Send a test email via Trovesuite notification service.</summary>
    [HttpPost("notifications/email")]
    public async Task<IActionResult> SendEmail(
        [FromBody] NotificationEmailServiceWriteDto body,
        CancellationToken ct)
    {
        var result = await _notification.SendEmailAsync(body, ct);
        return StatusCode(result.StatusCode > 0 ? result.StatusCode : 200, result);
    }

    /// <summary>Issue a read URL for a blob (requires Azure configuration).</summary>
    [HttpPost("storage/file-url")]
    public async Task<IActionResult> GetFileUrl(
        [FromBody] StorageFileUrlServiceWriteDto body,
        CancellationToken ct)
    {
        var result = await _storage.GetFileUrlAsync(body, ct);
        return StatusCode(result.StatusCode > 0 ? result.StatusCode : 200, result);
    }
}
