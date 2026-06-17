namespace ZelosHR.Api.Entities.Leave;

/// <summary>Query params for <c>GET /leave/holidays/list</c> — matches frontend <c>PublicHolidayParams</c>.</summary>
public sealed record PublicHolidayListQuery
{
    /// <summary>Partial match on holiday name.</summary>
    public string? Search { get; init; }

    /// <summary>When <c>true</c>, scope to the current UTC calendar year and set <c>occurrence_date</c> on recurring rows.</summary>
    public bool Year { get; init; }

    /// <summary>Country name from <c>GET /countries/list</c> (e.g. <c>Ghana</c>).</summary>
    public string? Country { get; init; }

    public int Page { get; init; } = 1;
    public int Size { get; init; } = 50;
}
