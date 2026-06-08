using Microsoft.AspNetCore.Mvc;
using ZelosHR.Api.Configs;

namespace ZelosHR.Api.Entities.Employees;

/// <summary>Query filters for <c>GET /api/v1/employees/export</c>.</summary>
public sealed class EmployeeExportQuery
{
    /// <summary>Employment start on or after this date (<c>YYYY-MM-DD</c>).</summary>
    [FromQuery(Name = "start_date")]
    public DateOnly? StartDate { get; init; }

    /// <summary>Employment start on or before this date (<c>YYYY-MM-DD</c>).</summary>
    [FromQuery(Name = "end_date")]
    public DateOnly? EndDate { get; init; }

    public string? Search { get; init; }

    [FromQuery(Name = "employment_status")]
    [SwaggerAllowedValues(typeof(EmployeeFieldOptions), nameof(EmployeeFieldOptions.EmploymentStatuses))]
    public string? EmploymentStatus { get; init; }

    [FromQuery(Name = "department_id")]
    public Guid? DepartmentId { get; init; }

    [FromQuery(Name = "branch_id")]
    public Guid? BranchId { get; init; }

    [FromQuery(Name = "employment_type")]
    [SwaggerAllowedValues(typeof(EmployeeFieldOptions), nameof(EmployeeFieldOptions.EmploymentTypes))]
    public string? EmploymentType { get; init; }

    [FromQuery(Name = "work_location")]
    public string? WorkLocation { get; init; }

    [FromQuery(Name = "include_inactive")]
    public bool IncludeInactive { get; init; }
}
