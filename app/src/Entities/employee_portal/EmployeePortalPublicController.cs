using Microsoft.AspNetCore.Mvc;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Shared;

namespace ZelosHR.Api.Entities.EmployeePortal;

/// <summary>Public employee portal bootstrap — no authentication.</summary>
[ApiController]
[ApiExplorerSettings(GroupName = SwaggerGroups.EmployeePortal)]
[Route("api/v1/public/employee-portal")]
[Produces("application/json")]
public sealed class EmployeePortalPublicController : ControllerBase
{
    private readonly EmployeePortalSubdomainService _service;

    public EmployeePortalPublicController(EmployeePortalSubdomainService service) => _service = service;

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
        var result = await _service.ResolveBySubdomainAsync(subdomain ?? string.Empty, ct);
        return StatusCode(result.StatusCode, result);
    }
}
