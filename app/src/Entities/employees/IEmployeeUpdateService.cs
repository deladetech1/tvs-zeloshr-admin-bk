using System.Text.Json.Nodes;
using ZelosHR.Api.Entities.Shared;

namespace ZelosHR.Api.Entities.Employees;

public interface IEmployeeUpdateService
{
    Task<Respons<EmployeeAggregateReadDto>> GetAsync(
        Guid employeeId,
        CancellationToken ct = default);

    Task<Respons<EmployeeAggregateReadDto>> ApplyAsync(
        Guid employeeId,
        UpdateEmployeeAggregateRequest request,
        CancellationToken ct = default);

    Task<JsonNode?> GetCurrentValueJsonAsync(
        Guid employeeId,
        string fieldPath,
        CancellationToken ct = default);
}
