namespace ZelosHR.Api.Entities.Employees;

public sealed record EmployeeLeaveContextRow(
    Guid Id,
    string? UserId,
    string FullName,
    string EmployeeCode,
    string? JobTitle,
    Guid? DepartmentId,
    string? DepartmentName,
    Guid? ReportsToId,
    Guid? ManagerId,
    Guid? HeadOfDepartmentId,
    string? ProfilePhotoUrl);
