namespace ZelosHR.Api.Entities.Employees;

/// <summary>Minimal employee read contract for cross-module validation (leave, attendance, etc.).</summary>
public interface IEmployeeLookup
{
    Task<EmployeeDisplayInfo?> ResolveEmployeeDisplayAsync(
        Guid employeeId, string tenantId, string orgId, CancellationToken ct = default);

    Task<(Guid EmployeeId, string OrgId, EmployeeDisplayInfo Display)?> ResolveByPlatformUserAsync(
        string platformUserId, string tenantId, string orgId, CancellationToken ct = default);

    Task<(Guid EmployeeId, string OrgId, EmployeeDisplayInfo Display)?> ResolveEmployeeAsync(
        Guid employeeId, string tenantId, string orgId, CancellationToken ct = default);

    Task<IReadOnlyDictionary<Guid, EmployeeLeaveContext>> ResolveLeaveContextsAsync(
        IEnumerable<Guid> employeeIds, string tenantId, string orgId, CancellationToken ct = default);
}

public sealed record EmployeeDisplayInfo(string FullName, string? EmployeeCode);

public sealed record EmployeeLeaveContext(
    Guid EmployeeId,
    string FullName,
    string? EmployeeCode,
    string? JobTitle,
    Guid? DepartmentId,
    string? DepartmentName,
    Guid? LineManagerEmployeeId,
    Guid? HeadOfDepartmentEmployeeId,
    string? StoredProfileReference);
