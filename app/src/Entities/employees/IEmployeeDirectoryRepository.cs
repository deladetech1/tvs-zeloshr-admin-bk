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
    string? UserId,
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
    string? ManagerUserId,
    string? ManagerFirstName,
    string? ManagerLastName,
    string Status,
    string LifecycleState,
    bool IsDraft,
    DateOnly? ProbationEndDate,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string? CreatedBy,
    string? UpdatedBy);
