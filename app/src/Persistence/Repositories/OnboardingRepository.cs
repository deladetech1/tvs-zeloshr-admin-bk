using Microsoft.EntityFrameworkCore;
using ZelosHR.Api.Entities.Onboarding;
using ZelosHR.Api.Persistence.Entities;

namespace ZelosHR.Api.Persistence.Repositories;

public sealed class OnboardingRepository(ZelosHrDbContext db) : IOnboardingRepository
{
    private IQueryable<OnboardingTaskEntity> Scoped(string tenantId, string orgId) =>
        db.OnboardingTasks.AsNoTracking()
            .Where(t => t.TenantId == tenantId && t.OrgId == orgId);

    public async Task<OnboardingSummaryDto> GetSummaryScopedAsync(
        string tenantId, string orgId, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var query = Scoped(tenantId, orgId);

        return new OnboardingSummaryDto
        {
            PendingTasks = await query.CountAsync(t => t.Status == "Pending", ct),
            InProgress = await query.CountAsync(t => t.Status == "In progress", ct),
            Completed = await query.CountAsync(t => t.Status == "Completed", ct),
            Overdue = await query.CountAsync(
                t => t.Status != "Completed" && t.DueDate < today, ct),
        };
    }

    public async Task<(IReadOnlyList<OnboardingTaskListItemDto> Items, int Total)> ListScopedAsync(
        string tenantId,
        string orgId,
        string? search,
        string? status,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = ApplyFilters(Scoped(tenantId, orgId), search, status);
        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(t => t.DueDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => ToDto(t))
            .ToListAsync(ct);
        return (items, total);
    }

    public async Task<OnboardingTaskListItemDto?> GetByIdScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default)
    {
        var entity = await Scoped(tenantId, orgId).FirstOrDefaultAsync(t => t.Id == id, ct);
        return entity is null ? null : ToDto(entity);
    }

    public async Task<Guid> CreateScopedAsync(
        string tenantId,
        string orgId,
        Guid employeeId,
        string employeeFullName,
        string taskName,
        string category,
        DateOnly dueDate,
        string status,
        string? assignedTo,
        CancellationToken ct = default)
    {
        var entity = new OnboardingTaskEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OrgId = orgId,
            EmployeeId = employeeId,
            EmployeeFullName = employeeFullName,
            TaskName = taskName,
            Category = category,
            DueDate = dueDate,
            Status = status,
            AssignedTo = assignedTo,
        };
        db.OnboardingTasks.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity.Id;
    }

    public async Task<OnboardingTaskListItemDto?> UpdateScopedAsync(
        Guid id,
        string tenantId,
        string orgId,
        string? taskName,
        string? category,
        DateOnly? dueDate,
        string? status,
        string? assignedTo,
        CancellationToken ct = default)
    {
        var entity = await db.OnboardingTasks.FirstOrDefaultAsync(
            t => t.Id == id && t.TenantId == tenantId && t.OrgId == orgId, ct);
        if (entity is null)
            return null;

        var changed = false;
        if (!string.IsNullOrWhiteSpace(taskName))
        {
            entity.TaskName = taskName.Trim();
            changed = true;
        }
        if (!string.IsNullOrWhiteSpace(category))
        {
            entity.Category = category.Trim();
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
        if (assignedTo is not null)
        {
            entity.AssignedTo = string.IsNullOrWhiteSpace(assignedTo) ? null : assignedTo.Trim();
            changed = true;
        }

        if (!changed)
            return null;

        await db.SaveChangesAsync(ct);
        return ToDto(entity);
    }

    public async Task<bool> DeleteScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default)
    {
        var entity = await db.OnboardingTasks.FirstOrDefaultAsync(
            t => t.Id == id && t.TenantId == tenantId && t.OrgId == orgId, ct);
        if (entity is null)
            return false;
        db.OnboardingTasks.Remove(entity);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static IQueryable<OnboardingTaskEntity> ApplyFilters(
        IQueryable<OnboardingTaskEntity> query,
        string? search,
        string? status)
    {
        if (!string.IsNullOrWhiteSpace(search) && search.Length >= 3)
        {
            var pattern = $"%{search}%";
            query = query.Where(t =>
                EF.Functions.ILike(t.EmployeeFullName, pattern)
                || EF.Functions.ILike(t.TaskName, pattern));
        }

        if (!string.IsNullOrWhiteSpace(status) && status != "all")
            query = query.Where(t => t.Status == status);

        return query;
    }

    private static OnboardingTaskListItemDto ToDto(OnboardingTaskEntity t) => new()
    {
        TaskId = t.Id.ToString(),
        EmployeeId = t.EmployeeId.ToString(),
        EmployeeFullName = t.EmployeeFullName,
        TaskName = t.TaskName,
        Category = t.Category,
        DueDate = t.DueDate,
        Status = t.Status,
        AssignedTo = t.AssignedTo,
    };
}
