using Microsoft.EntityFrameworkCore;
using ZelosHR.Api.Entities.LifecycleEvents;
using ZelosHR.Api.Persistence.Entities;

namespace ZelosHR.Api.Persistence.Repositories;

public sealed class LifecycleEventRepository(ZelosHrDbContext db) : ILifecycleEventRepository
{
    private IQueryable<LifecycleEventEntity> Scoped(string tenantId, string orgId) =>
        db.LifecycleEvents.AsNoTracking()
            .Where(e => e.TenantId == tenantId && e.OrgId == orgId);

    public async Task<LifecycleEventSummaryDto> GetSummaryScopedAsync(
        string tenantId, string orgId, CancellationToken ct = default)
    {
        var query = Scoped(tenantId, orgId);
        return new LifecycleEventSummaryDto
        {
            OverdueCount = await query.CountAsync(e => e.Urgency == "Overdue", ct),
            CriticalCount = await query.CountAsync(e => e.Urgency == "Critical", ct),
            PendingActionCount = await query.CountAsync(
                e => e.Status == "Pending" || e.Status == "Awaiting Manager", ct),
            TotalEventsCount = await query.CountAsync(ct),
        };
    }

    public async Task<(IReadOnlyList<LifecycleEventListItemDto> Items, int Total)> ListScopedAsync(
        string tenantId,
        string orgId,
        string? search,
        string? eventType,
        string? urgency,
        string? department,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = ApplyFilters(Scoped(tenantId, orgId), search, eventType, urgency, department);
        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(e => e.DueDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => ToDto(e))
            .ToListAsync(ct);
        return (items, total);
    }

    public async Task<LifecycleEventListItemDto?> GetByIdScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default)
    {
        var entity = await Scoped(tenantId, orgId).FirstOrDefaultAsync(e => e.Id == id, ct);
        return entity is null ? null : ToDto(entity);
    }

    public async Task<Guid> CreateScopedAsync(
        string tenantId,
        string orgId,
        Guid employeeId,
        string employeeFullName,
        string eventType,
        string? departmentName,
        string? branchName,
        DateOnly dueDate,
        string status,
        string urgency,
        CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var entity = new LifecycleEventEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OrgId = orgId,
            EmployeeId = employeeId,
            EmployeeFullName = employeeFullName,
            EventType = eventType,
            DepartmentName = departmentName,
            BranchName = branchName,
            DueDate = dueDate,
            Status = status,
            Urgency = urgency,
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.LifecycleEvents.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity.Id;
    }

    public async Task<LifecycleEventListItemDto?> UpdateScopedAsync(
        Guid id,
        string tenantId,
        string orgId,
        string? eventType,
        DateOnly? dueDate,
        string? status,
        string? urgency,
        CancellationToken ct = default)
    {
        var entity = await db.LifecycleEvents.FirstOrDefaultAsync(
            e => e.Id == id && e.TenantId == tenantId && e.OrgId == orgId, ct);
        if (entity is null)
            return null;

        var changed = false;
        if (!string.IsNullOrWhiteSpace(eventType))
        {
            entity.EventType = eventType.Trim();
            changed = true;
        }
        if (dueDate.HasValue)
        {
            entity.DueDate = dueDate.Value;
            changed = true;
        }
        if (!string.IsNullOrWhiteSpace(status))
        {
            entity.Status = status.Trim();
            changed = true;
        }
        if (!string.IsNullOrWhiteSpace(urgency))
        {
            entity.Urgency = urgency.Trim();
            changed = true;
        }

        if (!changed)
            return null;

        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return ToDto(entity);
    }

    public async Task<bool> DeleteScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default)
    {
        var entity = await db.LifecycleEvents.FirstOrDefaultAsync(
            e => e.Id == id && e.TenantId == tenantId && e.OrgId == orgId, ct);
        if (entity is null)
            return false;

        db.LifecycleEvents.Remove(entity);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static IQueryable<LifecycleEventEntity> ApplyFilters(
        IQueryable<LifecycleEventEntity> query,
        string? search,
        string? eventType,
        string? urgency,
        string? department)
    {
        if (!string.IsNullOrWhiteSpace(search) && search.Trim().Length >= 3)
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(e =>
                EF.Functions.ILike(e.EmployeeFullName, pattern)
                || EF.Functions.ILike(e.EventType, pattern));
        }

        if (!string.IsNullOrWhiteSpace(eventType) && !eventType.Equals("all", StringComparison.OrdinalIgnoreCase))
            query = query.Where(e => e.EventType == eventType.Trim());

        if (!string.IsNullOrWhiteSpace(urgency) && !urgency.Equals("all", StringComparison.OrdinalIgnoreCase))
            query = query.Where(e => e.Urgency == urgency.Trim());

        if (!string.IsNullOrWhiteSpace(department) && !department.Equals("all", StringComparison.OrdinalIgnoreCase))
            query = query.Where(e =>
                e.DepartmentName != null && EF.Functions.ILike(e.DepartmentName, $"%{department.Trim()}%"));

        return query;
    }

    private static LifecycleEventListItemDto ToDto(LifecycleEventEntity e) => new()
    {
        LifecycleEventId = e.Id.ToString(),
        EmployeeId = e.EmployeeId.ToString(),
        EmployeeFullName = e.EmployeeFullName,
        EventType = e.EventType,
        DepartmentName = e.DepartmentName,
        BranchName = e.BranchName,
        DueDate = e.DueDate,
        Status = e.Status,
        Urgency = e.Urgency,
    };
}
