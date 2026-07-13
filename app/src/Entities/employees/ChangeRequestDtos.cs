using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Shared.Constants;

namespace ZelosHR.Api.Entities.Employees;

/// <summary>Single employee profile change request (self-service approval queue item).</summary>
public sealed class ChangeRequestReadDto
{
    /// <summary>Change request UUID.</summary>
    public required Guid Id { get; init; }

    /// <summary>Employee UUID the change applies to.</summary>
    public required Guid EmployeeId { get; init; }

    /// <summary>Dot path into the employee update JSON (e.g. <c>identity.full_name</c>).</summary>
    public required string FieldPath { get; init; }

    /// <summary>Previous value at <see cref="FieldPath"/> when the request was created; null for new fields.</summary>
    public JsonNode? OldValue { get; init; }

    /// <summary>Proposed value replayed through PUT /employees/update when approved.</summary>
    public required JsonNode NewValue { get; init; }

    /// <summary>Workflow status: pending · approved · rejected · superseded.</summary>
    [SwaggerAllowedValues(typeof(ChangeRequestFieldOptions), nameof(ChangeRequestFieldOptions.Statuses))]
    public required string Status { get; init; }

    /// <summary>Platform user id of the employee who submitted the change.</summary>
    public required string RequestedById { get; init; }

    /// <summary>Display name resolved from <see cref="RequestedById"/>.</summary>
    public string? RequestedBy { get; init; }

    /// <summary>Platform user id of the HR reviewer (null while pending).</summary>
    public string? ReviewedById { get; init; }

    /// <summary>Display name resolved from <see cref="ReviewedById"/>.</summary>
    public string? ReviewedBy { get; init; }

    /// <summary>Optional HR note on reject (or internal review context).</summary>
    public string? ReviewNote { get; init; }

    /// <summary>When the request was created (UTC).</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>When the request last changed (UTC).</summary>
    public DateTimeOffset UpdatedAt { get; init; }

    /// <summary>Audit: creator platform user id.</summary>
    public string? CreatedById { get; init; }

    /// <summary>Audit: last updater platform user id.</summary>
    public string? UpdatedById { get; init; }

    /// <summary>Audit: creator display name.</summary>
    public string? CreatedBy { get; init; }

    /// <summary>Audit: last updater display name.</summary>
    public string? UpdatedBy { get; init; }
}

/// <summary>Result of PUT /employees/me/update — immediate applies plus queued approvals.</summary>
public sealed class EmployeeSelfUpdateResultDto
{
    /// <summary>Employee aggregate after free-tier fields were applied (unchanged when only approval fields were sent).</summary>
    public EmployeeAggregateReadDto? Employee { get; init; }

    /// <summary>Change requests created for approval-tier fields (status pending).</summary>
    public IReadOnlyList<ChangeRequestReadDto> Pending { get; init; } = [];

    /// <summary>Field paths applied immediately (free tier from GET /employees/field-policy).</summary>
    public IReadOnlyList<string> Applied { get; init; } = [];

    /// <summary>Field paths rejected (admin-only — not listed in field-policy).</summary>
    public IReadOnlyList<string> Rejected { get; init; } = [];
}

/// <summary>Optional body for POST /change-requests/{change_request_id}/reject.</summary>
public sealed class RejectChangeRequestBody
{
    /// <summary>Reason shown to the employee (optional).</summary>
    public string? ReviewNote { get; init; }
}

/// <summary>Query params for GET /change-requests (HR review queue).</summary>
public sealed class ChangeRequestListQuery
{
    /// <summary>Filter by workflow status. Omit for all statuses.</summary>
    [Microsoft.AspNetCore.Mvc.FromQuery(Name = "status")]
    [SwaggerAllowedValues(typeof(ChangeRequestFieldOptions), nameof(ChangeRequestFieldOptions.Statuses))]
    public string? Status { get; init; }

    /// <summary>Scope the queue to one employee UUID.</summary>
    [FromQuery(Name = "employee_id")]
    public Guid? EmployeeId { get; init; }

    /// <summary>1-based page index (default 1).</summary>
    [FromQuery(Name = PlatformQueryParams.Page)]
    public int Page { get; init; } = 1;

    /// <summary>Page size (default 20, max 100).</summary>
    [FromQuery(Name = PlatformQueryParams.Size)]
    public int Size { get; init; } = 20;
}
