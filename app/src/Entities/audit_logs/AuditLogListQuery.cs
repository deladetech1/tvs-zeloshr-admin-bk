using Microsoft.AspNetCore.Mvc;
using ZelosHR.Api.Shared.Constants;

namespace ZelosHR.Api.Entities.AuditLogs;

/// <summary>
/// Query filters for <c>GET /api/v1/audit-logs/list</c> —
/// matches frontend <c>EmployeeAuditLogParams</c> (<c>page</c>, <c>size</c>, <c>search</c>,
/// <c>action</c>, <c>severity</c>, <c>actor</c>, <c>start_date</c>, <c>end_date</c>).
/// </summary>
public sealed class AuditLogListQuery : AuditLogFilterQuery
{
    [FromQuery(Name = PlatformQueryParams.Page)]
    public int Page { get; init; } = 1;

    [FromQuery(Name = PlatformQueryParams.Size)]
    public int Size { get; init; } = 20;
}
