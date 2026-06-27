using ZelosHR.Api.Persistence.Entities;

namespace ZelosHR.Api.Entities.Employees;

public interface IEmployeeIdentificationRepository
{
    Task<bool> EmployeeExistsAsync(Guid employeeId, string tenantId, string orgId, CancellationToken ct = default);

    Task<IReadOnlyList<EmployeeIdentificationEntity>> ListByEmployeeAsync(
        Guid employeeId, string tenantId, string orgId, CancellationToken ct = default);

    Task<EmployeeIdentificationEntity?> GetByIdAsync(
        Guid id, Guid employeeId, string tenantId, string orgId, CancellationToken ct = default);

    Task<EmployeeIdentificationEntity> AddAsync(EmployeeIdentificationEntity entity, CancellationToken ct = default);

    Task UpdateAsync(EmployeeIdentificationEntity entity, CancellationToken ct = default);

    Task<bool> DeleteAsync(
        Guid id, Guid employeeId, string tenantId, string orgId, CancellationToken ct = default);
}
