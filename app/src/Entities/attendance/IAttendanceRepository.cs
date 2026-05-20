namespace ZelosHR.Api.Entities.Attendance;

public interface IAttendanceRepository
{
    Task<AttendanceSummaryDto> GetSummaryScopedAsync(
        string tenantId, string orgId, DateOnly date, CancellationToken ct = default);

    Task<(IReadOnlyList<AttendanceListItemDto> Items, int Total)> ListScopedAsync(
        string tenantId,
        string orgId,
        DateOnly date,
        string? search,
        string? status,
        string? branch,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<AttendanceListItemDto?> GetByIdScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default);

    Task<Guid> CreateScopedAsync(
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
        CancellationToken ct = default);

    Task<AttendanceListItemDto?> UpdateScopedAsync(
        Guid id,
        string tenantId,
        string orgId,
        string? status,
        string? clockIn,
        string? clockOut,
        decimal? hoursWorked,
        CancellationToken ct = default);

    Task<bool> DeleteScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default);
}
