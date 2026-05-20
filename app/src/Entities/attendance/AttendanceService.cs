using Dapper;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Pagination;

namespace ZelosHR.Api.Entities.Attendance;

public class AttendanceService
{
    private readonly IDatabaseManager _database;
    private readonly EmployeesService _employees;

    public AttendanceService(IDatabaseManager database, EmployeesService employees)
    {
        _database = database;
        _employees = employees;
    }

    public async Task<Respons<AttendanceSummaryDto>> GetSummaryAsync(
        string tenantId, string orgId, DateOnly? date, CancellationToken ct = default)
    {
        var targetDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var targetDateDb = targetDate.ToDateTime(TimeOnly.MinValue);
        await using var connection = await _database.GetConnectionAsync(ct);

        var summary = await connection.QuerySingleAsync<AttendanceSummaryDto>(
            """
            SELECT
                COUNT(*)::int AS TotalScheduled,
                COUNT(*) FILTER (WHERE status = 'Present')::int AS Present,
                COUNT(*) FILTER (WHERE status = 'Late')::int AS Late,
                COUNT(*) FILTER (WHERE status = 'Absent')::int AS Absent,
                COUNT(*) FILTER (WHERE status = 'On Leave')::int AS OnLeave
            FROM zeloshr.zhr_attendance_records
            WHERE tenant_id = @TenantId AND org_id = @OrgId AND attendance_date = @Date
            """,
            new { TenantId = tenantId, OrgId = orgId, Date = targetDateDb });

        return Respons<AttendanceSummaryDto>.Ok(summary);
    }

    public async Task<Respons<AttendanceListDto>> ListAsync(
        string? search, string? status, string? branch, DateOnly? date,
        int page, int size, string tenantId, string orgId, CancellationToken ct = default)
    {
        var targetDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var targetDateDb = targetDate.ToDateTime(TimeOnly.MinValue);
        var paging = PagedQuery.From(page, size);
        await using var connection = await _database.GetConnectionAsync(ct);

        var conditions = new List<string>
        {
            "tenant_id = @TenantId", "org_id = @OrgId", "attendance_date = @Date",
        };
        var parameters = new DynamicParameters(new { TenantId = tenantId, OrgId = orgId, Date = targetDateDb });

        if (!string.IsNullOrWhiteSpace(search) && search.Trim().Length >= 3)
        {
            conditions.Add("(employee_full_name ILIKE @Search OR employee_code ILIKE @Search)");
            parameters.Add("Search", $"%{search.Trim()}%");
        }
        if (!string.IsNullOrWhiteSpace(status) && !status.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            conditions.Add("status = @Status");
            parameters.Add("Status", status);
        }
        if (!string.IsNullOrWhiteSpace(branch) && !branch.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            conditions.Add("branch_name ILIKE @Branch");
            parameters.Add("Branch", $"%{branch.Trim()}%");
        }

        var where = string.Join(" AND ", conditions);
        parameters.Add("Limit", paging.Size);
        parameters.Add("Offset", paging.Offset);

        var total = await connection.ExecuteScalarAsync<int>(
            $"SELECT COUNT(*)::int FROM zeloshr.zhr_attendance_records WHERE {where}", parameters);

        var rows = await connection.QueryAsync<AttendanceListItemDto>(
            $"""
            SELECT
                id::text AS AttendanceId,
                employee_id::text AS EmployeeId,
                employee_full_name AS EmployeeFullName,
                employee_code AS EmployeeCode,
                department_name AS DepartmentName,
                branch_name AS BranchName,
                attendance_date AS AttendanceDate,
                TO_CHAR(clock_in, 'HH24:MI') AS ClockIn,
                TO_CHAR(clock_out, 'HH24:MI') AS ClockOut,
                status AS Status,
                hours_worked AS HoursWorked
            FROM zeloshr.zhr_attendance_records
            WHERE {where}
            ORDER BY employee_full_name
            LIMIT @Limit OFFSET @Offset
            """,
            parameters);

        var items = rows.ToList();
        var summary = (await GetSummaryAsync(tenantId, orgId, targetDate, ct)).Data ?? new AttendanceSummaryDto();

        return Respons<AttendanceListDto>.Ok(
            new AttendanceListDto { Summary = summary, Items = items },
            pagination: new PaginationMeta
            {
                Page = paging.Page, Size = paging.Size, Total = total,
                HasNext = paging.Offset + items.Count < total,
            });
    }

