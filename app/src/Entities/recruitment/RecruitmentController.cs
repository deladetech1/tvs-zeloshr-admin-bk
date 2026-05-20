using Microsoft.AspNetCore.Mvc;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Tenant;

namespace ZelosHR.Api.Entities.Recruitment;

/// <summary>Job postings — full CRUD.</summary>
[ApiController]
[ApiExplorerSettings(GroupName = SwaggerGroups.Recruitment)]
[Route("api/v1/recruitment")]
[Produces("application/json")]
public class RecruitmentController : ControllerBase
{
    private readonly RecruitmentService _service;
    private readonly ITenantContextAccessor _tenant;
    public RecruitmentController(RecruitmentService service, ITenantContextAccessor tenant)
    {
        _service = service;
        _tenant = tenant;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<Respons<RecruitmentSummaryDto>>> Summary(CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.GetSummaryAsync(ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet]
    public async Task<ActionResult<Respons<JobPostingListDto>>> List(
        [FromQuery] string? search, [FromQuery] string? status,
        [FromQuery] int page = 1, [FromQuery] int size = 20, CancellationToken ct = default)
    {
        var ctx = _tenant.Current;
        var result = await _service.ListAsync(search, status, page, size, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Respons<JobPostingListItemDto>>> Get(Guid id, CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.GetByIdAsync(id, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost]
    public async Task<ActionResult<Respons<JobPostingListItemDto>>> Create(
        [FromBody] CreateJobPostingDto body, CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.CreateAsync(body, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<Respons<JobPostingListItemDto>>> Update(
        Guid id, [FromBody] UpdateJobPostingDto body, CancellationToken ct)
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
