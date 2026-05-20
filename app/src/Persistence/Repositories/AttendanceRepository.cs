using Microsoft.EntityFrameworkCore;
using ZelosHR.Api.Entities.Attendance;
using ZelosHR.Api.Persistence.Entities;

namespace ZelosHR.Api.Persistence.Repositories;

public sealed class AttendanceRepository(ZelosHrDbContext db) : IAttendanceRepository
{
    private IQueryable<AttendanceRecordEntity> Scoped(string tenantId, string orgId) =>
        db.AttendanceRecords.AsNoTracking()
            .Where(a => a.TenantId == tenantId && a.OrgId == orgId);

    public async Task<AttendanceSummaryDto> GetSummaryScopedAsync(
        string tenantId, string orgId, DateOnly date, CancellationToken ct = default)
    {
        var query = Scoped(tenantId, orgId).Where(a => a.AttendanceDate == date);
        return new AttendanceSummaryDto
        {
            TotalScheduled = await query.CountAsync(ct),
            Present = await query.CountAsync(a => a.Status == "Present", ct),
            Late = await query.CountAsync(a => a.Status == "Late", ct),
            Absent = await query.CountAsync(a => a.Status == "Absent", ct),
            OnLeave = await query.CountAsync(a => a.Status == "On Leave", ct),
        };
    }

    public async Task<(IReadOnlyList<AttendanceListItemDto> Items, int Total)> ListScopedAsync(
        string tenantId,
        string orgId,
        DateOnly date,
        string? search,
        string? status,
        string? branch,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = ApplyFilters(Scoped(tenantId, orgId).Where(a => a.AttendanceDate == date), search, status, branch);
        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(a => a.EmployeeFullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => ToDto(a))
            .ToListAsync(ct);
        return (items, total);
    }

    public async Task<AttendanceListItemDto?> GetByIdScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default)
    {
        var entity = await Scoped(tenantId, orgId).FirstOrDefaultAsync(a => a.Id == id, ct);
        return entity is null ? null : ToDto(entity);
    }

    public async Task<Guid> CreateScopedAsync(
        string tenantId,
        string orgId,
        Guid employeeId,
        string employeeFullName,
        string? employeeCode,
        string? departmentName,
        string? branchName,
        DateOnly attendanceDate,
        string? clockIn,
        string? clockOut,
        string status,
        decimal? hoursWorked,
        CancellationToken ct = default)
    {
        var entity = new AttendanceRecordEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OrgId = orgId,
            EmployeeId = employeeId,
            EmployeeFullName = employeeFullName,
            EmployeeCode = employeeCode,
            DepartmentName = departmentName,
            BranchName = branchName,
            AttendanceDate = attendanceDate,
            ClockIn = PersistenceMappingHelpers.ParseTime(clockIn),
            ClockOut = PersistenceMappingHelpers.ParseTime(clockOut),
            Status = status,
            HoursWorked = hoursWorked,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        db.AttendanceRecords.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity.Id;
    }

    public async Task<AttendanceListItemDto?> UpdateScopedAsync(
        Guid id,
        string tenantId,
        string orgId,
        string? status,
        string? clockIn,
        string? clockOut,
        decimal? hoursWorked,
        CancellationToken ct = default)
    {
        var entity = await db.AttendanceRecords.FirstOrDefaultAsync(
            a => a.Id == id && a.TenantId == tenantId && a.OrgId == orgId, ct);
        if (entity is null)
            return null;

        var changed = false;
        if (!string.IsNullOrWhiteSpace(status))
        {
            entity.Status = status.Trim();
            changed = true;
        }
        if (clockIn is not null)
        {
            entity.ClockIn = PersistenceMappingHelpers.ParseTime(clockIn);
            changed = true;
        }
        if (clockOut is not null)
        {
            entity.ClockOut = PersistenceMappingHelpers.ParseTime(clockOut);
            changed = true;
        }
        if (hoursWorked.HasValue)
        {
            entity.HoursWorked = hoursWorked;
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
        var entity = await db.AttendanceRecords.FirstOrDefaultAsync(
            a => a.Id == id && a.TenantId == tenantId && a.OrgId == orgId, ct);
        if (entity is null)
            return false;
        db.AttendanceRecords.Remove(entity);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static IQueryable<AttendanceRecordEntity> ApplyFilters(
        IQueryable<AttendanceRecordEntity> query,
        string? search,
        string? status,
        string? branch)
    {
        if (!string.IsNullOrWhiteSpace(search) && search.Trim().Length >= 3)
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(a =>
                EF.Functions.ILike(a.EmployeeFullName, pattern)
                || (a.EmployeeCode != null && EF.Functions.ILike(a.EmployeeCode, pattern)));
        }

        if (!string.IsNullOrWhiteSpace(status) && !status.Equals("all", StringComparison.OrdinalIgnoreCase))
            query = query.Where(a => a.Status == status.Trim());

        if (!string.IsNullOrWhiteSpace(branch) && !branch.Equals("all", StringComparison.OrdinalIgnoreCase))
            query = query.Where(a => a.BranchName != null && EF.Functions.ILike(a.BranchName, $"%{branch.Trim()}%"));

        return query;
    }

    private static AttendanceListItemDto ToDto(AttendanceRecordEntity a) => new()
    {
        AttendanceId = a.Id.ToString(),
        EmployeeId = a.EmployeeId.ToString(),
        EmployeeFullName = a.EmployeeFullName,
        EmployeeCode = a.EmployeeCode,
        DepartmentName = a.DepartmentName,
        BranchName = a.BranchName,
        AttendanceDate = a.AttendanceDate,
        ClockIn = PersistenceMappingHelpers.FormatTime(a.ClockIn),
        ClockOut = PersistenceMappingHelpers.FormatTime(a.ClockOut),
        Status = a.Status,
        HoursWorked = a.HoursWorked,
    };
}
