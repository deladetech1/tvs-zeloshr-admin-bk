namespace ZelosHR.Api.Entities.Employees;

public static class EmployeeLifecycleStates
{
    public const string PreHire = "Pre-hire";
    public const string Active = "Active";
    public const string OnLeave = "On Leave";
    public const string Suspended = "Suspended";
    public const string Resigned = "Resigned";
    public const string Terminated = "Terminated";
}

public sealed class EmployeeRecord
{
    public Guid Id { get; init; }
    public required string EmployeeCode { get; init; }
    public required string FirstName { get; init; }
    public string? MiddleName { get; init; }
    public required string LastName { get; init; }
    public DateOnly DateOfBirth { get; init; }
    public required string Gender { get; init; }
    public required string Nationality { get; init; }
    public required string GhanaCardNumber { get; init; }
    public required string PersonalEmail { get; init; }
    public required string PersonalPhone { get; init; }
    public required string ResidentialAddress { get; init; }
    public required string GhanaPostGps { get; init; }
    public required string LifecycleState { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
