using ZelosHR.Api.Entities.Clock;
using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Persistence.Repositories;
using ZelosHR.Api.Shared.Formatting;
using ZelosHR.Api.Shared.Pagination;

namespace ZelosHR.Api.Entities.Adjustments;

public class AdjustmentService
{
    private readonly IAdjustmentRepository _adjustments;
    private readonly IClockRepository _clock;
    private readonly IEmployeeRepository _employees;

    public AdjustmentService(
        IAdjustmentRepository adjustments,
        IClockRepository clock,
        IEmployeeRepository employees)
    {
        _adjustments = adjustments;
        _clock = clock;
        _employees = employees;
    }

    public async Task<Respons<IReadOnlyList<AdjustmentDto>>> ListAsync(
        Guid? employeeId,
        DateOnly? from,
        DateOnly? to,
        int page,
        int size,
        string tenantId,
        string orgId,
        CancellationToken ct = default)
    {
        var paging = PagedQuery.From(page, size);
        var (items, total) = await _adjustments.ListScopedAsync(
            tenantId, orgId, employeeId, from, to, paging.Page, paging.Size, ct);
        return Respons<IReadOnlyList<AdjustmentDto>>.Ok(
            items,
            pagination: new PaginationMeta
            {
                Page = paging.Page,
                Size = paging.Size,
                Total = total,
                HasNext = paging.Offset + items.Count < total,
            });
    }

    public async Task<Respons<AdjustmentDto>> CreateAsync(
        CreateAdjustmentDto data, string tenantId, string orgId, string? actorUserId, CancellationToken ct = default)
    {
        if (data.EmployeeId == Guid.Empty)
            return Respons<AdjustmentDto>.ValidationError(
                new Dictionary<string, string> { ["employee_id"] = "employee_id is required." });
        if (string.IsNullOrWhiteSpace(data.Kind))
            return Respons<AdjustmentDto>.ValidationError(
                new Dictionary<string, string> { ["kind"] = "kind is required." });
        if (string.IsNullOrWhiteSpace(data.Reason))
            return Respons<AdjustmentDto>.ValidationError(
                new Dictionary<string, string> { ["reason"] = "reason is required." });

        var emp = await _employees.GetByIdScopedAsync(data.EmployeeId, tenantId, orgId, ct);
        if (emp is null)
            return Respons<AdjustmentDto>.Fail("Employee not found.", statusCode: 404);

        var kind = data.Kind.Trim().ToLowerInvariant();
        if (kind is not ("missing_pair" or "missing_single" or "supersede"))
            return Respons<AdjustmentDto>.ValidationError(
                new Dictionary<string, string> { ["kind"] = "kind must be missing_pair, missing_single, or supersede." });

        var fullName = string.IsNullOrWhiteSpace(emp.FullName)
            ? NameFormatting.BuildFullName(emp.FirstName, emp.MiddleName, emp.LastName)
            : emp.FullName;
        var day = await _clock.GetOrCreateDayAsync(
            tenantId, orgId, data.EmployeeId, fullName, emp.EmployeeCode,
            emp.Department?.Name, emp.Branch?.Name, data.AttendanceDate, actorUserId, ct);

        if (kind == "supersede" && data.OriginalPunchId.HasValue)
            await _clock.MarkPunchSupersededAsync(data.OriginalPunchId.Value, tenantId, orgId, actorUserId, ct);

        var punchType = string.IsNullOrWhiteSpace(data.PunchType) ? null : data.PunchType.Trim().ToLowerInvariant();
        var punchTime = PersistenceMappingHelpers.ParseTime(data.PunchTime);

        if (kind == "missing_pair")
        {
            var inTime = punchTime ?? new TimeOnly(8, 0);
            var outTime = inTime.AddHours(8);
            await _clock.AddPunchAsync(
                tenantId, orgId, data.EmployeeId, day.Id, "clock_in",
                new DateTimeOffset(data.AttendanceDate.ToDateTime(inTime), TimeSpan.Zero),
                "adjustment", null, actorUserId, ct);
            await _clock.AddPunchAsync(
                tenantId, orgId, data.EmployeeId, day.Id, "clock_out",
                new DateTimeOffset(data.AttendanceDate.ToDateTime(outTime), TimeSpan.Zero),
                "adjustment", null, actorUserId, ct);
        }
        else if (punchType is "clock_in" or "clock_out" && punchTime.HasValue)
        {
            var punchedAt = new DateTimeOffset(data.AttendanceDate.ToDateTime(punchTime.Value), TimeSpan.Zero);
            await _clock.AddPunchAsync(
                tenantId, orgId, data.EmployeeId, day.Id, punchType, punchedAt,
                "adjustment", null, actorUserId, ct);
        }

        await _clock.RecalculateDayAsync(day.Id, tenantId, orgId, actorUserId, ct);
        await _clock.MarkAdjustedAsync(day.Id, tenantId, orgId, actorUserId, ct);

        var entity = await _adjustments.AddAsync(
            tenantId, orgId, data.EmployeeId, day.Id, data.AttendanceDate, kind,
            punchType, punchTime, data.OriginalPunchId, data.Reason.Trim(), actorUserId, ct);

        return Respons<AdjustmentDto>.Ok(AdjustmentRepository.ToDto(entity), "Adjustment created.", statusCode: 201);
    }
}
