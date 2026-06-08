using System.Globalization;
using System.Text;

namespace ZelosHR.Api.Entities.AuditLogs;

/// <summary>CSV export for audit log entries.</summary>
public static class AuditLogCsvExport
{
    public static readonly IReadOnlyList<string> Headers =
    [
        "audit_log_id",
        "occurred_at",
        "action_title",
        "action_description",
        "employee_id",
        "employee_display_code",
        "employee_full_name",
        "actor_id",
        "actor_full_name",
        "category",
        "severity",
        "is_flagged",
    ];

    public static byte[] Build(IEnumerable<AuditLogListRow> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(',', Headers));

        foreach (var row in rows)
        {
            sb.AppendLine(string.Join(',',
                Escape(row.Id.ToString()),
                Escape(row.OccurredAt.ToString("O", CultureInfo.InvariantCulture)),
                Escape(row.ActionTitle),
                Escape(row.ActionDescription),
                Escape(row.EmployeeId?.ToString()),
                Escape(row.EmployeeDisplayCode),
                Escape(row.EmployeeFullName),
                Escape(row.ActorId),
                Escape(row.ActorFullName),
                Escape(row.Category),
                Escape(row.Severity),
                Escape(row.IsFlagged ? "true" : "false")));
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        var needsQuotes = value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r');
        if (!needsQuotes)
            return value;

        return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }
}
