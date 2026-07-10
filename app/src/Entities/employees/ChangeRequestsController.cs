using Microsoft.AspNetCore.Mvc;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Authorization;

namespace ZelosHR.Api.Entities.Employees;

/// <summary>HR review queue for employee profile change requests.</summary>
[ApiController]
[ApiExplorerSettings(GroupName = SwaggerGroups.Employees)]
[Route("api/v1/change-requests")]
[Produces("application/json")]
public class ChangeRequestsController : ControllerBase
{
    private readonly ChangeRequestService _changeRequests;

    public ChangeRequestsController(ChangeRequestService changeRequests) =>
        _changeRequests = changeRequests;

    /// <summary>Lists change requests for HR review (filter by status and/or employee_id).</summary>
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeGet)]
    [HttpGet]
    [ProducesResponseType(typeof(Respons<IReadOnlyList<ChangeRequestReadDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Respons<IReadOnlyList<ChangeRequestReadDto>>>> List(
        [FromQuery] ChangeRequestListQuery query,
        CancellationToken ct)
    {
        var result = await _changeRequests.ListAsync(query, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Approves a pending change request and replays the stored patch through the employee update pipeline.</summary>
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeUpdate)]
    [HttpPost("{changeRequestId:guid}/approve")]
    [ProducesResponseType(typeof(Respons<EmployeeAggregateReadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<EmployeeAggregateReadDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Respons<EmployeeAggregateReadDto>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Respons<EmployeeAggregateReadDto>>> Approve(
        Guid changeRequestId,
        CancellationToken ct)
    {
        var result = await _changeRequests.ApproveAsync(changeRequestId, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Rejects a pending change request.</summary>
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeUpdate)]
    [HttpPost("{changeRequestId:guid}/reject")]
    [ProducesResponseType(typeof(Respons<ChangeRequestReadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<ChangeRequestReadDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Respons<ChangeRequestReadDto>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Respons<ChangeRequestReadDto>>> Reject(
        Guid changeRequestId,
        [FromBody] RejectChangeRequestBody? body,
        CancellationToken ct)
    {
        var result = await _changeRequests.RejectAsync(changeRequestId, body?.ReviewNote, ct);
        return StatusCode(result.StatusCode, result);
    }
}
