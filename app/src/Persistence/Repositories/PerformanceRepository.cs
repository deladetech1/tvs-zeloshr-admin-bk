using Microsoft.EntityFrameworkCore;
using ZelosHR.Api.Entities.Performance;
using ZelosHR.Api.Persistence.Entities;

namespace ZelosHR.Api.Persistence.Repositories;

public sealed class PerformanceRepository(ZelosHrDbContext db) : IPerformanceRepository
{
    private IQueryable<PerformanceReviewEntity> Scoped(string tenantId, string orgId) =>
        db.PerformanceReviews.AsNoTracking()
            .Where(r => r.TenantId == tenantId && r.OrgId == orgId);

    public async Task<PerformanceSummaryDto> GetSummaryScopedAsync(
        string tenantId, string orgId, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var query = Scoped(tenantId, orgId);

        return new PerformanceSummaryDto
        {
            Pending = await query.CountAsync(r => r.Status == "Pending", ct),
            InProgress = await query.CountAsync(r => r.Status == "In progress", ct),
            Completed = await query.CountAsync(r => r.Status == "Completed", ct),
            Overdue = await query.CountAsync(
                r => r.Status != "Completed" && r.DueDate < today, ct),
        };
    }

    public async Task<(IReadOnlyList<PerformanceReviewListItemDto> Items, int Total)> ListScopedAsync(
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
            .OrderBy(r => r.DueDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => ToDto(r))
            .ToListAsync(ct);
        return (items, total);
    }

    public async Task<PerformanceReviewListItemDto?> GetByIdScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default)
    {
        var entity = await Scoped(tenantId, orgId).FirstOrDefaultAsync(r => r.Id == id, ct);
        return entity is null ? null : ToDto(entity);
    }

    public async Task<Guid> CreateScopedAsync(
        string tenantId,
        string orgId,
        Guid employeeId,
        string employeeFullName,
        string reviewPeriod,
        string? reviewerName,
        string status,
        DateOnly dueDate,
        CancellationToken ct = default)
    {
        var entity = new PerformanceReviewEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OrgId = orgId,
            EmployeeId = employeeId,
            EmployeeFullName = employeeFullName,
            ReviewPeriod = reviewPeriod,
            ReviewerName = reviewerName,
            Status = status,
            DueDate = dueDate,
        };
        db.PerformanceReviews.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity.Id;
    }

    public async Task<PerformanceReviewListItemDto?> UpdateScopedAsync(
        Guid id,
        string tenantId,
        string orgId,
        string? reviewPeriod,
        DateOnly? dueDate,
        string? reviewerName,
        string? overallRating,
        string? status,
        CancellationToken ct = default)
    {
        var entity = await db.PerformanceReviews.FirstOrDefaultAsync(
            r => r.Id == id && r.TenantId == tenantId && r.OrgId == orgId, ct);
        if (entity is null)
            return null;

        var changed = false;
        if (!string.IsNullOrWhiteSpace(reviewPeriod))
        {
            entity.ReviewPeriod = reviewPeriod.Trim();
            changed = true;
        }
        if (dueDate.HasValue)
        {
            entity.DueDate = dueDate.Value;
            changed = true;
        }
        if (reviewerName is not null)
        {
            entity.ReviewerName = string.IsNullOrWhiteSpace(reviewerName) ? null : reviewerName.Trim();
            changed = true;
        }
        if (overallRating is not null)
        {
            entity.OverallRating = string.IsNullOrWhiteSpace(overallRating) ? null : overallRating.Trim();
            changed = true;
        }
        if (!string.IsNullOrWhiteSpace(status))
        {
            entity.Status = status.Trim();
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
        var entity = await db.PerformanceReviews.FirstOrDefaultAsync(
            r => r.Id == id && r.TenantId == tenantId && r.OrgId == orgId, ct);
        if (entity is null)
            return false;
        db.PerformanceReviews.Remove(entity);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static IQueryable<PerformanceReviewEntity> ApplyFilters(
        IQueryable<PerformanceReviewEntity> query,
        string? search,
        string? status)
    {
        if (!string.IsNullOrWhiteSpace(search) && search.Length >= 3)
            query = query.Where(r => EF.Functions.ILike(r.EmployeeFullName, $"%{search}%"));

        if (!string.IsNullOrWhiteSpace(status) && status != "all")
            query = query.Where(r => r.Status == status);

        return query;
    }

    private static PerformanceReviewListItemDto ToDto(PerformanceReviewEntity r) => new()
    {
        ReviewId = r.Id.ToString(),
        EmployeeId = r.EmployeeId.ToString(),
        EmployeeFullName = r.EmployeeFullName,
        ReviewPeriod = r.ReviewPeriod,
        ReviewerName = r.ReviewerName,
        OverallRating = r.OverallRating,
        Status = r.Status,
        DueDate = r.DueDate,
    };
}
