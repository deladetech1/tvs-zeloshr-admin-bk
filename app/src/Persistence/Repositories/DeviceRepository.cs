using Microsoft.EntityFrameworkCore;
using ZelosHR.Api.Entities.Devices;
using ZelosHR.Api.Persistence.Entities;

namespace ZelosHR.Api.Persistence.Repositories;

public sealed class DeviceRepository(ZelosHrDbContext db) : IDeviceRepository
{
    public async Task<(IReadOnlyList<DeviceDto> Items, int Total)> ListScopedAsync(
        string tenantId, string orgId, string? search, int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.Devices.AsNoTracking()
            .Where(d => d.TenantId == tenantId && d.OrgId == orgId);
        if (!string.IsNullOrWhiteSpace(search) && search.Trim().Length >= 2)
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(d =>
                EF.Functions.ILike(d.Name, pattern)
                || (d.Serial != null && EF.Functions.ILike(d.Serial, pattern))
                || (d.Location != null && EF.Functions.ILike(d.Location, pattern)));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(d => d.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
        return (items.Select(ToDto).ToList(), total);
    }

    public async Task<DeviceDto?> GetByIdScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default)
    {
        var entity = await db.Devices.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id && d.TenantId == tenantId && d.OrgId == orgId, ct);
        return entity is null ? null : ToDto(entity);
    }

    public async Task<Guid> CreateScopedAsync(
        string tenantId,
        string orgId,
        string name,
        string? vendor,
        string? model,
        string? serial,
        string? location,
        string? actorUserId,
        CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var entity = new DeviceEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OrgId = orgId,
            Name = name,
            Vendor = vendor,
            Model = model,
            Serial = serial,
            Location = location,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedById = actorUserId,
            UpdatedById = actorUserId,
        };
        db.Devices.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity.Id;
    }

    public async Task<bool> DeleteScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default)
    {
        var entity = await db.Devices.FirstOrDefaultAsync(
            d => d.Id == id && d.TenantId == tenantId && d.OrgId == orgId, ct);
        if (entity is null)
            return false;
        db.Devices.Remove(entity);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static DeviceDto ToDto(DeviceEntity d) => new()
    {
        DeviceId = d.Id.ToString(),
        Name = d.Name,
        Vendor = d.Vendor,
        Model = d.Model,
        Serial = d.Serial,
        Location = d.Location,
        CreatedAt = d.CreatedAt,
        UpdatedAt = d.UpdatedAt,
        CreatedById = d.CreatedById,
        UpdatedById = d.UpdatedById,
        CreatedBy = null,
        UpdatedBy = null,
    };
}
