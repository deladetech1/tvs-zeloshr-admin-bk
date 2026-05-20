using Microsoft.EntityFrameworkCore;
using ZelosHR.Api.Entities.Recruitment;
using ZelosHR.Api.Persistence.Entities;

namespace ZelosHR.Api.Persistence.Repositories;

public sealed class RecruitmentRepository(ZelosHrDbContext db) : IRecruitmentRepository
{
    private IQueryable<JobPostingEntity> Scoped(string tenantId, string orgId) =>
        db.JobPostings.AsNoTracking()
            .Where(j => j.TenantId == tenantId && j.OrgId == orgId);

    public async Task<RecruitmentSummaryDto> GetSummaryScopedAsync(
        string tenantId, string orgId, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var closingThreshold = today.AddDays(7);
        var query = Scoped(tenantId, orgId);

        return new RecruitmentSummaryDto
        {
            OpenPostings = await query.CountAsync(j => j.Status == "Open", ct),
            TotalApplicants = await query
                .Where(j => j.Status == "Open")
                .SumAsync(j => j.ApplicantsCount, ct),
            ClosingSoon = await query.CountAsync(
                j => j.Status == "Open"
                     && j.ClosingDate != null
                     && j.ClosingDate <= closingThreshold,
                ct),
            ClosedPostings = await query.CountAsync(j => j.Status == "Closed", ct),
        };
    }

    public async Task<(IReadOnlyList<JobPostingListItemDto> Items, int Total)> ListScopedAsync(
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
            .OrderByDescending(j => j.PostedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(j => ToDto(j))
            .ToListAsync(ct);
        return (items, total);
    }

    public async Task<JobPostingListItemDto?> GetByIdScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default)
    {
        var entity = await Scoped(tenantId, orgId).FirstOrDefaultAsync(j => j.Id == id, ct);
        return entity is null ? null : ToDto(entity);
    }

    public async Task<Guid> CreateScopedAsync(
        string tenantId,
        string orgId,
        string title,
        string? departmentName,
        string? branchName,
        string? employmentType,
        string status,
        DateOnly postedAt,
        DateOnly? closingDate,
        CancellationToken ct = default)
    {
        var entity = new JobPostingEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OrgId = orgId,
            Title = title,
            DepartmentName = departmentName,
            BranchName = branchName,
            EmploymentType = employmentType,
            Status = status,
            ApplicantsCount = 0,
            PostedAt = postedAt,
            ClosingDate = closingDate,
        };
        db.JobPostings.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity.Id;
    }

    public async Task<JobPostingListItemDto?> UpdateScopedAsync(
        Guid id,
        string tenantId,
        string orgId,
        string? title,
        string? departmentName,
        string? branchName,
        string? employmentType,
        DateOnly? postedAt,
        DateOnly? closingDate,
        string? status,
        int? applicantsCount,
        CancellationToken ct = default)
    {
        var entity = await db.JobPostings.FirstOrDefaultAsync(
            j => j.Id == id && j.TenantId == tenantId && j.OrgId == orgId, ct);
        if (entity is null)
            return null;

        var changed = false;
        if (!string.IsNullOrWhiteSpace(title))
        {
            entity.Title = title.Trim();
            changed = true;
        }
        if (departmentName is not null)
        {
            entity.DepartmentName = string.IsNullOrWhiteSpace(departmentName) ? null : departmentName.Trim();
            changed = true;
        }
        if (branchName is not null)
        {
            entity.BranchName = string.IsNullOrWhiteSpace(branchName) ? null : branchName.Trim();
            changed = true;
        }
        if (employmentType is not null)
        {
            entity.EmploymentType = string.IsNullOrWhiteSpace(employmentType) ? null : employmentType.Trim();
            changed = true;
        }
        if (postedAt.HasValue)
        {
            entity.PostedAt = postedAt.Value;
            changed = true;
        }
        if (closingDate.HasValue)
        {
            entity.ClosingDate = closingDate;
            changed = true;
        }
        if (!string.IsNullOrWhiteSpace(status))
        {
            entity.Status = status.Trim();
            changed = true;
        }
        if (applicantsCount.HasValue)
        {
            entity.ApplicantsCount = applicantsCount.Value;
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
        var entity = await db.JobPostings.FirstOrDefaultAsync(
            j => j.Id == id && j.TenantId == tenantId && j.OrgId == orgId, ct);
        if (entity is null)
            return false;
        db.JobPostings.Remove(entity);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static IQueryable<JobPostingEntity> ApplyFilters(
        IQueryable<JobPostingEntity> query,
        string? search,
        string? status)
    {
        if (!string.IsNullOrWhiteSpace(search) && search.Length >= 3)
            query = query.Where(j => EF.Functions.ILike(j.Title, $"%{search}%"));

        if (!string.IsNullOrWhiteSpace(status) && status != "all")
            query = query.Where(j => j.Status == status);

        return query;
    }

    private static JobPostingListItemDto ToDto(JobPostingEntity j) => new()
    {
        JobPostingId = j.Id.ToString(),
        Title = j.Title,
        DepartmentName = j.DepartmentName,
        BranchName = j.BranchName,
        EmploymentType = j.EmploymentType,
        Status = j.Status,
        ApplicantsCount = j.ApplicantsCount,
        PostedAt = j.PostedAt,
        ClosingDate = j.ClosingDate,
    };
}
