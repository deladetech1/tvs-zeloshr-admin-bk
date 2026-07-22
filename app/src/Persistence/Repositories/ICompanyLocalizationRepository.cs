using ZelosHR.Api.Entities.CompanyLocalization;
using ZelosHR.Api.Persistence.Entities;

namespace ZelosHR.Api.Persistence.Repositories;

public interface ICompanyLocalizationRepository
{
    Task<CompanyLocalizationEntity?> GetEntityAsync(
        string tenantId, string orgId, CancellationToken ct = default);

    Task<CompanyLocalizationEntity> EnsureStubAsync(
        string tenantId,
        string orgId,
        string currencyId,
        string? actorUserId,
        CancellationToken ct = default);

    Task<CompanyLocalizationEntity> CreateAsync(
        string tenantId,
        string orgId,
        CreateCompanyLocalizationDto data,
        string? actorUserId,
        CancellationToken ct = default);

    Task<CompanyLocalizationEntity?> UpdateAsync(
        string tenantId,
        string orgId,
        UpdateCompanyLocalizationDto data,
        string? actorUserId,
        CancellationToken ct = default);

    Task<bool> DeleteAsync(string tenantId, string orgId, CancellationToken ct = default);
}
