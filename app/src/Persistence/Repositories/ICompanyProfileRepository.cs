using ZelosHR.Api.Entities.CompanyInfo;
using ZelosHR.Api.Persistence.Entities;

namespace ZelosHR.Api.Persistence.Repositories;

public interface ICompanyProfileRepository
{
    Task<CompanyProfileEntity?> GetEntityAsync(string tenantId, string orgId, CancellationToken ct = default);

    Task<CompanyProfileEntity> EnsureStubAsync(
        string tenantId,
        string orgId,
        string? actorUserId,
        string? defaultLegalName,
        CancellationToken ct = default);

    Task<CompanyProfileEntity> CreateAsync(
        string tenantId,
        string orgId,
        CreateCompanyInfoDto data,
        string? actorUserId,
        CancellationToken ct = default);

    Task<CompanyProfileEntity?> UpdateAsync(
        string tenantId,
        string orgId,
        UpdateCompanyInfoDto data,
        string? actorUserId,
        CancellationToken ct = default);

    Task<bool> DeleteAsync(string tenantId, string orgId, CancellationToken ct = default);
}
