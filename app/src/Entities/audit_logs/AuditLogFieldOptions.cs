namespace ZelosHR.Api.Entities.AuditLogs;

/// <summary>Allow-lists for audit-log query params and OpenAPI hints.</summary>
public static class AuditLogFieldOptions
{
    public static readonly IReadOnlyList<string> Categories =
        ["Lifecycle", "Field change", "System", "Approval"];

    public static readonly IReadOnlyList<string> Severities = ["Low", "Medium", "High"];

    public static readonly IReadOnlyList<string> FilterAll = ["all"];

    public static readonly IReadOnlyList<string> RetentionWindowDays = ["90", "180", "365"];
}

/// <summary>Retention window validation and cutoff calculation for audit log purge.</summary>
public static class AuditLogRetention
{
    public const int DefaultWindowDays = 90;

    private static readonly HashSet<int> AllowedWindowDays =
        AuditLogFieldOptions.RetentionWindowDays.Select(int.Parse).ToHashSet();

    public static bool TryResolveWindowDays(int days, out int resolved, out string? error)
    {
        if (!AllowedWindowDays.Contains(days))
        {
            resolved = DefaultWindowDays;
            error = $"retention_window must be one of: {string.Join(", ", AuditLogFieldOptions.RetentionWindowDays)} (days).";
            return false;
        }

        resolved = days;
        error = null;
        return true;
    }

    public static DateTimeOffset CutoffBefore(int retentionWindowDays) =>
        DateTimeOffset.UtcNow.AddDays(-retentionWindowDays);
}

