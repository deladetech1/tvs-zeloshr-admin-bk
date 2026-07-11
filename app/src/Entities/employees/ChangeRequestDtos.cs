using System.Text.Json.Nodes;

namespace ZelosHR.Api.Entities.Employees;

public sealed class ChangeRequestReadDto
{
    public required Guid Id { get; init; }
    public required Guid EmployeeId { get; init; }
    public required string FieldPath { get; init; }
    public JsonNode? OldValue { get; init; }
    public required JsonNode NewValue { get; init; }
    public required string Status { get; init; }
    public required string RequestedById { get; init; }
    public string? RequestedBy { get; init; }
    public string? ReviewedById { get; init; }
    public string? ReviewedBy { get; init; }
    public string? ReviewNote { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public string? CreatedById { get; init; }
    public string? UpdatedById { get; init; }
    public string? CreatedBy { get; init; }
    public string? UpdatedBy { get; init; }
}

public sealed class EmployeeSelfUpdateResultDto
{
    public EmployeeAggregateReadDto? Employee { get; init; }
    public IReadOnlyList<ChangeRequestReadDto> Pending { get; init; } = [];
    public IReadOnlyList<string> Applied { get; init; } = [];
    public IReadOnlyList<string> Rejected { get; init; } = [];
}

public sealed class RejectChangeRequestBody
{
    public string? ReviewNote { get; init; }
}

public sealed class ChangeRequestListQuery
{
    [Microsoft.AspNetCore.Mvc.FromQuery(Name = "status")]
    public string? Status { get; init; }

    [Microsoft.AspNetCore.Mvc.FromQuery(Name = "employee_id")]
    public Guid? EmployeeId { get; init; }
}
