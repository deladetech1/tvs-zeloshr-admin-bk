using Microsoft.AspNetCore.Mvc;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Shared.Tenant;

namespace ZelosHR.Api.Entities.Shared;

/// <summary>Machine-readable route map for frontends (from OpenAPI / API explorer).</summary>
[ApiController]
[ApiExplorerSettings(GroupName = SwaggerGroups.Discovery)]
[Route("api/v1/navigation")]
public class NavigationController : ControllerBase
{
    private readonly NavigationService _navigation;

    public NavigationController(NavigationService navigation) => _navigation = navigation;

    [HttpGet]
    public IActionResult GetNavigationMap() => Ok(_navigation.Build());
}

public sealed class NavigationMapResponse
{
    public IReadOnlyList<NavigationModuleDto> Modules { get; init; } = [];
    public TenantHeadersDto TenantHeaders { get; init; } = new();
}

public sealed class NavigationModuleDto
{
    public required string Id { get; init; }
    public required string Label { get; init; }
    public IReadOnlyList<NavigationModuleDto>? Children { get; init; }
    public IReadOnlyList<string>? Endpoints { get; init; }
}

/// <summary>Development fallback when JWT is disabled. Production uses Trovesuite JWT claims.</summary>
public sealed class TenantHeadersDto
{
    public string TenantId { get; init; } = LocalDevelopmentDefaults.TenantId;
    public string OrgId { get; init; } = LocalDevelopmentDefaults.OrgId;
}
