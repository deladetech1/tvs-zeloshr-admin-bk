using System.Text.Json.Nodes;
using ZelosHR.Api.Entities.Shared;

namespace ZelosHR.Api.Entities.Employees;

public sealed class EmployeeUpdateService(EmployeeAggregateService aggregate) : IEmployeeUpdateService
{
    public Task<Respons<EmployeeAggregateReadDto>> GetAsync(
        Guid employeeId,
        CancellationToken ct = default) =>
        aggregate.GetAsync(employeeId, ct);

    public Task<Respons<EmployeeAggregateReadDto>> ApplyAsync(
        Guid employeeId,
        UpdateEmployeeAggregateRequest request,
        CancellationToken ct = default) =>
        aggregate.UpdateAsync(employeeId, request, ct);

    public async Task<JsonNode?> GetCurrentValueJsonAsync(
        Guid employeeId,
        string fieldPath,
        CancellationToken ct = default)
    {
        var result = await aggregate.GetAsync(employeeId, ct);
        if (!result.Success || result.Data is null)
            return null;

        var updateShape = EmployeeAggregateJsonSnapshot.ToUpdateShape(result.Data);
        return EmployeeAggregateJsonSnapshot.ResolvePath(updateShape, fieldPath);
    }
}
