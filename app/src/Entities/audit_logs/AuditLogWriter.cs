using Microsoft.AspNetCore.Http;
using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Shared.Tenant;

namespace ZelosHR.Api.Entities.AuditLogs;

public sealed class AuditLogWriter(
    IAuditLogRepository auditLogs,
    ICpUserRepository cpUsers,
    IHttpContextAccessor httpContextAccessor) : IAuditLogWriter
{
    public async Task RecordAsync(
        string tenantId,
        string orgId,
        AuditEvent auditEvent,
        CancellationToken ct = default)
    {
        var (actorId, actorName) = await ResolveActorAsync(tenantId, ct);

        await auditLogs.AppendScopedAsync(
            tenantId,
            orgId,
            new AuditLogAppendRow(
                DateTimeOffset.UtcNow,
                auditEvent.ActionTitle,
                auditEvent.ActionDescription,
                auditEvent.EmployeeId,
                auditEvent.EmployeeDisplayCode,
                auditEvent.EmployeeFullName,
                actorId,
                actorName,
                auditEvent.Category,
                auditEvent.Severity,
                auditEvent.IsFlagged,
                auditEvent.IsSensitiveRead),
            ct);
    }

    private async Task<(string? ActorId, string ActorFullName)> ResolveActorAsync(
        string tenantId,
        CancellationToken ct)
    {
        var actorId = httpContextAccessor.HttpContext?.Items[TrovesuiteHttpContextKeys.UserId] as string;
        if (string.IsNullOrWhiteSpace(actorId))
            return (null, "System");

        var user = await cpUsers.GetByIdAsync(actorId, tenantId, ct);
        if (user is not null && !string.IsNullOrWhiteSpace(user.FullName))
            return (actorId, user.FullName);

        return (actorId, actorId);
    }
}
