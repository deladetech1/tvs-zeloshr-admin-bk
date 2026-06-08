namespace ZelosHR.Api.Entities.AuditLogs;

/// <summary>Allow-lists for audit-log query params and OpenAPI hints.</summary>
public static class AuditLogFieldOptions
{
    public static readonly IReadOnlyList<string> Categories =
        ["Lifecycle", "Field change", "System", "Approval"];

    public static readonly IReadOnlyList<string> Severities = ["Low", "Medium", "High"];

    public static readonly IReadOnlyList<string> FilterAll = ["all"];
}