    public async Task<Respons<AttendanceListItemDto>> GetByIdAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default) =>
        await QueryOneAsync(id, tenantId, orgId, ct);

    public async Task<Respons<AttendanceListItemDto>> CreateAsync(
        CreateAttendanceDto data, string tenantId, string orgId, CancellationToken ct = default)
    {
        var emp = await _employees.GetByIdAsync(data.EmployeeId, tenantId, orgId, ct);
        if (!emp.Success || emp.Data is null)
            return Respons<AttendanceListItemDto>.ValidationError(
                new Dictionary<string, string> { ["employeeId"] = "Employee not found." });

        await using var connection = await _database.GetConnectionAsync(ct);
        var id = await connection.QuerySingleAsync<Guid>(
            """
            INSERT INTO zeloshr.zhr_attendance_records (
                tenant_id, org_id, employee_id, employee_full_name, employee_code,
                department_name, branch_name, attendance_date, clock_in, clock_out, status, hours_worked
            )
            VALUES (
                @TenantId, @OrgId, @EmployeeId, @FullName, @Code,
                @Dept, @Branch, @Date,
                @ClockIn::time, @ClockOut::time, @Status, @Hours
            )
            RETURNING id
            """,
            new
            {
                TenantId = tenantId,
                OrgId = orgId,
                EmployeeId = data.EmployeeId,
                FullName = emp.Data.FullName,
                Code = emp.Data.EmployeeCode,
                Dept = emp.Data.DepartmentName,
                Branch = emp.Data.BranchName,
                Date = data.AttendanceDate,
                ClockIn = data.ClockIn,
                ClockOut = data.ClockOut,
                Status = data.Status!.Trim(),
                Hours = data.HoursWorked,
            });

        return await QueryOneAsync(id, tenantId, orgId, ct);
    }

    public async Task<Respons<AttendanceListItemDto>> UpdateAsync(
        Guid id, UpdateAttendanceDto data, string tenantId, string orgId, CancellationToken ct = default)
    {
        var sets = new List<string>();
        var p = new DynamicParameters(new { Id = id, TenantId = tenantId, OrgId = orgId });
        if (!string.IsNullOrWhiteSpace(data.Status)) { sets.Add("status = @Status"); p.Add("Status", data.Status.Trim()); }
        if (data.ClockIn is not null) { sets.Add("clock_in = @ClockIn::time"); p.Add("ClockIn", data.ClockIn); }
        if (data.ClockOut is not null) { sets.Add("clock_out = @ClockOut::time"); p.Add("ClockOut", data.ClockOut); }
        if (data.HoursWorked.HasValue) { sets.Add("hours_worked = @Hours"); p.Add("Hours", data.HoursWorked); }
        if (sets.Count == 0)
            return Respons<AttendanceListItemDto>.Fail("No fields to update.", statusCode: 400);

        await using var connection = await _database.GetConnectionAsync(ct);
        if (await connection.ExecuteAsync(
                $"UPDATE zeloshr.zhr_attendance_records SET {string.Join(", ", sets)} WHERE id = @Id AND tenant_id = @TenantId AND org_id = @OrgId", p) == 0)
            return Respons<AttendanceListItemDto>.Fail("Attendance record not found.", statusCode: 404);

        return await QueryOneAsync(id, tenantId, orgId, ct);
    }

    public async Task<Respons<object>> DeleteAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default)
    {
        await using var connection = await _database.GetConnectionAsync(ct);
        var n = await connection.ExecuteAsync(
            "DELETE FROM zeloshr.zhr_attendance_records WHERE id = @Id AND tenant_id = @TenantId AND org_id = @OrgId",
            new { Id = id, TenantId = tenantId, OrgId = orgId });
        return n == 0
            ? Respons<object>.Fail("Attendance record not found.", statusCode: 404)
            : Respons<object>.Ok(new { attendanceId = id.ToString() }, "Attendance record deleted.");
    }

    private async Task<Respons<AttendanceListItemDto>> QueryOneAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct)
    {
        await using var connection = await _database.GetConnectionAsync(ct);
        var row = await connection.QuerySingleOrDefaultAsync<AttendanceListItemDto>(
            """
            SELECT
                id::text AS AttendanceId, employee_id::text AS EmployeeId,
                employee_full_name AS EmployeeFullName, employee_code AS EmployeeCode,
                department_name AS DepartmentName, branch_name AS BranchName,
                attendance_date AS AttendanceDate,
                TO_CHAR(clock_in, 'HH24:MI') AS ClockIn,
                TO_CHAR(clock_out, 'HH24:MI') AS ClockOut,
                status AS Status, hours_worked AS HoursWorked
            FROM zeloshr.zhr_attendance_records
            WHERE id = @Id AND tenant_id = @TenantId AND org_id = @OrgId
            """,
            new { Id = id, TenantId = tenantId, OrgId = orgId });
        return row is null
            ? Respons<AttendanceListItemDto>.Fail("Attendance record not found.", statusCode: 404)
            : Respons<AttendanceListItemDto>.Ok(row);
    }
}
