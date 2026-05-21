using ZelosHR.Api.Persistence.Entities;

namespace ZelosHR.Api.Entities.Employees;

public interface IEmployeeCertificationRepository
{
    Task<bool> EmployeeExistsAsync(Guid employeeId, string tenantId, string orgId, CancellationToken ct = default);
    Task<IReadOnlyList<EmployeeCertificationEntity>> ListByEmployeeAsync(
        Guid employeeId, string tenantId, string orgId, CancellationToken ct = default);
    Task<EmployeeCertificationEntity?> GetByIdAsync(
        Guid id, Guid employeeId, string tenantId, string orgId, CancellationToken ct = default);
    Task<EmployeeCertificationEntity> AddAsync(EmployeeCertificationEntity entity, CancellationToken ct = default);
    Task UpdateAsync(EmployeeCertificationEntity entity, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, Guid employeeId, string tenantId, string orgId, CancellationToken ct = default);
}
