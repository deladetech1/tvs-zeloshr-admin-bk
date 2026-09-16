using Microsoft.AspNetCore.Mvc;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Constants;
using ZelosHR.Api.Shared.Tenant;

namespace ZelosHR.Api.Entities.Adjustments;

[ApiController]
[ApiExplorerSettings(GroupName = SwaggerGroups.Adjustments)]
[Route("api/v1/adjustments")]
[Produces("application/json")]
public sealed class AdjustmentController : ControllerBase
{
    private readonly AdjustmentService _service;
    private readonly ITenantContextAccessor _tenant;

    public AdjustmentController(AdjustmentService service, ITenantContextAccessor tenant)
    {
        _service = service;
        _tenant = tenant;
    }

    [HttpGet("list")]
    public async Task<ActionResult<Respons<IReadOnlyList<AdjustmentDto>>>> List(
        [FromQuery(Name = PlatformQueryParams.EmployeeId)] Guid? employeeId,
        [FromQuery(Name = PlatformQueryParams.FromDate)] DateOnly? fromDate,
        [FromQuery(Name = PlatformQueryParams.ToDate)] DateOnly? toDate,
        [FromQuery(Name = PlatformQueryParams.Page)] int page = 1,
        [FromQuery(Name = PlatformQueryParams.Size)] int size = 20,
        CancellationToken ct = default)
    {
        var ctx = _tenant.Current;
        var result = await _service.ListAsync(employeeId, fromDate, toDate, page, size, ctx.TenantId, ctx.OrgId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("add")]
    public async Task<ActionResult<Respons<AdjustmentDto>>> Create(
        [FromBody] CreateAdjustmentDto body,
        CancellationToken ct)
    {
        var ctx = _tenant.Current;
        var result = await _service.CreateAsync(body, ctx.TenantId, ctx.OrgId, ctx.UserId, ct);
        return StatusCode(result.StatusCode, result);
    }
}
