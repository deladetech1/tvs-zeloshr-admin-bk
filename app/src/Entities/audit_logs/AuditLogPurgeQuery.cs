using Microsoft.AspNetCore.Mvc;
using ZelosHR.Api.Configs;

namespace ZelosHR.Api.Entities.AuditLogs;

/// <summary>Query for audit log purge and purge preview.</summary>
public sealed class AuditLogPurgeQuery
{
    /// <summary>Delete entries with <c>occurred_at</c> older than this many days. Allowed: 90, 180, 365.</summary>
    [FromQuery(Name = "retention_window")]
    [SwaggerAllowedValues(typeof(AuditLogFieldOptions), nameof(AuditLogFieldOptions.RetentionWindowDays),
        Description = "Retention window in days before entries are eligible for purge.")]
    public int RetentionWindow { get; init; } = AuditLogRetention.DefaultWindowDays;
}
