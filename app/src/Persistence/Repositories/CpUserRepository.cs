using Microsoft.EntityFrameworkCore;
using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Persistence.Entities;

namespace ZelosHR.Api.Persistence.Repositories;

public sealed class CpUserRepository(ZelosHrDbContext db) : ICpUserRepository
{
    public async Task<CpUserDto?> FindByEmailAsync(string email, string tenantId, CancellationToken ct = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        var row = await db.CpUsers.AsNoTracking()
            .Where(u => u.TenantId == tenantId && u.Email.ToLower() == normalized)
            .Select(u => new CpUserDto(u.Id, u.Fullname, u.Email, u.Contact, u.IsActive))
            .FirstOrDefaultAsync(ct);
        return row;
    }

    public async Task<CpUserDto?> GetByIdAsync(string userId, string tenantId, CancellationToken ct = default)
    {
        var row = await db.CpUsers.AsNoTracking()
            .Where(u => u.TenantId == tenantId && u.Id == userId)
            .Select(u => new CpUserDto(u.Id, u.Fullname, u.Email, u.Contact, u.IsActive))
            .FirstOrDefaultAsync(ct);
        return row;
    }

    public async Task<IReadOnlyList<CpUserDto>> SearchAsync(
        string query, string tenantId, int limit = 20, CancellationToken ct = default)
    {
        var q = $"%{query.Trim()}%";
        return await db.CpUsers.AsNoTracking()
            .Where(u => u.TenantId == tenantId
                        && (EF.Functions.ILike(u.Fullname, q) || EF.Functions.ILike(u.Email, q)))
            .OrderBy(u => u.Fullname)
            .Take(limit)
            .Select(u => new CpUserDto(u.Id, u.Fullname, u.Email, u.Contact, u.IsActive))
            .ToListAsync(ct);
    }

    public Task<bool> IsLinkedToEmployeeAsync(string userId, string tenantId, CancellationToken ct = default) =>
        db.Employees.AsNoTracking()
            .AnyAsync(e => e.TenantId == tenantId && e.UserId == userId && !e.IsDeleted, ct);
}
