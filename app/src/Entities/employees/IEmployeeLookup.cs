namespace ZelosHR.Api.Entities.Employees;

/// <summary>Minimal employee read contract for cross-module validation (leave, attendance, etc.).</summary>
public interface IEmployeeLookup
{
    Task<EmployeeDisplayInfo?> ResolveEmployeeDisplayAsync(
        Guid employeeId, string tenantId, string orgId, CancellationToken ct = default);

    Task<(Guid EmployeeId, EmployeeDisplayInfo Display)?> ResolveByPlatformUserAsync(
        string platformUserId, string tenantId, string orgId, CancellationToken ct = default);
}

public sealed record EmployeeDisplayInfo(string FullName, string? EmployeeCode);
