using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Employees.Authorization;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Authorization;

namespace ZelosHR.Api.Entities.Employees;

/// <summary>Employee self-service profile updates and field policy.</summary>
[ApiController]
[ApiExplorerSettings(GroupName = SwaggerGroups.Employees)]
[Route("api/v1/employees")]
[Produces("application/json")]
public class EmployeeSelfServiceController : ControllerBase
{
    private readonly EmployeeSelfServiceService _selfService;
    private readonly ChangeRequestService _changeRequests;

    public EmployeeSelfServiceController(
        EmployeeSelfServiceService selfService,
        ChangeRequestService changeRequests)
    {
        _selfService = selfService;
        _changeRequests = changeRequests;
    }

    /// <summary>
    /// Partial self-update — same JSON shape as admin update, filtered by <see cref="FieldPolicy"/>.
    /// Free fields apply immediately; approval fields create change requests; admin-only fields are rejected.
    /// </summary>
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeGet)]
    [HttpPut("me/update")]
    [ProducesResponseType(typeof(Respons<EmployeeSelfUpdateResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<EmployeeSelfUpdateResultDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Respons<EmployeeSelfUpdateResultDto>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Respons<EmployeeSelfUpdateResultDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Respons<EmployeeSelfUpdateResultDto>>> SelfUpdate(
        [FromBody] JsonObject body,
        CancellationToken ct)
    {
        var result = await _selfService.SelfUpdateAsync(body, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Lists change requests for the employee linked to the current user.</summary>
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeGet)]
    [HttpGet("me/change-requests")]
    [ProducesResponseType(typeof(Respons<IReadOnlyList<ChangeRequestReadDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Respons<IReadOnlyList<ChangeRequestReadDto>>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Respons<IReadOnlyList<ChangeRequestReadDto>>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Respons<IReadOnlyList<ChangeRequestReadDto>>>> ListMyChangeRequests(
        [FromQuery(Name = "status")] string? status,
        CancellationToken ct)
    {
        var (employeeId, error) = await _selfService.ResolveLinkedEmployeeAsync(ct);
        if (error is not null)
        {
            return StatusCode(error.StatusCode, Respons<IReadOnlyList<ChangeRequestReadDto>>.Fail(
                error.Error ?? error.Detail ?? "Request failed.",
                error.StatusCode,
                error.Detail));
        }

        var result = await _changeRequests.ListForEmployeeAsync(employeeId, status, ct);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Employee-visible field edit policy matrix (free / approval / admin-only by omission).</summary>
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeGet)]
    [HttpGet("field-policy")]
    [ProducesResponseType(typeof(Respons<IReadOnlyList<FieldPolicyEntryDto>>), StatusCodes.Status200OK)]
    public ActionResult<Respons<IReadOnlyList<FieldPolicyEntryDto>>> GetFieldPolicy()
    {
        var result = Respons<IReadOnlyList<FieldPolicyEntryDto>>.Ok(FieldPolicy.ListForEmployeeUi());
        return Ok(result);
    }
}
