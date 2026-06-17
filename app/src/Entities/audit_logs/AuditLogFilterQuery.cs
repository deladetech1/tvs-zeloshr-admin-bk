using Microsoft.AspNetCore.Mvc;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Shared.Constants;

namespace ZelosHR.Api.Entities.AuditLogs;

/// <summary>Shared filters for audit log list/export — matches frontend <c>EmployeeAuditLogParams</c>.</summary>
public class AuditLogFilterQuery
{
    /// <summary>Optional text filter (min 3 chars) on actor name, employee name, or action title.</summary>
    [FromQuery(Name = PlatformQueryParams.Search)]
    public string? Search { get; init; }

    /// <summary>Filter by action title substring, or <c>all</c> for no filter.</summary>
    [FromQuery(Name = PlatformQueryParams.Action)]
    public string? Action { get; init; }

    /// <summary>Filter by severity (<c>Low</c>, <c>Medium</c>, <c>High</c>) or <c>all</c>.</summary>
    [FromQuery(Name = PlatformQueryParams.Severity)]
    [SwaggerAllowedValues(typeof(AuditLogFieldOptions), nameof(AuditLogFieldOptions.Severities),
        Description = "Filter by severity or `all`.")]
    public string? Severity { get; init; }

    /// <summary>Filter by platform user id or actor display name substring, or <c>all</c>.</summary>
    [FromQuery(Name = PlatformQueryParams.Actor)]
    public string? Actor { get; init; }

    /// <summary>Activity occurred on or after this date (<c>YYYY-MM-DD</c>).</summary>
    [FromQuery(Name = PlatformQueryParams.StartDate)]
    public DateOnly? StartDate { get; init; }

    /// <summary>Activity occurred on or before this date (<c>YYYY-MM-DD</c>).</summary>
    [FromQuery(Name = PlatformQueryParams.EndDate)]
    public DateOnly? EndDate { get; init; }
}
