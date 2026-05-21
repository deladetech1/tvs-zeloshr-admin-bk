using ZelosHR.Api.Persistence.Entities;

namespace ZelosHR.Api.Entities.Employees;

public interface IEmployeeWizardDocumentRepository
{
    Task<bool> EmployeeExistsAsync(Guid employeeId, string tenantId, string orgId, CancellationToken ct = default);
    Task<IReadOnlyList<EmployeeDocumentEntity>> ListByEmployeeAsync(
        Guid employeeId, string tenantId, string orgId, string? category, CancellationToken ct = default);
    Task<EmployeeDocumentEntity?> GetByIdAsync(
        Guid id, Guid employeeId, string tenantId, string orgId, CancellationToken ct = default);
    Task<EmployeeDocumentEntity> AddAsync(EmployeeDocumentEntity entity, CancellationToken ct = default);
    Task<bool> SoftDeleteAsync(Guid id, Guid employeeId, string tenantId, string orgId, CancellationToken ct = default);
}
