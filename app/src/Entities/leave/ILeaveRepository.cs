namespace ZelosHR.Api.Entities.Leave;

public interface ILeaveRepository
{
    Task<LeaveSummaryDto> GetSummaryScopedAsync(string tenantId, string orgId, CancellationToken ct = default);

    Task<(IReadOnlyList<LeaveRequestListItemDto> Requests, IReadOnlyList<LeaveBalanceListItemDto> Balances, int Total)> ListScopedAsync(
        string tenantId,
        string orgId,
        string? search,
        string? status,
        string? leaveType,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<LeaveRequestListItemDto?> GetRequestByIdScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default);

    Task<Guid> CreateRequestScopedAsync(
        string tenantId,
        string orgId,
        Guid employeeId,
        string employeeFullName,
        string leaveType,
        DateOnly startDate,
        DateOnly endDate,
        decimal daysRequested,
        CancellationToken ct = default);

    Task<LeaveRequestListItemDto?> UpdateRequestScopedAsync(
        Guid id,
        string tenantId,
        string orgId,
        string? status,
        string? approverName,
        CancellationToken ct = default);

    Task<bool> DeletePendingRequestScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default);
}
