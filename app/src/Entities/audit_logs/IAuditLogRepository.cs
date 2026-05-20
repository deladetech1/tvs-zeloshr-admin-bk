namespace ZelosHR.Api.Entities.AuditLogs;

public interface IAuditLogRepository
{
    Task<AuditLogSummaryDto> GetSummaryScopedAsync(
        string tenantId, string orgId, CancellationToken ct = default);

    Task<(IReadOnlyList<AuditLogListRow> Items, int Total)> ListScopedAsync(
        string tenantId,
        string orgId,
        string? search,
        string? action,
        string? severity,
        string? actor,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<AuditLogListRow?> GetByIdScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default);
}

public sealed record AuditLogListRow(
    Guid Id,
    DateTimeOffset OccurredAt,
    string ActionTitle,
    string? ActionDescription,
    Guid? EmployeeId,
    string? EmployeeDisplayCode,
    string? EmployeeFullName,
    string ActorFullName,
    string Category,
    string Severity,
    bool IsFlagged);
