using ZelosHR.Api.Entities.CompanyInfo;
using ZelosHR.Api.Persistence.Entities;

namespace ZelosHR.Api.Persistence.Repositories;

public interface ICompanyOfficeRepository
{
    Task<IReadOnlyList<CompanyOfficeEntity>> ListAsync(
        string tenantId, string orgId, CancellationToken ct = default);

    /// <summary>
    /// Diffs <paramref name="items"/> against the org's current offices: entries with no
    /// <c>OfficeId</c> are created, entries matching an existing office replace its fields,
    /// existing offices absent from <paramref name="items"/> are deleted. If any entry's
    /// <c>OfficeId</c> doesn't match an office owned by this org, no changes are made and
    /// those ids are returned for the caller to reject.
    /// </summary>
    Task<(IReadOnlyList<CompanyOfficeEntity> Items, IReadOnlyList<Guid> UnknownIds)> ReplaceAllAsync(
        string tenantId,
        string orgId,
        IReadOnlyList<CompanyOfficeWriteDto> items,
        string? actorUserId,
        CancellationToken ct = default);

    Task DeleteAllAsync(string tenantId, string orgId, CancellationToken ct = default);
}
