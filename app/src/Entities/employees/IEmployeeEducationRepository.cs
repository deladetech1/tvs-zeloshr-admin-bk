using ZelosHR.Api.Persistence.Entities;

namespace ZelosHR.Api.Entities.Employees;

public interface IEmployeeEducationRepository
{
    Task<bool> EmployeeExistsAsync(Guid employeeId, string tenantId, string orgId, CancellationToken ct = default);
    Task<IReadOnlyList<EmployeeEducationEntity>> ListByEmployeeAsync(
        Guid employeeId, string tenantId, string orgId, CancellationToken ct = default);
    Task<EmployeeEducationEntity?> GetByIdAsync(
        Guid id, Guid employeeId, string tenantId, string orgId, CancellationToken ct = default);
    Task<EmployeeEducationEntity> AddAsync(EmployeeEducationEntity entity, CancellationToken ct = default);
    Task UpdateAsync(EmployeeEducationEntity entity, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, Guid employeeId, string tenantId, string orgId, CancellationToken ct = default);
}
