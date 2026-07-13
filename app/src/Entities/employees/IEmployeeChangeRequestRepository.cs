using ZelosHR.Api.Persistence.Entities;

namespace ZelosHR.Api.Entities.Employees;

public interface IEmployeeChangeRequestRepository
{
    Task<EmployeeChangeRequestEntity?> GetByIdScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default);

    Task<EmployeeChangeRequestEntity?> GetTrackedByIdScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default);

    Task<(IReadOnlyList<EmployeeChangeRequestEntity> Items, int Total)> ListScopedAsync(
        string tenantId,
        string orgId,
        Guid? employeeId,
        string? status,
        int page,
        int size,
        CancellationToken ct = default);

    Task<IReadOnlyList<EmployeeChangeRequestEntity>> ListPendingByFieldPathsAsync(
        Guid employeeId,
        IEnumerable<string> fieldPaths,
        CancellationToken ct = default);

    Task AddAsync(EmployeeChangeRequestEntity entity, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
