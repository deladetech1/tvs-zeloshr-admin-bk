using Microsoft.EntityFrameworkCore;
using ZelosHR.Api.Entities.Disciplinary;
using ZelosHR.Api.Persistence.Entities;

namespace ZelosHR.Api.Persistence.Repositories;

public sealed class DisciplinaryRepository(ZelosHrDbContext db) : IDisciplinaryRepository
{
    private IQueryable<DisciplinaryCaseEntity> Scoped(string tenantId, string orgId) =>
        db.DisciplinaryCases.AsNoTracking()
            .Where(c => c.TenantId == tenantId && c.OrgId == orgId);

    public async Task<DisciplinarySummaryDto> GetSummaryScopedAsync(
        string tenantId, string orgId, CancellationToken ct = default)
    {
        var query = Scoped(tenantId, orgId);
        return new DisciplinarySummaryDto
        {
            OpenCases = await query.CountAsync(c => c.Status == "Open", ct),
            HighSeverity = await query.CountAsync(c => c.Severity == "High", ct),
            ClosedCases = await query.CountAsync(c => c.Status == "Closed", ct),
            TotalCases = await query.CountAsync(ct),
        };
    }

    public async Task<(IReadOnlyList<DisciplinaryCaseListItemDto> Items, int Total)> ListScopedAsync(
        string tenantId,
        string orgId,
        string? search,
        string? status,
        string? severity,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = ApplyFilters(Scoped(tenantId, orgId), search, status, severity);
        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(c => c.OpenedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => ToDto(c))
            .ToListAsync(ct);
        return (items, total);
    }

    public async Task<DisciplinaryCaseListItemDto?> GetByIdScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default)
    {
        var entity = await Scoped(tenantId, orgId).FirstOrDefaultAsync(c => c.Id == id, ct);
        return entity is null ? null : ToDto(entity);
    }

    public async Task<Guid> CreateScopedAsync(
        string tenantId,
        string orgId,
        Guid employeeId,
        string employeeFullName,
        string caseType,
        string severity,
        string status,
        DateOnly openedAt,
        string? description,
        CancellationToken ct = default)
    {
        var entity = new DisciplinaryCaseEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OrgId = orgId,
            EmployeeId = employeeId,
            EmployeeFullName = employeeFullName,
            CaseType = caseType,
            Severity = severity,
            Status = status,
            OpenedAt = openedAt,
            Description = description,
        };
        db.DisciplinaryCases.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity.Id;
    }

    public async Task<DisciplinaryCaseListItemDto?> UpdateScopedAsync(
        Guid id,
        string tenantId,
        string orgId,
        string? caseType,
        string? severity,
        DateOnly? openedAt,
        string? description,
        string? status,
        CancellationToken ct = default)
    {
        var entity = await db.DisciplinaryCases.FirstOrDefaultAsync(
            c => c.Id == id && c.TenantId == tenantId && c.OrgId == orgId, ct);
        if (entity is null)
            return null;

        var changed = false;
        if (!string.IsNullOrWhiteSpace(caseType))
        {
            entity.CaseType = caseType.Trim();
            changed = true;
        }
        if (!string.IsNullOrWhiteSpace(severity))
        {
            entity.Severity = severity.Trim();
            changed = true;
        }
        if (openedAt.HasValue)
        {
            entity.OpenedAt = openedAt.Value;
            changed = true;
        }
        if (description is not null)
        {
            entity.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
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
        var entity = await db.DisciplinaryCases.FirstOrDefaultAsync(
            c => c.Id == id && c.TenantId == tenantId && c.OrgId == orgId, ct);
        if (entity is null)
            return false;
        db.DisciplinaryCases.Remove(entity);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static IQueryable<DisciplinaryCaseEntity> ApplyFilters(
        IQueryable<DisciplinaryCaseEntity> query,
        string? search,
        string? status,
        string? severity)
    {
        if (!string.IsNullOrWhiteSpace(search) && search.Length >= 3)
            query = query.Where(c => EF.Functions.ILike(c.EmployeeFullName, $"%{search}%"));

        if (!string.IsNullOrWhiteSpace(status) && status != "all")
            query = query.Where(c => c.Status == status);

        if (!string.IsNullOrWhiteSpace(severity) && severity != "all")
            query = query.Where(c => c.Severity == severity);

        return query;
    }

    private static DisciplinaryCaseListItemDto ToDto(DisciplinaryCaseEntity c) => new()
    {
        CaseId = c.Id.ToString(),
        EmployeeId = c.EmployeeId.ToString(),
        EmployeeFullName = c.EmployeeFullName,
        CaseType = c.CaseType,
        Severity = c.Severity,
        Status = c.Status,
        OpenedAt = c.OpenedAt,
        Description = c.Description,
    };
}
