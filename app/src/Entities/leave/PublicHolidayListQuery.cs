using Microsoft.AspNetCore.Mvc;
using ZelosHR.Api.Shared.Constants;

namespace ZelosHR.Api.Entities.Leave;

/// <summary>Query params for <c>GET /leave/holidays/list</c> — matches frontend <c>PublicHolidayParams</c>.</summary>
public sealed record PublicHolidayListQuery
{
    [FromQuery(Name = PlatformQueryParams.Search)]
    public string? Search { get; init; }

    /// <summary>Calendar year to filter and project <c>occurrence_date</c> (e.g. <c>2026</c>). Omit for no year filter.</summary>
    [FromQuery(Name = PlatformQueryParams.Year)]
    public int? Year { get; init; }

    /// <summary>Country name from <c>GET /countries/list</c> (e.g. <c>Ghana</c>).</summary>
    [FromQuery(Name = PlatformQueryParams.Country)]
    public string? Country { get; init; }

    [FromQuery(Name = PlatformQueryParams.Page)]
    public int Page { get; init; } = 1;

    [FromQuery(Name = PlatformQueryParams.Size)]
    public int Size { get; init; } = 50;
}
