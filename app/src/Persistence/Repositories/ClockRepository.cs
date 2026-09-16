using Microsoft.EntityFrameworkCore;
using ZelosHR.Api.Entities.Clock;
using ZelosHR.Api.Persistence.Entities;

namespace ZelosHR.Api.Persistence.Repositories;

public sealed class ClockRepository(ZelosHrDbContext db) : IClockRepository
{
    public async Task<AttendanceRecordEntity> GetOrCreateDayAsync(
        string tenantId,
        string orgId,
        Guid employeeId,
        string employeeFullName,
        string? employeeCode,
        string? departmentName,
        string? branchName,
        DateOnly date,
        string? actorUserId,
        CancellationToken ct = default)
    {
        var existing = await db.AttendanceRecords.FirstOrDefaultAsync(
            a => a.TenantId == tenantId
                 && a.OrgId == orgId
                 && a.EmployeeId == employeeId
                 && a.AttendanceDate == date,
            ct);
        if (existing is not null)
            return existing;

        var now = DateTimeOffset.UtcNow;
        var created = new AttendanceRecordEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OrgId = orgId,
            EmployeeId = employeeId,
            EmployeeFullName = employeeFullName,
            EmployeeCode = employeeCode,
            DepartmentName = departmentName,
            BranchName = branchName,
            AttendanceDate = date,
            Status = "Present",
            CaptureSource = "web",
            CreatedAt = now,
            UpdatedAt = now,
            CreatedById = actorUserId,
            UpdatedById = actorUserId,
        };
        db.AttendanceRecords.Add(created);
        await db.SaveChangesAsync(ct);
        return created;
    }

    public async Task<AttendanceRecordEntity?> GetDayAsync(
        string tenantId, string orgId, Guid employeeId, DateOnly date, CancellationToken ct = default) =>
        await db.AttendanceRecords.AsNoTracking().FirstOrDefaultAsync(
            a => a.TenantId == tenantId
                 && a.OrgId == orgId
                 && a.EmployeeId == employeeId
                 && a.AttendanceDate == date,
            ct);

    public async Task<IReadOnlyList<PunchEntity>> ListPunchesForDayAsync(
        string tenantId, string orgId, Guid employeeId, Guid attendanceId, CancellationToken ct = default) =>
        await db.Punches.AsNoTracking()
            .Where(p => p.TenantId == tenantId
                        && p.OrgId == orgId
                        && p.EmployeeId == employeeId
                        && p.AttendanceId == attendanceId
                        && !p.IsSuperseded)
            .OrderBy(p => p.PunchedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<PunchEntity>> ListPunchesForEmployeeRangeAsync(
        string tenantId, string orgId, Guid employeeId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var fromTs = new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var toTs = new DateTimeOffset(to.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero);
        return await db.Punches.AsNoTracking()
            .Where(p => p.TenantId == tenantId
                        && p.OrgId == orgId
                        && p.EmployeeId == employeeId
                        && !p.IsSuperseded
                        && p.PunchedAt >= fromTs
                        && p.PunchedAt <= toTs)
            .OrderBy(p => p.PunchedAt)
            .ToListAsync(ct);
    }

    public async Task<PunchEntity> AddPunchAsync(
        string tenantId,
        string orgId,
        Guid employeeId,
        Guid attendanceId,
        string punchType,
        DateTimeOffset punchedAt,
        string source,
        Guid? deviceId,
        string? actorUserId,
        CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var punch = new PunchEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OrgId = orgId,
            EmployeeId = employeeId,
            AttendanceId = attendanceId,
            PunchType = punchType,
            PunchedAt = punchedAt,
            Source = source,
            DeviceId = deviceId,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedById = actorUserId,
            UpdatedById = actorUserId,
        };
        db.Punches.Add(punch);
        await db.SaveChangesAsync(ct);
        return punch;
    }

    public async Task RecalculateDayAsync(
        Guid attendanceId, string tenantId, string orgId, string? actorUserId, CancellationToken ct = default)
    {
        var day = await db.AttendanceRecords.FirstOrDefaultAsync(
            a => a.Id == attendanceId && a.TenantId == tenantId && a.OrgId == orgId, ct);
        if (day is null)
            return;

        var punches = await db.Punches
            .Where(p => p.AttendanceId == attendanceId && !p.IsSuperseded)
            .OrderBy(p => p.PunchedAt)
            .ToListAsync(ct);

        TimeOnly? firstIn = null;
        TimeOnly? lastOut = null;
        var minutes = 0d;
        DateTimeOffset? openIn = null;

        foreach (var punch in punches)
        {
            var local = TimeOnly.FromDateTime(punch.PunchedAt.UtcDateTime);
            if (string.Equals(punch.PunchType, "clock_in", StringComparison.OrdinalIgnoreCase))
            {
                firstIn ??= local;
                openIn = punch.PunchedAt;
            }
            else if (string.Equals(punch.PunchType, "clock_out", StringComparison.OrdinalIgnoreCase))
            {
                lastOut = local;
                if (openIn.HasValue)
                {
                    minutes += (punch.PunchedAt - openIn.Value).TotalMinutes;
                    openIn = null;
                }
            }
        }

        day.ClockIn = firstIn;
        day.ClockOut = lastOut;
        day.HoursWorked = punches.Count == 0 ? null : Math.Round((decimal)(minutes / 60d), 2);
        day.Status = firstIn is null ? "Absent" : "Present";
        day.UpdatedAt = DateTimeOffset.UtcNow;
        day.UpdatedById = actorUserId;
        await db.SaveChangesAsync(ct);
    }

    public async Task MarkPunchSupersededAsync(Guid punchId, string tenantId, string orgId, string? actorUserId, CancellationToken ct = default)
    {
        var punch = await db.Punches.FirstOrDefaultAsync(
            p => p.Id == punchId && p.TenantId == tenantId && p.OrgId == orgId, ct);
        if (punch is null)
            return;
        punch.IsSuperseded = true;
        punch.UpdatedAt = DateTimeOffset.UtcNow;
        punch.UpdatedById = actorUserId;
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<AttendanceRecordEntity>> ListDaysAsync(
        string tenantId, string orgId, Guid employeeId, DateOnly from, DateOnly to, CancellationToken ct = default) =>
        await db.AttendanceRecords.AsNoTracking()
            .Where(a => a.TenantId == tenantId
                        && a.OrgId == orgId
                        && a.EmployeeId == employeeId
                        && a.AttendanceDate >= from
                        && a.AttendanceDate <= to)
            .OrderBy(a => a.AttendanceDate)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<AttendanceRecordEntity>> ListDaysForOrgAsync(
        string tenantId, string orgId, DateOnly date, CancellationToken ct = default) =>
        await db.AttendanceRecords.AsNoTracking()
            .Where(a => a.TenantId == tenantId && a.OrgId == orgId && a.AttendanceDate == date)
            .ToListAsync(ct);

    public async Task MarkAdjustedAsync(Guid attendanceId, string tenantId, string orgId, string? actorUserId, CancellationToken ct = default)
    {
        var day = await db.AttendanceRecords.FirstOrDefaultAsync(
            a => a.Id == attendanceId && a.TenantId == tenantId && a.OrgId == orgId, ct);
        if (day is null)
            return;
        day.IsAdjusted = true;
        day.UpdatedAt = DateTimeOffset.UtcNow;
        day.UpdatedById = actorUserId;
        await db.SaveChangesAsync(ct);
    }
}
