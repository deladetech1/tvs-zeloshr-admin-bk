namespace ZelosHR.Api.Entities.Employees;

public interface IEmployeeDirectoryRepository
{
    Task<EmployeeDirectorySummaryDto> GetSummaryScopedAsync(
        string tenantId, string orgId, CancellationToken ct = default);

    Task<(IReadOnlyList<EmployeeDirectoryListRow> Items, int Total)> ListScopedAsync(
        EmployeeDirectoryQuery query,
        string tenantId,
        string orgId,
        CancellationToken ct = default);
}

public sealed record EmployeeDirectoryListRow(
    Guid Id,
    string EmployeeCode,
    string FullName,
    string? FirstName,
    string? MiddleName,
    string? LastName,
    string? JobTitle,
    Guid? DepartmentId,
    string? DepartmentName,
    Guid? BranchId,
    string? BranchName,
    string? EmploymentType,
    Guid? ManagerId,
    string? ManagerFirstName,
    string? ManagerLastName,
    string Status);
