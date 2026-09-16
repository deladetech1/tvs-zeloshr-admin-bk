using Microsoft.EntityFrameworkCore;
using ZelosHR.Api.Entities.Adjustments;
using ZelosHR.Api.Persistence.Entities;
using ZelosHR.Api.Persistence.Repositories;

namespace ZelosHR.Api.Persistence.Repositories;

public sealed class AdjustmentRepository(ZelosHrDbContext db) : IAdjustmentRepository
{
    public async Task<(IReadOnlyList<AdjustmentDto> Items, int Total)> ListScopedAsync(
        string tenantId,
        string orgId,
        Guid? employeeId,
        DateOnly? from,
        DateOnly? to,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = db.Adjustments.AsNoTracking()
            .Where(a => a.TenantId == tenantId && a.OrgId == orgId);
        if (employeeId.HasValue)
            query = query.Where(a => a.EmployeeId == employeeId.Value);
        if (from.HasValue)
            query = query.Where(a => a.AttendanceDate >= from.Value);
        if (to.HasValue)
            query = query.Where(a => a.AttendanceDate <= to.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
        return (items.Select(ToDto).ToList(), total);
    }

    public async Task<AdjustmentEntity> AddAsync(
        string tenantId,
        string orgId,
        Guid employeeId,
        Guid? attendanceId,
        DateOnly attendanceDate,
        string kind,
        string? punchType,
        TimeOnly? punchTime,
        Guid? originalPunchId,
        string reason,
        string? actorUserId,
        CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var entity = new AdjustmentEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OrgId = orgId,
            EmployeeId = employeeId,
            AttendanceId = attendanceId,
            AttendanceDate = attendanceDate,
            Kind = kind,
            PunchType = punchType,
            PunchTime = punchTime,
            OriginalPunchId = originalPunchId,
            Reason = reason,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedById = actorUserId,
            UpdatedById = actorUserId,
        };
        db.Adjustments.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public static AdjustmentDto ToDto(AdjustmentEntity a) => new()
    {
        AdjustmentId = a.Id.ToString(),
        EmployeeId = a.EmployeeId.ToString(),
        AttendanceId = a.AttendanceId?.ToString(),
        AttendanceDate = a.AttendanceDate,
        Kind = a.Kind,
        PunchType = a.PunchType,
        PunchTime = PersistenceMappingHelpers.FormatTime(a.PunchTime),
        OriginalPunchId = a.OriginalPunchId?.ToString(),
        Reason = a.Reason,
        CreatedAt = a.CreatedAt,
        UpdatedAt = a.UpdatedAt,
        CreatedById = a.CreatedById,
        UpdatedById = a.UpdatedById,
        CreatedBy = null,
        UpdatedBy = null,
    };
}
