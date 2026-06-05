using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Formatting;
using ZelosHR.Api.Shared.Pagination;

namespace ZelosHR.Api.Entities.Attendance;

public class AttendanceService
{
    private readonly IAttendanceRepository _attendance;
    private readonly IEmployeeRepository _employees;

    public AttendanceService(IAttendanceRepository attendance, IEmployeeRepository employees)
    {
        _attendance = attendance;
        _employees = employees;
    }

    public async Task<Respons<AttendanceSummaryDto>> GetSummaryAsync(
        string tenantId, string orgId, DateOnly? date, CancellationToken ct = default)
    {
        var targetDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var summary = await _attendance.GetSummaryScopedAsync(tenantId, orgId, targetDate, ct);
        return Respons<AttendanceSummaryDto>.Ok(summary);
    }

    public async Task<Respons<AttendanceListDto>> ListAsync(
        string? search, string? status, string? branch, DateOnly? date,
        int page, int size, string tenantId, string orgId, CancellationToken ct = default)
    {
        var targetDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var paging = PagedQuery.From(page, size);
        var (items, total) = await _attendance.ListScopedAsync(
            tenantId, orgId, targetDate, search, status, branch, paging.Page, paging.Size, ct);
        var summary = await _attendance.GetSummaryScopedAsync(tenantId, orgId, targetDate, ct);

        return Respons<AttendanceListDto>.Ok(
            new AttendanceListDto { Summary = summary, Items = items },
            pagination: new PaginationMeta
            {
                Page = paging.Page,
                Size = paging.Size,
                Total = total,
                HasNext = paging.Offset + items.Count < total,
            });
    }

    public Task<Respons<AttendanceListItemDto>> GetByIdAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default) =>
        QueryOneAsync(id, tenantId, orgId, ct);

    public async Task<Respons<AttendanceListItemDto>> CreateAsync(
        CreateAttendanceDto data, string tenantId, string orgId, CancellationToken ct = default)
    {
        var emp = await _employees.GetByIdScopedAsync(data.EmployeeId, tenantId, orgId, ct);
        if (emp is null)
            return Respons<AttendanceListItemDto>.ValidationError(
                new Dictionary<string, string> { ["employeeId"] = "Employee not found." });

        var id = await _attendance.CreateScopedAsync(
            tenantId,
            orgId,
            data.EmployeeId,
            NameFormatting.BuildFullName(emp.FirstName, emp.MiddleName, emp.LastName),
            emp.EmployeeCode,
            emp.Department?.Name,
            emp.Branch?.Name,
            data.AttendanceDate,
            data.ClockIn,
            data.ClockOut,
            data.Status!.Trim(),
            data.HoursWorked,
            ct);

        return await QueryOneAsync(id, tenantId, orgId, ct);
    }

    public async Task<Respons<AttendanceListItemDto>> UpdateAsync(
        Guid id, UpdateAttendanceDto data, string tenantId, string orgId, CancellationToken ct = default)
    {
        var hasStatus = !string.IsNullOrWhiteSpace(data.Status);
        var hasClockIn = data.ClockIn is not null;
        var hasClockOut = data.ClockOut is not null;
        var hasHours = data.HoursWorked.HasValue;
        if (!hasStatus && !hasClockIn && !hasClockOut && !hasHours)
            return Respons<AttendanceListItemDto>.EmptyUpdateRequest();

        var updated = await _attendance.UpdateScopedAsync(
            id, tenantId, orgId,
            hasStatus ? data.Status : null,
            hasClockIn ? data.ClockIn : null,
            hasClockOut ? data.ClockOut : null,
            hasHours ? data.HoursWorked : null,
            ct);

        if (updated is null)
        {
            var exists = await _attendance.GetByIdScopedAsync(id, tenantId, orgId, ct);
            return exists is null
                ? Respons<AttendanceListItemDto>.Fail("Attendance record not found.", statusCode: 404)
                : Respons<AttendanceListItemDto>.EmptyUpdateRequest();
        }

        return Respons<AttendanceListItemDto>.Ok(updated);
    }

    public async Task<Respons<object>> DeleteAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default)
    {
        if (!await _attendance.DeleteScopedAsync(id, tenantId, orgId, ct))
            return Respons<object>.Fail("Attendance record not found.", statusCode: 404);
        return Respons<object>.Ok(new { attendanceId = id.ToString() }, "Attendance record deleted.");
    }

    private async Task<Respons<AttendanceListItemDto>> QueryOneAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct)
    {
        var row = await _attendance.GetByIdScopedAsync(id, tenantId, orgId, ct);
        return row is null
            ? Respons<AttendanceListItemDto>.Fail("Attendance record not found.", statusCode: 404)
            : Respons<AttendanceListItemDto>.Ok(row);
    }
}
