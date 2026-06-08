namespace ZelosHR.Api.Entities.AuditLogs;

public interface IAuditLogRepository
{
    Task<AuditLogSummaryDto> GetSummaryScopedAsync(
        string tenantId, string orgId, CancellationToken ct = default);

    Task<(IReadOnlyList<AuditLogListRow> Items, int Total)> ListScopedAsync(
        string tenantId,
        string orgId,
        AuditLogFilterQuery filters,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<IReadOnlyList<AuditLogListRow>> ExportListScopedAsync(
        string tenantId,
        string orgId,
        AuditLogFilterQuery filters,
        CancellationToken ct = default);

    Task<AuditLogListRow?> GetByIdScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default);

    Task<int> CountOlderThanScopedAsync(
        string tenantId,
        string orgId,
        DateTimeOffset cutoffBefore,
        CancellationToken ct = default);

    Task<int> PurgeOlderThanScopedAsync(
        string tenantId,
        string orgId,
        DateTimeOffset cutoffBefore,
        CancellationToken ct = default);

    Task AppendScopedAsync(
        string tenantId,
        string orgId,
        AuditLogAppendRow row,
        CancellationToken ct = default);
}

public sealed record AuditLogAppendRow(
    DateTimeOffset OccurredAt,
    string ActionTitle,
    string? ActionDescription,
    Guid? EmployeeId,
    string? EmployeeDisplayCode,
    string? EmployeeFullName,
    string? ActorId,
    string ActorFullName,
    string Category,
    string Severity,
    bool IsFlagged,
    bool IsSensitiveRead);

public sealed record AuditLogListRow(
    Guid Id,
    DateTimeOffset OccurredAt,
    string ActionTitle,
    string? ActionDescription,
    Guid? EmployeeId,
    string? EmployeeDisplayCode,
    string? EmployeeFullName,
    string? ActorId,
    string ActorFullName,
    string Category,
    string Severity,
    bool IsFlagged);
