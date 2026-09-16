using ZelosHR.Api.Persistence.Entities;

namespace ZelosHR.Api.Entities.Clock;

public interface IClockRepository
{
    Task<AttendanceRecordEntity> GetOrCreateDayAsync(
        string tenantId,
        string orgId,
        Guid employeeId,
        string employeeFullName,
        string? employeeCode,
        string? departmentName,
        string? branchName,
        DateOnly date,
        string? actorUserId,
        CancellationToken ct = default);

    Task<AttendanceRecordEntity?> GetDayAsync(
        string tenantId, string orgId, Guid employeeId, DateOnly date, CancellationToken ct = default);

    Task<IReadOnlyList<PunchEntity>> ListPunchesForDayAsync(
        string tenantId, string orgId, Guid employeeId, Guid attendanceId, CancellationToken ct = default);

    Task<IReadOnlyList<PunchEntity>> ListPunchesForEmployeeRangeAsync(
        string tenantId, string orgId, Guid employeeId, DateOnly from, DateOnly to, CancellationToken ct = default);

    Task<PunchEntity> AddPunchAsync(
        string tenantId,
        string orgId,
        Guid employeeId,
        Guid attendanceId,
        string punchType,
        DateTimeOffset punchedAt,
        string source,
        Guid? deviceId,
        string? actorUserId,
        CancellationToken ct = default);

    Task RecalculateDayAsync(
        Guid attendanceId, string tenantId, string orgId, string? actorUserId, CancellationToken ct = default);

    Task MarkPunchSupersededAsync(Guid punchId, string tenantId, string orgId, string? actorUserId, CancellationToken ct = default);

    Task<IReadOnlyList<AttendanceRecordEntity>> ListDaysAsync(
        string tenantId, string orgId, Guid employeeId, DateOnly from, DateOnly to, CancellationToken ct = default);

    Task<IReadOnlyList<AttendanceRecordEntity>> ListDaysForOrgAsync(
        string tenantId, string orgId, DateOnly date, CancellationToken ct = default);

    Task MarkAdjustedAsync(Guid attendanceId, string tenantId, string orgId, string? actorUserId, CancellationToken ct = default);
}
