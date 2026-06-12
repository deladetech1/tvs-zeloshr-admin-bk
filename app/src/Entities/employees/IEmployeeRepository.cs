using ZelosHR.Api.Persistence.Entities;
using ZelosHR.Api.Shared.Abstractions;

namespace ZelosHR.Api.Entities.Employees;

public interface IEmployeeRepository : IRepository<EmployeeEntity, Guid>
{
    Task<EmployeeEntity?> GetByIdScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default);

    Task<EmployeeEntity?> GetByPlatformUserIdScopedAsync(
        string platformUserId, string tenantId, string orgId, CancellationToken ct = default);

    Task<EmployeeEntity?> GetByIdScopedForUpdateAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default);

    Task<EmployeeEntity?> GetByGhanaCardAsync(
        string ghanaCard, string tenantId, CancellationToken ct = default);

    Task<bool> ExistsByGhanaCardAsync(
        string ghanaCard, string tenantId, Guid? excludeId = null, CancellationToken ct = default);

    Task<(IReadOnlyList<EmployeeEntity> Items, int TotalCount)> GetPagedScopedAsync(
        string tenantId, string orgId, int page, int pageSize, CancellationToken ct = default);

    Task<(IReadOnlyList<EmployeeEntity> Items, int TotalCount)> ListScopedAsync(
        EmployeeListQuery query,
        string tenantId,
        string orgId,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<IReadOnlyList<EmployeeEntity>> ExportListScopedAsync(
        EmployeeExportQuery query,
        string tenantId,
        string orgId,
        CancellationToken ct = default);

    Task<(IReadOnlyList<EmployeeEntity> Items, int TotalCount)> SearchScopedAsync(
        string? nameQuery,
        Guid? departmentId,
        string? status,
        string tenantId,
        string orgId,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<long> GetNextEmployeeSequenceAsync(
        string tenantId, string orgId, CancellationToken ct = default);

    Task<bool> SoftDeleteScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default);

    Task<bool> ExistsActiveScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default);
}
