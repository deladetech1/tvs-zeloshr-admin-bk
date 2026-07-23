using Microsoft.EntityFrameworkCore;
using ZelosHR.Api.Entities.EmployeeIdFormat;
using ZelosHR.Api.Persistence.Entities;

namespace ZelosHR.Api.Persistence.Repositories;

public interface IEmployeeIdFormatRepository
{
    Task<EmployeeIdFormatEntity?> GetEntityAsync(
        string tenantId, string orgId, CancellationToken ct = default);

    Task<EmployeeIdFormatEntity> CreateAsync(
        string tenantId,
        string orgId,
        CreateEmployeeIdFormatDto data,
        string? actorUserId,
        CancellationToken ct = default);

    Task<EmployeeIdFormatEntity?> UpdateAsync(
        string tenantId,
        string orgId,
        UpdateEmployeeIdFormatDto data,
        string? actorUserId,
        CancellationToken ct = default);

    Task<bool> DeleteAsync(string tenantId, string orgId, CancellationToken ct = default);
}
