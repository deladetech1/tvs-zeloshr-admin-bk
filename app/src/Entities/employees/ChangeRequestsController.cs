using Microsoft.AspNetCore.Mvc;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Authorization;

namespace ZelosHR.Api.Entities.Employees;

/// <summary>
/// HR review queue for employee profile change requests created by self-service updates.
/// </summary>
/// <remarks>
/// Change requests are created when an employee submits an <c>approval</c>-tier field via
/// <c>PUT /api/v1/employees/me/update</c>. Each row stores <c>field_path</c>, <c>old_value</c>,
/// and <c>new_value</c> for replay on approve.
/// </remarks>
[ApiController]
[ApiExplorerSettings(GroupName = SwaggerGroups.Employees)]
[Route("api/v1/change-requests")]
[Produces("application/json")]
public class ChangeRequestsController : ControllerBase
{
    private readonly ChangeRequestService _changeRequests;

    public ChangeRequestsController(ChangeRequestService changeRequests) =>
        _changeRequests = changeRequests;

    /// <summary>Lists change requests for HR review.</summary>
    /// <remarks>
    /// Filter by <c>status</c> (pending · approved · rejected · superseded) and/or <c>employee_id</c>.
    /// Returns <c>data[]</c> of change-request rows with standard audit fields on each item.
    /// </remarks>
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

    /// <summary>Approves a pending change request.</summary>
    /// <remarks>
    /// Replays <c>new_value</c> through the same pipeline as <c>PUT /employees/update</c> for the target employee.
    /// Returns the updated employee aggregate. Responds **409** when the request is no longer pending.
    /// </remarks>
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
    /// <remarks>
    /// Sets status to <c>rejected</c>. Optional <c>review_note</c> in the body is stored on the row
    /// and returned to the employee on <c>GET /employees/me/change-requests</c>.
    /// </remarks>
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
