using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Pagination;

namespace ZelosHR.Api.Entities.AuditLogs;

public class AuditLogsService
{
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
        string? search,
        string? action,
        string? severity,
        string? actor,
        int page,
        int size,
        string tenantId,
        string orgId,
        CancellationToken ct = default)
    {
        var paging = PagedQuery.From(page, size);
        var (rows, total) = await _auditLogs.ListScopedAsync(
            tenantId, orgId, search, action, severity, actor, paging.Page, paging.Size, ct);

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
        ActorFullName = r.ActorFullName,
        Category = r.Category,
        Severity = r.Severity,
        IsFlagged = r.IsFlagged,
    };
}
