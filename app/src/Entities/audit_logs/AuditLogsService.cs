using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Pagination;

namespace ZelosHR.Api.Entities.AuditLogs;

public class AuditLogsService
{
    private const int RetentionMonths = 3;

    private readonly IAuditLogRepository _auditLogs;

    public AuditLogsService(IAuditLogRepository auditLogs) => _auditLogs = auditLogs;

    public async Task<Respons<AuditLogSummaryDto>> GetSummaryAsync(
        string tenantId,
        string orgId,
        CancellationToken ct = default)
    {
        var summary = await _auditLogs.GetSummaryScopedAsync(tenantId, orgId, ct);
        return Respons<AuditLogSummaryDto>.Ok(summary);
    }

    public async Task<Respons<AuditLogListDto>> ListAsync(
        AuditLogListQuery query,
        string tenantId,
        string orgId,
        CancellationToken ct = default)
    {
        if (query.StartDate is not null && query.EndDate is not null && query.StartDate > query.EndDate)
        {
            return Respons<AuditLogListDto>.ValidationError(new Dictionary<string, string>
            {
                ["start_date"] = "start_date must be on or before end_date.",
            });
        }

        var paging = PagedQuery.From(query.Page, query.Size);
        var (rows, total) = await _auditLogs.ListScopedAsync(
            tenantId, orgId, query, paging.Page, paging.Size, ct);

        var items = rows.Select(MapRow).ToList();
        var summary = await _auditLogs.GetSummaryScopedAsync(tenantId, orgId, ct);

        return Respons<AuditLogListDto>.Ok(
            new AuditLogListDto { Summary = summary, Items = items },
            pagination: new PaginationMeta
            {
                Page = paging.Page,
                Size = paging.Size,
                Total = total,
                HasNext = paging.Offset + items.Count < total,
            });
    }

    public async Task<Respons<byte[]>> ExportCsvAsync(
        AuditLogExportQuery query,
        string tenantId,
        string orgId,
        CancellationToken ct = default)
    {
        if (query.StartDate is not null && query.EndDate is not null && query.StartDate > query.EndDate)
        {
            return Respons<byte[]>.ValidationError(new Dictionary<string, string>
            {
                ["start_date"] = "start_date must be on or before end_date.",
            });
        }

        var rows = await _auditLogs.ExportListScopedAsync(tenantId, orgId, query, ct);
        return Respons<byte[]>.Ok(AuditLogCsvExport.Build(rows));
    }

    public async Task<Respons<AuditLogPurgeResultDto>> PurgeOldAsync(
        string tenantId,
        string orgId,
        CancellationToken ct = default)
    {
        var cutoff = DateTimeOffset.UtcNow.AddMonths(-RetentionMonths);
        var deleted = await _auditLogs.PurgeOlderThanScopedAsync(tenantId, orgId, cutoff, ct);
        return Respons<AuditLogPurgeResultDto>.Ok(new AuditLogPurgeResultDto
        {
            DeletedCount = deleted,
            CutoffBefore = cutoff,
        });
    }

    public async Task<Respons<AuditLogListItemDto>> GetByIdAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default)
    {
        var row = await _auditLogs.GetByIdScopedAsync(id, tenantId, orgId, ct);
        return row is null
            ? Respons<AuditLogListItemDto>.Fail("Audit log entry not found.", statusCode: 404)
            : Respons<AuditLogListItemDto>.Ok(MapRow(row));
    }

    private static AuditLogListItemDto MapRow(AuditLogListRow r) => new()
    {
        AuditLogId = r.Id.ToString(),
        OccurredAt = r.OccurredAt,
        ActionTitle = r.ActionTitle,
        ActionDescription = r.ActionDescription,
        Employee = string.IsNullOrWhiteSpace(r.EmployeeFullName)
            ? null
            : new AuditLogEmployeeRefDto
            {
                EmployeeId = r.EmployeeId?.ToString(),
                EmployeeDisplayCode = r.EmployeeDisplayCode,
                EmployeeFullName = r.EmployeeFullName,
            },
        ActorId = r.ActorId,
        ActorFullName = r.ActorFullName,
        Category = r.Category,
        Severity = r.Severity,
        IsFlagged = r.IsFlagged,
    };
}
