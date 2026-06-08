namespace ZelosHR.Api.Entities.AuditLogs;

public sealed class AuditLogSummaryDto
{
    public int TotalEntries { get; init; }
    public int CriticalCount { get; init; }
    public int FlaggedCount { get; init; }
    public int SensitiveReadsCount { get; init; }
    public int UniqueActorsCount { get; init; }
}

public sealed class AuditLogEmployeeRefDto
{
    public string? EmployeeId { get; init; }
    public string? EmployeeDisplayCode { get; init; }
    public string? EmployeeFullName { get; init; }
}

public sealed class AuditLogListItemDto
{
    public required string AuditLogId { get; init; }
    public DateTimeOffset OccurredAt { get; init; }
    public required string ActionTitle { get; init; }
    public string? ActionDescription { get; init; }
    public AuditLogEmployeeRefDto? Employee { get; init; }
    public string? ActorId { get; init; }
    public required string ActorFullName { get; init; }
    public required string Category { get; init; }
    public required string Severity { get; init; }
    public bool IsFlagged { get; init; }
}

public sealed class AuditLogListDto
{
    public AuditLogSummaryDto Summary { get; init; } = new();
    public IReadOnlyList<AuditLogListItemDto> Items { get; init; } = [];
}
