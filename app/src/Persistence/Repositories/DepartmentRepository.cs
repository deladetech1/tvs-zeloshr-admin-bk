using Microsoft.EntityFrameworkCore;
using ZelosHR.Api.Entities.Departments;
using ZelosHR.Api.Entities.OrgStructure;
using ZelosHR.Api.Persistence.Entities;

namespace ZelosHR.Api.Persistence.Repositories;

public sealed class DepartmentRepository(ZelosHrDbContext db) : IDepartmentRepository
{
    private IQueryable<DepartmentEntity> Scoped(string tenantId, string orgId) =>
        db.Departments.AsNoTracking()
            .Where(d => d.TenantId == tenantId && d.OrgId == orgId);

    public async Task<OrganisationSummaryDto> GetSummaryScopedAsync(
        string tenantId, string orgId, CancellationToken ct = default)
    {
        var departments = Scoped(tenantId, orgId);
        var branches = db.Branches.AsNoTracking()
            .Where(b => b.TenantId == tenantId && b.OrgId == orgId);

        return new OrganisationSummaryDto
        {
            DepartmentCount = await departments.CountAsync(d => !d.IsArchived, ct),
            BranchCount = await branches.CountAsync(b => !b.IsArchived, ct),
            ArchivedCount = await departments.CountAsync(d => d.IsArchived, ct),
        };
    }

    public async Task<(IReadOnlyList<DepartmentListRow> Items, int Total)> ListScopedAsync(
        string tenantId,
        string orgId,
        string? search,
        string sortBy,
        string sortOrder,
        bool includeArchived,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = Scoped(tenantId, orgId);
        if (!includeArchived)
            query = query.Where(d => !d.IsArchived);

        if (!string.IsNullOrWhiteSpace(search) && search.Trim().Length >= 3)
            query = query.Where(d => EF.Functions.ILike(d.Name, $"%{search.Trim()}%"));

        var total = await query.CountAsync(ct);

        var projected = query.Select(d => new
        {
            d.Id,
            d.Name,
            d.Description,
            d.ParentDepartmentId,
            ParentDepartmentName = d.ParentDepartment != null ? d.ParentDepartment.Name : null,
            d.IsArchived,
            HeadId = d.HeadOfDepartmentId,
            HeadUserId = d.HeadOfDepartment != null ? d.HeadOfDepartment.UserId : null,
            HeadFullName = d.HeadOfDepartment != null ? d.HeadOfDepartment.FullName : null,
            HeadFirstName = d.HeadOfDepartment != null ? d.HeadOfDepartment.FirstName : null,
            HeadLastName = d.HeadOfDepartment != null ? d.HeadOfDepartment.LastName : null,
            HeadJobTitle = d.HeadOfDepartment != null ? d.HeadOfDepartment.JobTitle : null,
            HeadProfilePhotoUrl = d.HeadOfDepartment != null ? d.HeadOfDepartment.ProfilePhotoUrl : null,
            d.HeadcountCapacity,
            d.CreatedAt,
            d.UpdatedAt,
            d.CreatedBy,
            d.UpdatedBy,
            EmployeeCount = db.Employees.Count(e =>
                e.DepartmentId == d.Id
                && e.TenantId == tenantId
                && e.OrgId == orgId
                && !e.IsDeleted),
        });

        var byEmployeeCount = sortBy.Equals("employeeCount", StringComparison.OrdinalIgnoreCase)
            || sortBy.Equals("employeecount", StringComparison.OrdinalIgnoreCase);
        var desc = sortOrder.Equals("desc", StringComparison.OrdinalIgnoreCase);

        projected = byEmployeeCount
            ? (desc ? projected.OrderByDescending(x => x.EmployeeCount) : projected.OrderBy(x => x.EmployeeCount))
            : (desc ? projected.OrderByDescending(x => x.Name) : projected.OrderBy(x => x.Name));

        var pageItems = await projected
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var rows = pageItems.Select(x => new DepartmentListRow(
            x.Id,
            x.Name,
            x.Description,
            x.ParentDepartmentId,
            x.ParentDepartmentName,
            x.IsArchived,
            x.HeadId,
            x.HeadUserId,
            x.HeadFullName,
            x.HeadFirstName,
            x.HeadLastName,
            x.HeadJobTitle,
            x.HeadProfilePhotoUrl,
            x.EmployeeCount,
            x.HeadcountCapacity,
            x.CreatedAt,
            x.UpdatedAt,
            x.CreatedBy,
            x.UpdatedBy)).ToList();

        return (rows, total);
    }

