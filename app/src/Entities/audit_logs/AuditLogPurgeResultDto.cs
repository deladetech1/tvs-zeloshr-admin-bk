namespace ZelosHR.Api.Entities.AuditLogs;

public sealed class AuditLogPurgeResultDto
{
    public int DeletedCount { get; init; }

    /// <summary>Entries with <c>occurred_at</c> strictly before this instant were removed.</summary>
    public DateTimeOffset CutoffBefore { get; init; }
}
