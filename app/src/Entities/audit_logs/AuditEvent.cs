namespace ZelosHR.Api.Entities.AuditLogs;

/// <summary>Immutable audit event appended by <see cref="IAuditLogWriter"/>.</summary>
public sealed record AuditEvent(
    string ActionTitle,
    string? ActionDescription,
    string Category,
    string Severity,
    Guid? EmployeeId = null,
    string? EmployeeDisplayCode = null,
    string? EmployeeFullName = null,
    bool IsFlagged = false,
    bool IsSensitiveRead = false);
