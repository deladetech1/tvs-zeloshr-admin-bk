using ZelosHR.Api.Entities.Shared;

namespace ZelosHR.Api.Entities.Employees;

public interface IEmployeesService
{
    Task<Respons<EmployeeReadDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Respons<IReadOnlyList<EmployeeReadDto>>> GetAllAsync(
        int page, int pageSize, CancellationToken ct = default);
    Task<Respons<EmployeeReadDto>> CreateAsync(EmployeeWriteDto dto, CancellationToken ct = default);
    Task<Respons<EmployeeReadDto>> UpdateAsync(Guid id, EmployeeWriteDto dto, CancellationToken ct = default);
    Task<Respons<bool>> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<Respons<IReadOnlyList<EmployeeReadDto>>> SearchAsync(
        string? nameQuery,
        Guid? departmentId,
        string? status,
        int page,
        int pageSize,
        CancellationToken ct = default);
}
