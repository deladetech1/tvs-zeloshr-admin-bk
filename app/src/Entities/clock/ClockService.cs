using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Persistence.Entities;
using ZelosHR.Api.Persistence.Repositories;
using ZelosHR.Api.Shared.Formatting;

namespace ZelosHR.Api.Entities.Clock;

public class ClockService
{
    private readonly IClockRepository _clock;
    private readonly IEmployeeRepository _employees;

    public ClockService(IClockRepository clock, IEmployeeRepository employees)
    {
        _clock = clock;
        _employees = employees;
    }

    public async Task<Respons<ClockTodayDto>> TodayAsync(
        Guid employeeId, string tenantId, string orgId, CancellationToken ct = default)
    {
        var emp = await _employees.GetByIdScopedAsync(employeeId, tenantId, orgId, ct);
        if (emp is null)
            return Respons<ClockTodayDto>.Fail("Employee not found.", statusCode: 404);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var day = await _clock.GetDayAsync(tenantId, orgId, employeeId, today, ct);
        var punches = day is null
            ? []
            : await _clock.ListPunchesForDayAsync(tenantId, orgId, employeeId, day.Id, ct);

        return Respons<ClockTodayDto>.Ok(ToToday(emp, today, day, punches));
    }

    public Task<Respons<ClockTodayDto>> ClockInAsync(
        ClockActionRequest body, string tenantId, string orgId, string? actorUserId, CancellationToken ct = default) =>
        PunchAsync(body, "clock_in", tenantId, orgId, actorUserId, ct);

    public Task<Respons<ClockTodayDto>> ClockOutAsync(
        ClockActionRequest body, string tenantId, string orgId, string? actorUserId, CancellationToken ct = default) =>
        PunchAsync(body, "clock_out", tenantId, orgId, actorUserId, ct);

    public async Task<Respons<TimesheetDto>> TimesheetAsync(
        Guid employeeId, DateOnly? fromDate, DateOnly? toDate, string tenantId, string orgId, CancellationToken ct = default)
    {
        var emp = await _employees.GetByIdScopedAsync(employeeId, tenantId, orgId, ct);
        if (emp is null)
            return Respons<TimesheetDto>.Fail("Employee not found.", statusCode: 404);

        var to = toDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var from = fromDate ?? to.AddDays(-6);
        if (from > to)
            return Respons<TimesheetDto>.ValidationError(
                new Dictionary<string, string> { ["from_date"] = "from_date must be on or before to_date." });

        var days = await _clock.ListDaysAsync(tenantId, orgId, employeeId, from, to, ct);
        var punches = await _clock.ListPunchesForEmployeeRangeAsync(tenantId, orgId, employeeId, from, to, ct);
        var punchesByDay = punches.GroupBy(p => DateOnly.FromDateTime(p.PunchedAt.UtcDateTime))
            .ToDictionary(g => g.Key, g => g.ToList());

        var items = new List<TimesheetDayDto>();
        var present = 0;
        var adjustments = 0;
        var autoClosed = 0;
        var minutes = 0;

        for (var cursor = from; cursor <= to; cursor = cursor.AddDays(1))
        {
            var day = days.FirstOrDefault(d => d.AttendanceDate == cursor);
            var dayPunches = punchesByDay.GetValueOrDefault(cursor) ?? [];
            if (day is not null && day.HoursWorked.HasValue)
                minutes += (int)Math.Round(day.HoursWorked.Value * 60);
            if (day is not null && !string.Equals(day.Status, "Absent", StringComparison.OrdinalIgnoreCase))
                present++;
            if (day?.IsAdjusted == true)
                adjustments++;
            if (day?.AutoClosed == true)
                autoClosed++;

            items.Add(new TimesheetDayDto
            {
                Date = cursor,
                Weekday = cursor.ToDateTime(TimeOnly.MinValue).ToString("ddd"),
                ClockIn = PersistenceMappingHelpers.FormatTime(day?.ClockIn),
                ClockOut = PersistenceMappingHelpers.FormatTime(day?.ClockOut),
                HoursWorked = day?.HoursWorked,
                Status = day?.Status ?? "No record",
                HasAdjustment = day?.IsAdjusted == true,
                AutoClosed = day?.AutoClosed == true,
                Punches = dayPunches.Select(ToPunch).ToList(),
            });
        }

        var expectedDays = to.DayNumber - from.DayNumber + 1;
        return Respons<TimesheetDto>.Ok(new TimesheetDto
        {
            EmployeeId = employeeId.ToString(),
            EmployeeFullName = DisplayName(emp),
            FromDate = from,
            ToDate = to,
            Stats = new TimesheetStatsDto
            {
                DaysPresent = present,
                NoRecord = Math.Max(0, expectedDays - days.Count),
                Adjustments = adjustments,
                AutoClosed = autoClosed,
                PeriodMinutes = minutes,
            },
            Days = items,
        });
    }

