using Microsoft.AspNetCore.Mvc;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Shared.Constants;

namespace ZelosHR.Api.Entities.OrgStructure;

/// <summary>
/// Query params for <c>GET /org-structure/departments/list</c> and
/// <c>GET /org-structure/branches/list</c> — matches frontend <c>OrgStructureParams</c>.
/// </summary>
public sealed class OrgStructureListQuery
{
    [FromQuery(Name = PlatformQueryParams.Page)]
    public int Page { get; init; } = 1;

    [FromQuery(Name = PlatformQueryParams.Size)]
    public int Size { get; init; } = 15;

    [FromQuery(Name = PlatformQueryParams.SortBy)]
    [SwaggerAllowedValues(typeof(OrgStructureFieldOptions), nameof(OrgStructureFieldOptions.ListSortBy))]
    public string SortBy { get; init; } = "name";

    [FromQuery(Name = PlatformQueryParams.SortOrder)]
    [SwaggerAllowedValues(typeof(OrgStructureFieldOptions), nameof(OrgStructureFieldOptions.SortOrder))]
    public string SortOrder { get; init; } = "asc";

    [FromQuery(Name = PlatformQueryParams.Search)]
    public string? Search { get; init; }

    [FromQuery(Name = PlatformQueryParams.IncludeArchived)]
    public bool IncludeArchived { get; init; }
}
