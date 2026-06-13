using ZelosHR.Api.Entities.Employees;

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

    Task<(IReadOnlyList<LeaveRequestRawRow> Requests, int Total)> ListRequestsScopedAsync(
        string tenantId,
        string orgId,
        LeaveRequestListQuery query,
        CancellationToken ct = default);

    Task<LeaveRequestRawRow?> GetRequestRawByIdScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default);

    Task<IReadOnlyList<LeaveRequestRawRow>> ListOnLeaveTodayScopedAsync(
        string tenantId, string orgId, int limit, CancellationToken ct = default);

    Task<IReadOnlyList<LeaveRequestRawRow>> ListPendingApprovalsScopedAsync(
        string tenantId, string orgId, int limit, CancellationToken ct = default);

    Task<IReadOnlyList<LeaveRequestRawRow>> ListPendingFinalApprovalsScopedAsync(
        string tenantId, string orgId, int limit, CancellationToken ct = default);

    Task<IReadOnlyList<LeaveRequestRawRow>> ListLeavingThisWeekScopedAsync(
        string tenantId, string orgId, int limit, CancellationToken ct = default);

    Task<HashSet<DateOnly>> GetPublicHolidayDatesInRangeScopedAsync(
        string tenantId,
        string orgId,
        DateOnly start,
        DateOnly end,
        string? countryCode,
        Guid? branchId,
        CancellationToken ct = default);

    Task<Guid> CreateRequestScopedAsync(
        string tenantId,
        string orgId,
        Guid employeeId,
        string employeeFullName,
        Guid leaveTypeId,
        string leaveTypeName,
        DateOnly startDate,
        DateOnly endDate,
        decimal daysRequested,
        string? notes,
        string initialApprovalStage,
        string? actorUserId = null,
        CancellationToken ct = default);

    Task<LeaveRequestRawRow?> UpdateRequestScopedAsync(
        Guid id,
        string tenantId,
        string orgId,
        string? status,
        string? notes,
        string? actorUserId = null,
        CancellationToken ct = default);

    Task<LeaveRequestRawRow?> AdvanceApprovalScopedAsync(
        Guid id,
        string tenantId,
        string orgId,
        string approverPlatformUserId,
        Guid? approverEmployeeId,
        EmployeeLeaveContext requestEmployee,
        CancellationToken ct = default);

    Task<LeaveRequestRawRow?> RejectRequestScopedAsync(
        Guid id, string tenantId, string orgId, string approverId, string? notes, CancellationToken ct = default);

    Task<bool> DeletePendingRequestScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default);

    Task<IReadOnlyList<LeaveBalanceRawRow>> ListBalancesScopedAsync(
        string tenantId,
        string orgId,
        Guid? employeeId,
        Guid? leaveTypeId,
        CancellationToken ct = default);

    Task<LeaveBalanceRawRow?> GetBalanceScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default);

    Task<LeaveBalanceRawRow?> GetBalanceForEmployeeScopedAsync(
        string tenantId, string orgId, Guid employeeId, Guid leaveTypeId, CancellationToken ct = default);

    Task<Guid> CreateBalanceScopedAsync(
        string tenantId,
        string orgId,
        Guid employeeId,
        string employeeFullName,
        Guid leaveTypeId,
        string leaveTypeName,
        decimal entitledDays,
        decimal usedDays,
        string? actorUserId = null,
        CancellationToken ct = default);

    Task<LeaveBalanceRawRow?> UpdateBalanceScopedAsync(
        Guid id, string tenantId, string orgId, decimal? entitledDays, decimal? usedDays, string? actorUserId = null, CancellationToken ct = default);

    Task<IReadOnlyList<LeaveTypeListItemDto>> ListTypesScopedAsync(
        string tenantId, string orgId, string? countryCode, bool activeOnly, CancellationToken ct = default);

    Task<LeaveTypeListItemDto?> GetTypeByIdScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default);

    Task<bool> TypeNameExistsScopedAsync(
        string tenantId, string orgId, string name, Guid? excludeId, CancellationToken ct = default);

    Task<Guid> CreateTypeScopedAsync(
        string tenantId, string orgId, CreateLeaveTypeDto data, string? actorUserId = null, CancellationToken ct = default);

    Task<LeaveTypeListItemDto?> UpdateTypeScopedAsync(
        Guid id, string tenantId, string orgId, UpdateLeaveTypeDto data, string? actorUserId = null, CancellationToken ct = default);

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
        string tenantId, string orgId, CreatePublicHolidayDto data, string? actorUserId = null, CancellationToken ct = default);

    Task<PublicHolidayListItemDto?> UpdateHolidayScopedAsync(
        Guid id, string tenantId, string orgId, UpdatePublicHolidayDto data, string? actorUserId = null, CancellationToken ct = default);

    Task<bool> DeleteHolidayScopedAsync(Guid id, string tenantId, string orgId, CancellationToken ct = default);
}
