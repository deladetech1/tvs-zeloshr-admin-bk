namespace ZelosHR.Api.Entities.AuditLogs;

public sealed class AuditLogPurgeResultDto
{
    public int DeletedCount { get; init; }

    /// <summary>Retention window applied (days).</summary>
    public int RetentionWindow { get; init; }

    /// <summary>Entries with <c>occurred_at</c> strictly before this instant were removed.</summary>
    public DateTimeOffset CutoffBefore { get; init; }
}

public sealed class AuditLogPurgePreviewDto
{
    /// <summary>Entries that would be deleted for the given retention window.</summary>
    public int EligibleCount { get; init; }

    public int RetentionWindow { get; init; }

    public DateTimeOffset CutoffBefore { get; init; }
}