    public async Task<Respons<TeamDto>> TeamAsync(
        Guid? managerEmployeeId,
        DateOnly? date,
        string? scope,
        string? department,
        string tenantId,
        string orgId,
        CancellationToken ct = default)
    {
        var targetDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var roster = await _employees.ListActiveAttendanceRosterAsync(tenantId, orgId, ct);
        var roleFlags = await _employees.ResolveRoleFlagsBatchAsync(
            roster.Select(e => e.EmployeeId).ToList(), tenantId, orgId, ct);
        var records = await _clock.ListDaysForOrgAsync(tenantId, orgId, targetDate, ct);
        var byEmployee = records.ToDictionary(r => r.EmployeeId.ToString(), r => r);

        var normalizedScope = string.IsNullOrWhiteSpace(scope) ? "all" : scope.Trim().ToLowerInvariant();
        IEnumerable<AttendanceRosterItem> filtered = roster;

        if (normalizedScope is "reports" && managerEmployeeId.HasValue)
        {
            var managerId = managerEmployeeId.Value;
            filtered = roster.Where(e => e.ReportsToEmployeeId == managerId);
        }
        else if (normalizedScope is "department" && !string.IsNullOrWhiteSpace(department))
        {
            filtered = roster.Where(e =>
                string.Equals(e.DepartmentName, department, StringComparison.OrdinalIgnoreCase));
        }

        var items = filtered.Select(e =>
        {
            byEmployee.TryGetValue(e.EmployeeId.ToString(), out var day);
            return new TeamMemberDto
            {
                EmployeeId = e.EmployeeId.ToString(),
                FullName = e.FullName,
                JobTitle = e.JobTitle,
                DepartmentName = e.DepartmentName,
                ClockIn = PersistenceMappingHelpers.FormatTime(day?.ClockIn),
                ClockOut = PersistenceMappingHelpers.FormatTime(day?.ClockOut),
                HoursWorked = day?.HoursWorked,
                Status = day?.Status ?? "No record",
                IsLineManager = roleFlags.IsLineManager(e.EmployeeId),
                IsHeadOfDepartment = roleFlags.IsHeadOfDepartment(e.EmployeeId),
            };
        }).ToList();

        var tabs = new List<TeamTabDto>
        {
            new() { Id = "all", Label = "Everyone", Count = roster.Count },
            new()
            {
                Id = "reports",
                Label = "My reports",
                Count = managerEmployeeId.HasValue
                    ? roster.Count(e => e.ReportsToEmployeeId == managerEmployeeId.Value)
                    : 0,
            },
        };

        foreach (var dept in roster
            .Select(e => e.DepartmentName)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n))
        {
            tabs.Add(new TeamTabDto
            {
                Id = $"dept:{dept}",
                Label = dept!,
                Count = roster.Count(e => string.Equals(e.DepartmentName, dept, StringComparison.OrdinalIgnoreCase)),
            });
        }

