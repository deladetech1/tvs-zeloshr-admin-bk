using ZelosHR.Api.Entities.EmploymentTypes;
using ZelosHR.Api.Persistence.Entities;

namespace ZelosHR.Api.Persistence.Repositories;

public interface IEmploymentTypeRepository
{
    Task EnsureSystemDefaultsScopedAsync(string tenantId, string orgId, CancellationToken ct = default);

    Task BackfillEmployeeTypeIdsScopedAsync(string tenantId, string orgId, CancellationToken ct = default);

    Task<(IReadOnlyList<EmploymentTypeListItemDto> Items, int Total)> ListScopedAsync(
        string tenantId,
        string orgId,
        EmploymentTypeListQuery query,
        int page,
        int size,
        CancellationToken ct = default);

    Task<EmploymentTypeListItemDto?> GetByIdScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default);

    Task<EmploymentTypeEntity?> GetEntityByIdScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default);

    Task<bool> NameExistsScopedAsync(
        string tenantId, string orgId, string name, Guid? excludeId, CancellationToken ct = default);

    Task<Guid> CreateScopedAsync(
        string tenantId,
        string orgId,
        CreateEmploymentTypeDto data,
        string? actorUserId,
        CancellationToken ct = default);

    Task<EmploymentTypeListItemDto?> UpdateScopedAsync(
        Guid id,
        string tenantId,
        string orgId,
        UpdateEmploymentTypeDto data,
        string? actorUserId,
        CancellationToken ct = default);

    Task<(bool Found, bool InUse)> DeleteScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default);

    Task<int> CountEmployeesUsingTypeScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default);
}