    public async Task<Guid> CreateScopedAsync(
        string tenantId,
        string orgId,
        string name,
        Guid? parentDepartmentId,
        Guid? headOfDepartmentId,
        string? description,
        int? headcountCapacity,
        string? actedBy,
        CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var entity = new DepartmentEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OrgId = orgId,
            Name = name.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            ParentDepartmentId = parentDepartmentId,
            HeadOfDepartmentId = headOfDepartmentId,
            HeadcountCapacity = headcountCapacity,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = actedBy,
            UpdatedBy = actedBy,
        };
        db.Departments.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity.Id;
    }

    public async Task<DepartmentListRow?> GetActiveScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default)
    {
        var entity = await db.Departments.AsNoTracking()
            .Include(d => d.HeadOfDepartment)
            .FirstOrDefaultAsync(
                d => d.Id == id && d.TenantId == tenantId && d.OrgId == orgId && !d.IsArchived, ct);
        if (entity is null)
            return null;

        var employeeCount = await db.Employees.CountAsync(
            e => e.DepartmentId == entity.Id
                 && e.TenantId == tenantId
                 && e.OrgId == orgId
                 && !e.IsDeleted,
            ct);

        return new DepartmentListRow(
            entity.Id,
            entity.Name,
            entity.Description,
            entity.ParentDepartmentId,
            null,
            entity.IsArchived,
            entity.HeadOfDepartmentId,
            entity.HeadOfDepartment?.UserId,
            entity.HeadOfDepartment?.FullName,
            entity.HeadOfDepartment?.FirstName,
            entity.HeadOfDepartment?.LastName,
            entity.HeadOfDepartment?.JobTitle,
            entity.HeadOfDepartment?.ProfilePhotoUrl,
            employeeCount,
            entity.HeadcountCapacity,
            entity.CreatedAt,
            entity.UpdatedAt,
            entity.CreatedBy,
            entity.UpdatedBy);
    }

    public async Task<bool> ExistsActiveScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default) =>
        await db.Departments.AsNoTracking()
            .AnyAsync(
                d => d.Id == id && d.TenantId == tenantId && d.OrgId == orgId && !d.IsArchived,
                ct);

    public async Task<string?> UpdateScopedAsync(
        Guid id,
        string tenantId,
        string orgId,
        string? name,
        Guid? parentDepartmentId,
        Guid? headOfDepartmentId,
        bool updateHeadOfDepartment,
        string? description,
        bool updateDescription,
        int? headcountCapacity,
        bool updateHeadcountCapacity,
        string? actedBy,
        CancellationToken ct = default)
    {
        var entity = await db.Departments.FirstOrDefaultAsync(
            d => d.Id == id && d.TenantId == tenantId && d.OrgId == orgId && !d.IsArchived, ct);
        if (entity is null)
            return null;

        var changed = false;
        if (!string.IsNullOrWhiteSpace(name))
        {
            entity.Name = name.Trim();
            changed = true;
        }
        if (parentDepartmentId.HasValue)
        {
            entity.ParentDepartmentId = parentDepartmentId;
            changed = true;
        }
        if (updateHeadOfDepartment)
        {
            entity.HeadOfDepartmentId = headOfDepartmentId;
            changed = true;
        }
        if (updateDescription)
        {
            entity.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
            changed = true;
        }
        if (updateHeadcountCapacity)
        {
            entity.HeadcountCapacity = headcountCapacity;
            changed = true;
        }

        if (!changed)
            return string.Empty;

        entity.UpdatedAt = DateTimeOffset.UtcNow;
        entity.UpdatedBy = actedBy;
        await db.SaveChangesAsync(ct);
        return entity.Name;
    }

    public async Task<OrgStructureDeleteResult> DeleteScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default)
    {
        var entity = await db.Departments.FirstOrDefaultAsync(
            d => d.Id == id && d.TenantId == tenantId && d.OrgId == orgId, ct);
        if (entity is null)
            return OrgStructureDeleteResult.NotFound;

        var hasEmployees = await db.Employees.AnyAsync(
            e => e.DepartmentId == id
                 && e.TenantId == tenantId
                 && e.OrgId == orgId
                 && !e.IsDeleted,
            ct);
        if (hasEmployees)
            return OrgStructureDeleteResult.InUseByEmployees;

        var hasChildren = await db.Departments.AnyAsync(
            d => d.ParentDepartmentId == id && d.TenantId == tenantId && d.OrgId == orgId,
            ct);
        if (hasChildren)
            return OrgStructureDeleteResult.HasChildDepartments;

        db.Departments.Remove(entity);
        await db.SaveChangesAsync(ct);
        return OrgStructureDeleteResult.Deleted;
    }
}
