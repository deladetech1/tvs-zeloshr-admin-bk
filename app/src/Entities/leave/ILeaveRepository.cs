namespace ZelosHR.Api.Entities.Leave;

public interface ILeaveRepository
{
    Task<LeaveSummaryDto> GetSummaryScopedAsync(string tenantId, string orgId, CancellationToken ct = default);

    Task<int> CountEmployeeRequestsScopedAsync(
        string tenantId,
        string orgId,
        Guid employeeId,
        string? status,
        DateOnly? fromDate,
        CancellationToken ct = default);

    Task<(IReadOnlyList<LeaveRequestListItemDto> Requests, IReadOnlyList<LeaveBalanceListItemDto> Balances, int Total)> ListScopedAsync(
        string tenantId,
        string orgId,
        string? search,
        string? status,
        string? leaveType,
        Guid? employeeId,
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
        string? notes,
        CancellationToken ct = default);

    Task<LeaveRequestListItemDto?> UpdateRequestScopedAsync(
        Guid id,
        string tenantId,
        string orgId,
        string? status,
        string? approverName,
        string? notes,
        CancellationToken ct = default);

    Task<LeaveRequestListItemDto?> ApproveRequestScopedAsync(
        Guid id, string tenantId, string orgId, string approverName, CancellationToken ct = default);

    Task<LeaveRequestListItemDto?> RejectRequestScopedAsync(
        Guid id, string tenantId, string orgId, string approverName, string? notes, CancellationToken ct = default);

    Task<bool> DeletePendingRequestScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default);

    Task<IReadOnlyList<LeaveBalanceListItemDto>> ListBalancesScopedAsync(
        string tenantId,
        string orgId,
        Guid? employeeId,
        string? leaveType,
        CancellationToken ct = default);

    Task<LeaveBalanceListItemDto?> GetBalanceScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default);

    Task<LeaveBalanceListItemDto?> GetBalanceForEmployeeScopedAsync(
        string tenantId, string orgId, Guid employeeId, string leaveType, CancellationToken ct = default);

    Task<Guid> CreateBalanceScopedAsync(
        string tenantId,
        string orgId,
        Guid employeeId,
        string employeeFullName,
        string leaveType,
        decimal entitledDays,
        decimal usedDays,
        CancellationToken ct = default);

    Task<LeaveBalanceListItemDto?> UpdateBalanceScopedAsync(
        Guid id, string tenantId, string orgId, decimal? entitledDays, decimal? usedDays, CancellationToken ct = default);

    Task<IReadOnlyList<LeaveTypeListItemDto>> ListTypesScopedAsync(
        string tenantId, string orgId, string? countryCode, bool activeOnly, CancellationToken ct = default);

    Task<LeaveTypeListItemDto?> GetTypeByIdScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default);

    Task<bool> TypeNameExistsScopedAsync(
        string tenantId, string orgId, string name, Guid? excludeId, CancellationToken ct = default);

    Task<Guid> CreateTypeScopedAsync(
        string tenantId, string orgId, CreateLeaveTypeDto data, CancellationToken ct = default);

    Task<LeaveTypeListItemDto?> UpdateTypeScopedAsync(
        Guid id, string tenantId, string orgId, UpdateLeaveTypeDto data, CancellationToken ct = default);

    Task<bool> DeleteTypeScopedAsync(Guid id, string tenantId, string orgId, CancellationToken ct = default);

    Task<(IReadOnlyList<PublicHolidayListItemDto> Items, int Total)> ListHolidaysScopedAsync(
        string tenantId,
        string orgId,
        string? countryCode,
        int? year,
        Guid? branchId,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<PublicHolidayListItemDto?> GetHolidayByIdScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default);

    Task<Guid> CreateHolidayScopedAsync(
        string tenantId, string orgId, CreatePublicHolidayDto data, CancellationToken ct = default);

    Task<PublicHolidayListItemDto?> UpdateHolidayScopedAsync(
        Guid id, string tenantId, string orgId, UpdatePublicHolidayDto data, CancellationToken ct = default);

    Task<bool> DeleteHolidayScopedAsync(Guid id, string tenantId, string orgId, CancellationToken ct = default);
}
