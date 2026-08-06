namespace ZelosHR.Api.Entities.Employees;

public sealed class CreateEmployeeControllerReadDto
{
    public required string EmployeeId { get; init; }
    public required string EmployeeCode { get; init; }
    public required string FirstName { get; init; }
    public string? MiddleName { get; init; }
    public required string LastName { get; init; }
}

public sealed class EmployeeListItemControllerReadDto
{
    public required string EmployeeId { get; init; }
    public required string EmployeeCode { get; init; }
    public required string FullName { get; init; }
}

public sealed class GetEmployeesControllerReadDto
{
    public IReadOnlyList<EmployeeListItemControllerReadDto> Items { get; init; } = [];
}

public sealed class CreateEmployeeServiceReadDto
{
    public required Guid Id { get; init; }
    public required string EmployeeCode { get; init; }
    public required string EmployeeCodeSystem { get; init; }
    public string? EmployeeCodeCustom { get; init; }
    public required string FirstName { get; init; }
    public string? MiddleName { get; init; }
    public required string LastName { get; init; }
}

public sealed class EmployeeListItemServiceReadDto
{
    public required Guid Id { get; init; }
    public required string EmployeeCode { get; init; }
    public required string FullName { get; init; }
}

public sealed class GetEmployeesServiceReadDto
{
    public IReadOnlyList<EmployeeListItemServiceReadDto> Items { get; init; } = [];
}
