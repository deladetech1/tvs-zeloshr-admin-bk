using Microsoft.EntityFrameworkCore;
using ZelosHR.Api.Entities.AuditLogs;
using ZelosHR.Api.Persistence.Entities;

namespace ZelosHR.Api.Persistence.Repositories;

public sealed class AuditLogRepository(ZelosHrDbContext db) : IAuditLogRepository
{
    private IQueryable<AuditLogEntity> Scoped(string tenantId, string orgId) =>
        db.AuditLogs.AsNoTracking()
            .Where(a => a.TenantId == tenantId && a.OrgId == orgId);

    public async Task<AuditLogSummaryDto> GetSummaryScopedAsync(
        string tenantId, string orgId, CancellationToken ct = default)
    {
        var query = Scoped(tenantId, orgId);
        return new AuditLogSummaryDto
        {
            TotalEntries = await query.CountAsync(ct),
            CriticalCount = await query.CountAsync(a => a.Severity == "High", ct),
            FlaggedCount = await query.CountAsync(a => a.IsFlagged, ct),
            SensitiveReadsCount = await query.CountAsync(a => a.IsSensitiveRead, ct),
            UniqueActorsCount = await query
                .Where(a => a.ActorId != null)
                .Select(a => a.ActorId)
                .Distinct()
                .CountAsync(ct),
        };
    }

    public async Task<(IReadOnlyList<AuditLogListRow> Items, int Total)> ListScopedAsync(
        string tenantId,
        string orgId,
        string? search,
        string? action,
        string? severity,
        string? actor,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = ApplyFilters(Scoped(tenantId, orgId), search, action, severity, actor);
        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(a => a.OccurredAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => ToRow(a))
            .ToListAsync(ct);
        return (items, total);
    }

    public async Task<AuditLogListRow?> GetByIdScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default)
    {
        var entity = await Scoped(tenantId, orgId).FirstOrDefaultAsync(a => a.Id == id, ct);
        return entity is null ? null : ToRow(entity);
    }

    private static IQueryable<AuditLogEntity> ApplyFilters(
        IQueryable<AuditLogEntity> query,
        string? search,
        string? action,
        string? severity,
        string? actor)
    {
        if (!string.IsNullOrWhiteSpace(search) && search.Trim().Length >= 3)
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(a =>
                EF.Functions.ILike(a.ActorFullName, pattern)
                || (a.EmployeeFullName != null && EF.Functions.ILike(a.EmployeeFullName, pattern))
                || EF.Functions.ILike(a.ActionTitle, pattern));
        }

        if (!string.IsNullOrWhiteSpace(action) && !action.Equals("all", StringComparison.OrdinalIgnoreCase))
            query = query.Where(a => EF.Functions.ILike(a.ActionTitle, $"%{action.Trim()}%"));

        if (!string.IsNullOrWhiteSpace(severity) && !severity.Equals("all", StringComparison.OrdinalIgnoreCase))
            query = query.Where(a => a.Severity == severity.Trim());

        if (!string.IsNullOrWhiteSpace(actor) && !actor.Equals("all", StringComparison.OrdinalIgnoreCase))
            query = query.Where(a => EF.Functions.ILike(a.ActorFullName, $"%{actor.Trim()}%"));

        return query;
    }

    private static AuditLogListRow ToRow(AuditLogEntity a) => new(
        a.Id,
        a.OccurredAt,
        a.ActionTitle,
        a.ActionDescription,
        a.EmployeeId,
        a.EmployeeDisplayCode,
        a.EmployeeFullName,
        a.ActorFullName,
        a.Category,
        a.Severity,
        a.IsFlagged);
}
