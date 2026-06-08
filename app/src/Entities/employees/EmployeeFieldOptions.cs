namespace ZelosHR.Api.Entities.Employees;

/// <summary>Canonical allowed / suggested values for employee API fields (Swagger + validation hints).</summary>
public static class EmployeeFieldOptions
{
    public static readonly IReadOnlyList<string> CreateStatuses = ["draft", "finalised"];

    public static readonly IReadOnlyList<string> Genders = ["male", "female", "other"];

    public static readonly IReadOnlyList<string> IdTypes = EmployeeIdTypes.Suggested;

    public static readonly IReadOnlyList<string> LifecycleStatesAll =
    [
        EmployeeLifecycleStates.Draft,
        EmployeeLifecycleStates.PreHire,
        EmployeeLifecycleStates.Active,
        EmployeeLifecycleStates.OnLeave,
        EmployeeLifecycleStates.Suspended,
        EmployeeLifecycleStates.Resigned,
        EmployeeLifecycleStates.Terminated,
    ];

    /// <summary>Values accepted by <c>PUT /employees/update</c> → <c>lifecycle_state</c>.</summary>
    public static readonly IReadOnlyList<string> LifecycleStatesForUpdate =
    [
        EmployeeLifecycleStates.PreHire,
        EmployeeLifecycleStates.Active,
        EmployeeLifecycleStates.OnLeave,
        EmployeeLifecycleStates.Suspended,
        EmployeeLifecycleStates.Resigned,
        EmployeeLifecycleStates.Terminated,
    ];

    public static readonly IReadOnlyList<string> EmploymentStatuses =
    [
        EmploymentStatusValues.Draft,
        EmploymentStatusValues.PreHire,
        EmploymentStatusValues.Active,
        EmploymentStatusValues.Probation,
        EmploymentStatusValues.OnLeave,
        EmploymentStatusValues.Suspended,
        EmploymentStatusValues.Resigned,
        EmploymentStatusValues.Terminated,
        EmploymentStatusValues.Inactive,
    ];

    /// <summary>Employment-status values used in legacy list filters and directory KPIs.</summary>
    public static readonly IReadOnlyList<string> DirectoryEmploymentStatuses =
    [
        EmploymentStatusValues.Active,
        EmploymentStatusValues.Probation,
        EmploymentStatusValues.OnLeave,
        EmploymentStatusValues.Suspended,
        EmploymentStatusValues.Resigned,
        EmploymentStatusValues.Terminated,
    ];

    /// <summary>Primary workforce filters for <c>GET /employees/list</c> and directory.</summary>
    public static readonly IReadOnlyList<string> Engagements = EmployeeStatusFilter.Engagements;

    /// <summary>Overlay filters combinable with <see cref="Engagements"/>.</summary>
    public static readonly IReadOnlyList<string> WorkStates = EmployeeStatusFilter.WorkStates;

    /// <summary>Simple list/export filter commands (<c>status=active</c>, <c>status=probation</c>, …).</summary>
    public static readonly IReadOnlyList<string> ListStatusFilters = EmployeeStatusFilter.ListStatusFilters;

    public static readonly IReadOnlyList<string> EmploymentTypes =
        ["Full-time", "Part-time", "Contractor", "Casual"];

    public static readonly IReadOnlyList<string> ContractTypes = ["Permanent", "Fixed-term"];

    /// <summary>How the employee works. <c>remote</c> must not be paired with <c>branch_id</c>.</summary>
    public static readonly IReadOnlyList<string> WorkArrangements =
        ["remote", "hybrid", "on_site", "onsite", "field"];

    /// <summary>Values used by annualized-cost calculation (case-insensitive).</summary>
    public static readonly IReadOnlyList<string> PayFrequencies = ["Monthly", "Bi-weekly", "Weekly", "Annual"];

    public static readonly IReadOnlyList<string> DirectorySortBy =
        ["name", "employeecode", "employeeid", "id", "department", "status", "employmenttype", "type", "jobtitle"];

    public static readonly IReadOnlyList<string> ListSortBy =
        ["name", "employeecode", "employeeid", "id", "department", "status", "employmenttype", "type", "jobtitle"];

    public static readonly IReadOnlyList<string> SortOrder = ["asc", "desc"];
}