        return Respons<TeamDto>.Ok(new TeamDto
        {
            Date = targetDate,
            Scope = normalizedScope,
            Tabs = tabs,
            Items = items,
        });
    }

    private async Task<Respons<ClockTodayDto>> PunchAsync(
        ClockActionRequest body,
        string punchType,
        string tenantId,
        string orgId,
        string? actorUserId,
        CancellationToken ct)
    {
        if (body.OnCompanyNetwork == false)
            return Respons<ClockTodayDto>.Fail(
                "You must be connected to your company's network.", statusCode: 403);

        if (body.EmployeeId == Guid.Empty)
            return Respons<ClockTodayDto>.ValidationError(
                new Dictionary<string, string> { ["employee_id"] = "employee_id is required." });

        var emp = await _employees.GetByIdScopedAsync(body.EmployeeId, tenantId, orgId, ct);
        if (emp is null)
            return Respons<ClockTodayDto>.Fail("Employee not found.", statusCode: 404);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var fullName = DisplayName(emp);
        var day = await _clock.GetOrCreateDayAsync(
            tenantId, orgId, body.EmployeeId, fullName, emp.EmployeeCode,
            emp.Department?.Name, emp.Branch?.Name, today, actorUserId, ct);

        var existing = await _clock.ListPunchesForDayAsync(tenantId, orgId, body.EmployeeId, day.Id, ct);
        var last = existing.LastOrDefault();
        if (string.Equals(punchType, "clock_in", StringComparison.OrdinalIgnoreCase)
            && last is not null
            && string.Equals(last.PunchType, "clock_in", StringComparison.OrdinalIgnoreCase))
        {
            return Respons<ClockTodayDto>.ValidationError(
                new Dictionary<string, string> { ["punch_type"] = "Already clocked in. Clock out first." });
        }
        if (string.Equals(punchType, "clock_out", StringComparison.OrdinalIgnoreCase)
            && (last is null || string.Equals(last.PunchType, "clock_out", StringComparison.OrdinalIgnoreCase)))
        {
            return Respons<ClockTodayDto>.ValidationError(
                new Dictionary<string, string> { ["punch_type"] = "Clock in before clocking out." });
        }

        var source = string.IsNullOrWhiteSpace(body.Source) ? "web" : body.Source.Trim();
        await _clock.AddPunchAsync(
            tenantId, orgId, body.EmployeeId, day.Id, punchType, DateTimeOffset.UtcNow,
            source, body.DeviceId, actorUserId, ct);
        await _clock.RecalculateDayAsync(day.Id, tenantId, orgId, actorUserId, ct);

        return await TodayAsync(body.EmployeeId, tenantId, orgId, ct);
    }

    private static ClockTodayDto ToToday(
        EmployeeEntity emp,
        DateOnly today,
        AttendanceRecordEntity? day,
        IReadOnlyList<PunchEntity> punches)
    {
        var last = punches.LastOrDefault();
        var state = last is null
            ? "not_in"
            : string.Equals(last.PunchType, "clock_in", StringComparison.OrdinalIgnoreCase)
                ? "clocked_in"
                : "day_complete";

        return new ClockTodayDto
        {
            EmployeeId = emp.Id.ToString(),
            EmployeeFullName = DisplayName(emp),
            JobTitle = emp.JobTitle,
            DepartmentName = emp.Department?.Name,
            Date = today,
            State = state,
            ClockIn = PersistenceMappingHelpers.FormatTime(day?.ClockIn),
            ClockOut = PersistenceMappingHelpers.FormatTime(day?.ClockOut),
            HoursWorked = day?.HoursWorked,
            Punches = punches.Select(ToPunch).ToList(),
            CreatedAt = day?.CreatedAt ?? DateTimeOffset.UtcNow,
            UpdatedAt = day?.UpdatedAt ?? DateTimeOffset.UtcNow,
            CreatedById = day?.CreatedById,
            UpdatedById = day?.UpdatedById,
            CreatedBy = null,
            UpdatedBy = null,
        };
    }

    private static string DisplayName(EmployeeEntity emp) =>
        string.IsNullOrWhiteSpace(emp.FullName)
            ? NameFormatting.BuildFullName(emp.FirstName, emp.MiddleName, emp.LastName)
            : emp.FullName;

    internal static PunchDto ToPunch(PunchEntity p) => new()
    {
        PunchId = p.Id.ToString(),
        PunchType = p.PunchType,
        PunchedAt = p.PunchedAt,
        Source = p.Source,
        DeviceId = p.DeviceId?.ToString(),
        IsSuperseded = p.IsSuperseded,
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt,
        CreatedById = p.CreatedById,
        UpdatedById = p.UpdatedById,
        CreatedBy = null,
        UpdatedBy = null,
    };
}
