using Microsoft.EntityFrameworkCore;
using Npgsql;
using ZelosHR.Api.Entities.EmployeeIdFormat;
using ZelosHR.Api.Persistence.Entities;

namespace ZelosHR.Api.Persistence.Repositories;

public sealed class EmployeeIdFormatRepository(ZelosHrDbContext db) : IEmployeeIdFormatRepository
{
    public Task<EmployeeIdFormatEntity?> GetEntityAsync(
        string tenantId, string orgId, CancellationToken ct = default) =>
        db.EmployeeIdFormats.AsNoTracking()
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.OrgId == orgId, ct);

    public async Task<EmployeeIdFormatEntity> EnsureStubAsync(
        string tenantId,
        string orgId,
        string? actorUserId,
        CancellationToken ct = default)
    {
        var existing = await GetEntityAsync(tenantId, orgId, ct);
        if (existing is not null)
            return existing;

        var now = DateTimeOffset.UtcNow;
        var entity = new EmployeeIdFormatEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OrgId = orgId,
            Prefix = EmployeeIdFormatDefaults.Prefix,
            DigitCount = EmployeeIdFormatDefaults.DigitCount,
            StartingNumber = EmployeeIdFormatDefaults.StartingNumber,
            Separator = EmployeeIdFormatDefaults.Separator,
            AutoGenerate = EmployeeIdFormatDefaults.AutoGenerate,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = actorUserId,
            UpdatedBy = actorUserId,
        };

        db.EmployeeIdFormats.Add(entity);
        try
        {
            await db.SaveChangesAsync(ct);
            return entity;
        }
        catch (DbUpdateException ex) when (IsTenantOrgUnique(ex))
        {
            db.Entry(entity).State = EntityState.Detached;
            var raced = await GetEntityAsync(tenantId, orgId, ct);
            return raced ?? throw new InvalidOperationException(
                "Employee ID format stub insert raced but row is missing.");
        }
    }

    public async Task<EmployeeIdFormatEntity> CreateAsync(
        string tenantId,
        string orgId,
        CreateEmployeeIdFormatDto data,
        string? actorUserId,
        CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var entity = new EmployeeIdFormatEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OrgId = orgId,
            Prefix = data.Prefix!.Trim(),
            DigitCount = data.DigitCount!.Value,
            StartingNumber = data.StartingNumber!.Value,
            Separator = data.Separator!.Trim().ToLowerInvariant(),
            AutoGenerate = data.AutoGenerate!.Value,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = actorUserId,
            UpdatedBy = actorUserId,
        };
        db.EmployeeIdFormats.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task<EmployeeIdFormatEntity?> UpdateAsync(
        string tenantId,
        string orgId,
        UpdateEmployeeIdFormatDto data,
        string? actorUserId,
        CancellationToken ct = default)
    {
        var entity = await db.EmployeeIdFormats
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.OrgId == orgId, ct);
        if (entity is null)
            return null;

        entity.Prefix = data.Prefix!.Trim();
        entity.DigitCount = data.DigitCount!.Value;
        entity.StartingNumber = data.StartingNumber!.Value;
        entity.Separator = data.Separator!.Trim().ToLowerInvariant();
        entity.AutoGenerate = data.AutoGenerate!.Value;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        entity.UpdatedBy = actorUserId;
        await db.SaveChangesAsync(ct);
        return entity;
    }

    private static bool IsTenantOrgUnique(DbUpdateException ex) =>
        ex.InnerException is PostgresException pg
        && pg.SqlState == PostgresErrorCodes.UniqueViolation
        && pg.ConstraintName?.Contains("zhr_employee_id_format", StringComparison.OrdinalIgnoreCase) == true;
}
