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
        AuditLogFilterQuery filters,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = ApplyFilters(Scoped(tenantId, orgId), filters);
        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(a => a.OccurredAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => ToRow(a))
            .ToListAsync(ct);
        return (items, total);
    }

    public async Task<IReadOnlyList<AuditLogListRow>> ExportListScopedAsync(
        string tenantId,
        string orgId,
        AuditLogFilterQuery filters,
        CancellationToken ct = default)
    {
        return await ApplyFilters(Scoped(tenantId, orgId), filters)
            .OrderByDescending(a => a.OccurredAt)
            .Select(a => ToRow(a))
            .ToListAsync(ct);
    }

    public Task<int> PurgeOlderThanScopedAsync(
        string tenantId,
        string orgId,
        DateTimeOffset cutoffBefore,
        CancellationToken ct = default) =>
        db.AuditLogs
            .Where(a => a.TenantId == tenantId && a.OrgId == orgId && a.OccurredAt < cutoffBefore)
            .ExecuteDeleteAsync(ct);

    public async Task<AuditLogListRow?> GetByIdScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default)
    {
        var entity = await Scoped(tenantId, orgId).FirstOrDefaultAsync(a => a.Id == id, ct);
        return entity is null ? null : ToRow(entity);
    }

    public async Task AppendScopedAsync(
        string tenantId,
        string orgId,
        AuditLogAppendRow row,
        CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        db.AuditLogs.Add(new AuditLogEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OrgId = orgId,
            OccurredAt = row.OccurredAt,
            ActionTitle = row.ActionTitle.Trim(),
            ActionDescription = string.IsNullOrWhiteSpace(row.ActionDescription)
                ? null
                : row.ActionDescription.Trim(),
            EmployeeId = row.EmployeeId,
            EmployeeDisplayCode = row.EmployeeDisplayCode,
            EmployeeFullName = row.EmployeeFullName,
            ActorId = row.ActorId,
            ActorFullName = row.ActorFullName.Trim(),
            Category = row.Category.Trim(),
            Severity = row.Severity.Trim(),
            IsFlagged = row.IsFlagged,
            IsSensitiveRead = row.IsSensitiveRead,
            CreatedAt = now,
        });
        await db.SaveChangesAsync(ct);
    }

    private static IQueryable<AuditLogEntity> ApplyFilters(
        IQueryable<AuditLogEntity> query,
        AuditLogFilterQuery filters)
    {
        if (!string.IsNullOrWhiteSpace(filters.Search) && filters.Search.Trim().Length >= 3)
        {
            var pattern = $"%{filters.Search.Trim()}%";
            query = query.Where(a =>
                EF.Functions.ILike(a.ActorFullName, pattern)
                || (a.EmployeeFullName != null && EF.Functions.ILike(a.EmployeeFullName, pattern))
                || EF.Functions.ILike(a.ActionTitle, pattern));
        }

        if (!string.IsNullOrWhiteSpace(filters.Action) && !filters.Action.Equals("all", StringComparison.OrdinalIgnoreCase))
            query = query.Where(a => EF.Functions.ILike(a.ActionTitle, $"%{filters.Action.Trim()}%"));

        if (!string.IsNullOrWhiteSpace(filters.Severity) && !filters.Severity.Equals("all", StringComparison.OrdinalIgnoreCase))
            query = query.Where(a => a.Severity == filters.Severity.Trim());

        if (!string.IsNullOrWhiteSpace(filters.Actor) && !filters.Actor.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            var trimmed = filters.Actor.Trim();
            query = query.Where(a =>
                a.ActorId == trimmed
                || EF.Functions.ILike(a.ActorFullName, $"%{trimmed}%"));
        }

        if (filters.StartDate is not null)
        {
            var from = new DateTimeOffset(filters.StartDate.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            query = query.Where(a => a.OccurredAt >= from);
        }

        if (filters.EndDate is not null)
        {
            var toExclusive = new DateTimeOffset(filters.EndDate.Value.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            query = query.Where(a => a.OccurredAt < toExclusive);
        }

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
        a.ActorId,
        a.ActorFullName,
        a.Category,
        a.Severity,
        a.IsFlagged);
}
