namespace ZelosHR.Api.Entities.Disciplinary;

public sealed class DisciplinarySummaryDto
{
    public int OpenCases { get; init; }
    public int HighSeverity { get; init; }
    public int ClosedCases { get; init; }
    public int TotalCases { get; init; }
}

public sealed class DisciplinaryCaseListItemDto
{
    public required string CaseId { get; init; }
    public required string EmployeeId { get; init; }
    public required string EmployeeFullName { get; init; }
    public required string CaseType { get; init; }
    public required string Severity { get; init; }
    public required string Status { get; init; }
    public DateOnly OpenedAt { get; init; }
    public string? Description { get; init; }
}

public sealed class DisciplinaryListDto
{
    public DisciplinarySummaryDto Summary { get; init; } = new();
    public IReadOnlyList<DisciplinaryCaseListItemDto> Items { get; init; } = [];
}
