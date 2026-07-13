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
    /// Partial self-update — same JSON shape as admin update, filtered by field policy.
    /// </summary>
    /// <remarks>
    /// Consult <c>GET /employees/field-policy</c> before building the payload:
    /// <list type="bullet">
    /// <item><description><c>free</c> — applied immediately; path listed in <c>data.applied[]</c></description></item>
    /// <item><description><c>approval</c> — creates a pending change request; full rows in <c>data.pending[]</c></description></item>
    /// <item><description>Admin-only (path omitted from field-policy) — rejected in <c>data.rejected[]</c></description></item>
    /// </list>
    /// Requires a linked <c>zhr_employees.user_id</c> for the authenticated platform user.
    /// </remarks>
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
    /// <remarks>
    /// Same row shape as <c>GET /change-requests</c>, scoped to the employee profile for the JWT user.
    /// Returns **404** when no <c>zhr_employees</c> row is linked (typical for HR admin accounts).
    /// Optional <c>status</c> filter: pending · approved · rejected · superseded.
    /// </remarks>
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

    /// <summary>Employee-visible field edit policy matrix.</summary>
    /// <remarks>
    /// Each entry is a JSON path (<c>path</c>) and self-service tier (<c>access</c>: free | approval).
    /// Paths not returned are admin-only and rejected on <c>PUT /employees/me/update</c>.
    /// </remarks>
    [RequiresZelosHrPermission(ZelosHrPermissions.EmployeeGet)]
    [HttpGet("field-policy")]
    [ProducesResponseType(typeof(Respons<IReadOnlyList<FieldPolicyEntryDto>>), StatusCodes.Status200OK)]
    public ActionResult<Respons<IReadOnlyList<FieldPolicyEntryDto>>> GetFieldPolicy()
    {
        var result = Respons<IReadOnlyList<FieldPolicyEntryDto>>.Ok(FieldPolicy.ListForEmployeeUi());
        return Ok(result);
    }
}
