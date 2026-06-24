using ZelosHR.Api.Entities.IdCardTypes;
using ZelosHR.Api.Persistence.Entities;

namespace ZelosHR.Api.Persistence.Repositories;

public interface IIdCardTypeRepository
{
    Task EnsureSystemDefaultsScopedAsync(string tenantId, string orgId, CancellationToken ct = default);

    Task<(IReadOnlyList<IdCardTypeListItemDto> Items, int Total)> ListScopedAsync(
        string tenantId,
        string orgId,
        IdCardTypeListQuery query,
        int page,
        int size,
        CancellationToken ct = default);

    Task<IdCardTypeListItemDto?> GetByIdScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default);

    Task<IdCardTypeEntity?> GetEntityByIdScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default);

    Task<bool> NameExistsScopedAsync(
        string tenantId, string orgId, string name, Guid? excludeId, CancellationToken ct = default);

    Task<Guid> CreateScopedAsync(
        string tenantId,
        string orgId,
        CreateIdCardTypeDto data,
        string? actorUserId,
        CancellationToken ct = default);

    Task<IdCardTypeListItemDto?> UpdateScopedAsync(
        Guid id,
        string tenantId,
        string orgId,
        UpdateIdCardTypeDto data,
        string? actorUserId,
        CancellationToken ct = default);

    Task<(bool Found, bool InUse)> DeleteScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default);
}
