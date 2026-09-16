namespace ZelosHR.Api.Entities.Employees;

public sealed record AttendanceRosterItem(
    Guid EmployeeId,
    string FullName,
    string? JobTitle,
    string? DepartmentName,
    string? BranchName,
    Guid? ReportsToEmployeeId);
