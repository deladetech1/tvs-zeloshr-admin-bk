using ZelosHR.Api.Entities.EmployeePortal;
using ZelosHR.Api.Persistence.Entities;

namespace ZelosHR.Api.Persistence.Repositories;

public interface IEmployeePortalSubdomainRepository
{
    Task<EmployeePortalSubdomainEntity?> GetEntityAsync(
        string tenantId, string orgId, CancellationToken ct = default);

    Task<EmployeePortalSubdomainEntity?> GetBySubdomainAsync(
        string subdomain, CancellationToken ct = default);

    Task<bool> SubdomainTakenAsync(
        string subdomain, Guid? excludeId = null, CancellationToken ct = default);

    Task<EmployeePortalSubdomainEntity> CreateAsync(
        string tenantId,
        string orgId,
        string busId,
        string locId,
        CreateEmployeePortalSubdomainDto data,
        string? actorUserId,
        CancellationToken ct = default);

    Task<EmployeePortalSubdomainEntity?> UpdateAsync(
        string tenantId,
        string orgId,
        string busId,
        string locId,
        UpdateEmployeePortalSubdomainDto data,
        string? actorUserId,
        CancellationToken ct = default);

    Task<bool> DeleteAsync(string tenantId, string orgId, CancellationToken ct = default);
}
