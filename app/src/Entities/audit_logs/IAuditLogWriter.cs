namespace ZelosHR.Api.Entities.AuditLogs;

public interface IAuditLogWriter
{
    Task RecordAsync(
        string tenantId,
        string orgId,
        AuditEvent auditEvent,
        CancellationToken ct = default);
}
