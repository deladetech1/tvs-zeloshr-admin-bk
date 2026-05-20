using Microsoft.AspNetCore.Mvc;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Tenant;

namespace ZelosHR.Api.Entities.Onboarding;

/// <summary>Onboarding tasks per employee — full CRUD.</summary>
[ApiController]
[ApiExplorerSettings(GroupName = SwaggerGroups.Onboarding)]
[Route("api/v1/onboarding")]
[Produces("application/json")]
public class OnboardingController : ControllerBase
{
    private readonly OnboardingService _service;
    private readonly ITenantContextAccessor _tenant;
    public OnboardingController(OnboardingService s, ITenantContextAccessor t) { _service = s; _tenant = t; }

    [HttpGet("summary")]
    public async Task<ActionResult<Respons<OnboardingSummaryDto>>> Summary(CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.GetSummaryAsync(ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet]
    public async Task<ActionResult<Respons<OnboardingListDto>>> List(
        [FromQuery] string? search, [FromQuery] string? status,
        [FromQuery] int page = 1, [FromQuery] int size = 20, CancellationToken ct = default)
    {
        var ctx = _tenant.Current;
        var result = await _service.ListAsync(search, status, page, size, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Respons<OnboardingTaskListItemDto>>> Get(Guid id, CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.GetByIdAsync(id, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost]
    public async Task<ActionResult<Respons<OnboardingTaskListItemDto>>> Create(
        [FromBody] CreateOnboardingTaskDto body, CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.CreateAsync(body, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<Respons<OnboardingTaskListItemDto>>> Update(
        Guid id, [FromBody] UpdateOnboardingTaskDto body, CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.UpdateAsync(id, body, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<Respons<object>>> Delete(Guid id, CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.DeleteAsync(id, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }
}
