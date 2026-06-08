using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using ZelosHR.Api.Entities.AuditLogs;
using ZelosHR.Api.Entities.Shared;

namespace ZelosHR.Api.Configs;

/// <summary>Audit log read API OpenAPI examples and query-parameter hints.</summary>
public sealed class SwaggerAuditLogsOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var method = context.ApiDescription.HttpMethod ?? "";
        var path = context.ApiDescription.RelativePath ?? "";
        if (!path.StartsWith("api/v1/audit-logs", StringComparison.OrdinalIgnoreCase))
            return;

        if (method.Equals("GET", StringComparison.OrdinalIgnoreCase)
            && path.Equals("api/v1/audit-logs/statistics", StringComparison.OrdinalIgnoreCase))
        {
            SetJsonResponseExample(operation, 200, SwaggerExamples.AuditLogStatisticsResponse());
            operation.Summary ??= "Audit log KPIs";
            operation.Description = SwaggerOptionFormat.Append(operation.Description,
                "KPI cards: total_entries · critical_count (High severity) · flagged_count · sensitive_reads_count · unique_actors_count.");
            return;
        }

        if (method.Equals("GET", StringComparison.OrdinalIgnoreCase)
            && path.Equals("api/v1/audit-logs/list", StringComparison.OrdinalIgnoreCase))
        {
            SetJsonResponseExample(operation, 200, SwaggerExamples.AuditLogListResponse());
            operation.Summary ??= "Audit log table";
            operation.Description = SwaggerOptionFormat.Append(operation.Description,
                "Paginated audit entries with embedded summary KPIs. Each item: audit_log_id · occurred_at · action_title · action_description · employee (id/code/name) · actor_id · actor_full_name · category · severity · is_flagged. "
                + "Optional start_date / end_date filter by occurred_at (UTC day boundaries). Same filters as GET /audit-logs/export.");
            AppendAuditLogFilterParameterDescriptions(operation);
            return;
        }

        if (method.Equals("GET", StringComparison.OrdinalIgnoreCase)
            && path.Equals("api/v1/audit-logs/export", StringComparison.OrdinalIgnoreCase))
        {
            operation.Summary ??= "Export audit logs CSV";
            operation.Description = SwaggerOptionFormat.Append(operation.Description,
                $"Returns CSV with columns: {string.Join(", ", AuditLogCsvExport.Headers)}. "
                + "Accepts the same filters as GET /audit-logs/list (search, action, severity, actor, start_date, end_date).");
            AppendAuditLogFilterParameterDescriptions(operation);
            return;
        }

        if (method.Equals("DELETE", StringComparison.OrdinalIgnoreCase)
            && path.Equals("api/v1/audit-logs/purge", StringComparison.OrdinalIgnoreCase))
        {
            SetJsonResponseExample(operation, 200, SwaggerExamples.AuditLogPurgeResponse());
            operation.Summary ??= "Purge old audit logs";
            operation.Description = SwaggerOptionFormat.Append(operation.Description,
                "Hard-deletes audit log entries with occurred_at older than three months for the current tenant/org. Returns deleted_count and cutoff_before.");
            return;
        }

        if (method.Equals("GET", StringComparison.OrdinalIgnoreCase)
            && path.Equals("api/v1/audit-logs/get", StringComparison.OrdinalIgnoreCase))
        {
            SetJsonResponseExample(operation, 200, SwaggerExamples.AuditLogGetResponse());
            operation.Summary ??= "Audit log entry";
            AppendParameterDescription(operation, "audit_log_id",
                "Audit log UUID from GET /audit-logs/list.");
        }
    }

    private static void SetJsonResponseExample(OpenApiOperation operation, int statusCode, JsonObject? example)
    {
        if (example is null)
            return;
        var key = statusCode.ToString();
        if (!operation.Responses.TryGetValue(key, out var response) || response.Content is null)
            return;
        if (!response.Content.TryGetValue("application/json", out var media))
            return;
        SwaggerMediaExamples.SetSingleExample(media, example);
    }

    private static void AppendAuditLogFilterParameterDescriptions(OpenApiOperation operation)
    {
        AppendParameterDescription(operation, "search",
            "Optional text filter (min 3 chars) on actor name, employee name, or action title.");
        AppendParameterDescription(operation, "action",
            $"Filter by action title substring, or `{SwaggerExampleHints.AuditFilterAll}` for all.");
        AppendParameterDescription(operation, "severity",
            $"Filter by severity. Allowed: {SwaggerExampleHints.AuditSeverity} or `{SwaggerExampleHints.AuditFilterAll}`.");
        AppendParameterDescription(operation, "actor",
            $"Filter by actor user id or display name substring, or `{SwaggerExampleHints.AuditFilterAll}`.");
        AppendParameterDescription(operation, "start_date",
            "Activity occurred on or after this date (YYYY-MM-DD, UTC day boundary).");
        AppendParameterDescription(operation, "end_date",
            "Activity occurred on or before this date (YYYY-MM-DD, UTC day boundary).");
    }

    private static void AppendParameterDescription(OpenApiOperation operation, string name, string addition)
    {
        if (operation.Parameters is null)
            return;
        foreach (var parameter in operation.Parameters)
        {
            if (!string.Equals(parameter.Name, name, StringComparison.OrdinalIgnoreCase))
                continue;
            parameter.Description = SwaggerOptionFormat.Append(parameter.Description, addition);
            break;
        }
    }
}
