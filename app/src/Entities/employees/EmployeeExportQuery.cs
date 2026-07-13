using Microsoft.AspNetCore.Mvc;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Shared.Constants;

namespace ZelosHR.Api.Entities.Employees;

/// <summary>Query filters for <c>GET /api/v1/employees/export</c> (subset of <c>EmployeeParams</c>).</summary>
public sealed class EmployeeExportQuery
{
    /// <summary>Employment start on or after this date (<c>YYYY-MM-DD</c>).</summary>
    [FromQuery(Name = PlatformQueryParams.StartDate)]
    public DateOnly? StartDate { get; init; }

    /// <summary>Employment start on or before this date (<c>YYYY-MM-DD</c>).</summary>
    [FromQuery(Name = PlatformQueryParams.EndDate)]
    public DateOnly? EndDate { get; init; }

    [FromQuery(Name = PlatformQueryParams.Search)]
    public string? Search { get; init; }

    [FromQuery(Name = PlatformQueryParams.EmploymentStatus)]
    [SwaggerAllowedValues(typeof(EmployeeFieldOptions), nameof(EmployeeFieldOptions.EmploymentStatuses),
        Description = "Exact match on stored employment_status (Draft, Active, Probation, …).")]
    public string? EmploymentStatus { get; init; }

    [FromQuery(Name = PlatformQueryParams.Status)]
    [SwaggerAllowedValues(typeof(EmployeeFieldOptions), nameof(EmployeeFieldOptions.ListStatusFilters),
        Description = "Smart filter using simple commands (active, probation, on_leave, …). Ignored when employment_status is set.")]
    public string? Status { get; init; }

    [FromQuery(Name = PlatformQueryParams.DepartmentId)]
    public Guid? DepartmentId { get; init; }

    [FromQuery(Name = PlatformQueryParams.BranchId)]
    public Guid? BranchId { get; init; }

    [FromQuery(Name = PlatformQueryParams.EmploymentType)]
    [SwaggerAllowedValues(typeof(EmployeeFieldOptions), nameof(EmployeeFieldOptions.EmploymentTypes))]
    public string? EmploymentType { get; init; }

    [FromQuery(Name = PlatformQueryParams.WorkLocation)]
    public string? WorkLocation { get; init; }

    [FromQuery(Name = PlatformQueryParams.IncludeInactive)]
    public bool IncludeInactive { get; init; }

    [FromQuery(Name = PlatformQueryParams.IsLineManager)]
    public bool? IsLineManager { get; init; }

    [FromQuery(Name = PlatformQueryParams.IsHeadOfDepartment)]
    public bool? IsHeadOfDepartment { get; init; }
}
