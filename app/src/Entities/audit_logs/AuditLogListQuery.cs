namespace ZelosHR.Api.Entities.AuditLogs;

/// <summary>Query filters for <c>GET /api/v1/audit-logs/list</c>.</summary>
public sealed class AuditLogListQuery : AuditLogFilterQuery
{
    public int Page { get; init; } = 1;

    public int Size { get; init; } = 20;
}
