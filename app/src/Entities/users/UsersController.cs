using Microsoft.AspNetCore.Mvc;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Authorization;
using ZelosHR.Api.Shared.Tenant;

namespace ZelosHR.Api.Entities.Users;

/// <summary>Trovesuite platform user directory (<c>core_platform.cp_users</c> + <c>cp_members</c>).</summary>
[ApiController]
[ApiExplorerSettings(GroupName = SwaggerGroups.Users)]
[Route("api/v1/users")]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly PlatformUsersService _service;
    private readonly ITenantContextAccessor _tenant;

    public UsersController(PlatformUsersService service, ITenantContextAccessor tenant)
    {
        _service = service;
        _tenant = tenant;
    }

    /// <summary>Paginated platform users with filters (Core Platform parity).</summary>
    /// <remarks>
    /// Reads <c>core_platform.cp_users</c> joined to <c>cp_members</c> (same scope as Core Platform
    /// <c>GET /api/v1/users/get-users</c>). HR-onboarded users without a <c>cp_members</c> row are excluded.
    /// </remarks>
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeGet)]
    [HttpGet("get-users")]
    [ProducesResponseType(typeof(Respons<IReadOnlyList<PlatformUserListItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Respons<IReadOnlyList<PlatformUserListItemDto>>>> GetUsers(
        [FromQuery] GetUsersQuery query,
        CancellationToken ct = default)
    {
        var ctx = _tenant.Current;
        var result = await _service.GetUsersAsync(query, ctx.TenantId, ct);
        return StatusCode(result.StatusCode, result);
    }
}
