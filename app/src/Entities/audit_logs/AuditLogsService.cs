using Dapper;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Pagination;

namespace ZelosHR.Api.Entities.AuditLogs;

public class AuditLogsService
{
    private readonly IDatabaseManager _database;

    public AuditLogsService(IDatabaseManager database) => _database = database;

    public async Task<Respons<AuditLogSummaryDto>> GetSummaryAsync(
        string tenantId,
        string orgId,
        CancellationToken ct = default)
    {
        await using var connection = await _database.GetConnectionAsync(ct);

        var summary = await connection.QuerySingleAsync<AuditLogSummaryDto>(
            """
            SELECT
                COUNT(*)::int AS TotalEntries,
                COUNT(*) FILTER (WHERE severity = 'High')::int AS CriticalCount,
                COUNT(*) FILTER (WHERE is_flagged = TRUE)::int AS FlaggedCount,
                COUNT(*) FILTER (WHERE is_sensitive_read = TRUE)::int AS SensitiveReadsCount,
                COUNT(DISTINCT actor_id)::int AS UniqueActorsCount
            FROM zeloshr.zhr_audit_logs
            WHERE tenant_id = @TenantId AND org_id = @OrgId
            """,
            new { TenantId = tenantId, OrgId = orgId });

        return Respons<AuditLogSummaryDto>.Ok(summary);
    }

    public async Task<Respons<AuditLogListDto>> ListAsync(
        string? search,
        string? action,
        string? severity,
        string? actor,
        int page,
        int size,
        string tenantId,
        string orgId,
        CancellationToken ct = default)
    {
        var paging = PagedQuery.From(page, size);
        await using var connection = await _database.GetConnectionAsync(ct);

        var conditions = new List<string> { "tenant_id = @TenantId", "org_id = @OrgId" };
        var parameters = new DynamicParameters(new { TenantId = tenantId, OrgId = orgId });

        if (!string.IsNullOrWhiteSpace(search) && search.Trim().Length >= 3)
        {
            conditions.Add(
                "(actor_full_name ILIKE @Search OR employee_full_name ILIKE @Search OR action_title ILIKE @Search)");
            parameters.Add("Search", $"%{search.Trim()}%");
        }

        if (!string.IsNullOrWhiteSpace(action) && !action.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            conditions.Add("action_title ILIKE @Action");
            parameters.Add("Action", $"%{action.Trim()}%");
        }

        if (!string.IsNullOrWhiteSpace(severity) && !severity.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            conditions.Add("severity = @Severity");
            parameters.Add("Severity", severity.Trim());
        }

        if (!string.IsNullOrWhiteSpace(actor) && !actor.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            conditions.Add("actor_full_name ILIKE @Actor");
            parameters.Add("Actor", $"%{actor.Trim()}%");
        }

        var where = string.Join(" AND ", conditions);
        parameters.Add("Limit", paging.Size);
        parameters.Add("Offset", paging.Offset);

        var total = await connection.ExecuteScalarAsync<int>(
            $"SELECT COUNT(*)::int FROM zeloshr.zhr_audit_logs WHERE {where}",
            parameters);

        var rows = await connection.QueryAsync<AuditLogRow>(
            $"""
            SELECT
                id AS Id,
                occurred_at AS OccurredAt,
                action_title AS ActionTitle,
                action_description AS ActionDescription,
                employee_id AS EmployeeId,
                employee_display_code AS EmployeeDisplayCode,
                employee_full_name AS EmployeeFullName,
                actor_full_name AS ActorFullName,
                category AS Category,
                severity AS Severity,
                is_flagged AS IsFlagged
            FROM zeloshr.zhr_audit_logs
            WHERE {where}
            ORDER BY occurred_at DESC
            LIMIT @Limit OFFSET @Offset
            """,
            parameters);

        var items = rows.Select(MapRow).ToList();

        var summary = (await GetSummaryAsync(tenantId, orgId, ct)).Data ?? new AuditLogSummaryDto();

        return Respons<AuditLogListDto>.Ok(
            new AuditLogListDto { Summary = summary, Items = items },
            pagination: new PaginationMeta
            {
                Page = paging.Page,
                Size = paging.Size,
                Total = total,
                HasNext = paging.Offset + items.Count < total,
            });
    }

    public async Task<Respons<AuditLogListItemDto>> GetByIdAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default)
    {
        await using var connection = await _database.GetConnectionAsync(ct);
        var r = await connection.QuerySingleOrDefaultAsync<AuditLogRow>(
            """
            SELECT
                id AS Id,
                occurred_at AS OccurredAt,
                action_title AS ActionTitle,
                action_description AS ActionDescription,
                employee_id AS EmployeeId,
                employee_display_code AS EmployeeDisplayCode,
                employee_full_name AS EmployeeFullName,
                actor_full_name AS ActorFullName,
                category AS Category,
                severity AS Severity,
                is_flagged AS IsFlagged
            FROM zeloshr.zhr_audit_logs
            WHERE id = @Id AND tenant_id = @TenantId AND org_id = @OrgId
            """,
            new { Id = id, TenantId = tenantId, OrgId = orgId });

        if (r is null)
            return Respons<AuditLogListItemDto>.Fail("Audit log entry not found.", statusCode: 404);

        return Respons<AuditLogListItemDto>.Ok(MapRow(r));
    }

    private static AuditLogListItemDto MapRow(AuditLogRow r) => new()
    {
        AuditLogId = r.Id.ToString(),
        OccurredAt = r.OccurredAt,
        ActionTitle = r.ActionTitle,
        ActionDescription = r.ActionDescription,
        Employee = string.IsNullOrWhiteSpace(r.EmployeeFullName)
            ? null
            : new AuditLogEmployeeRefDto
            {
                EmployeeId = r.EmployeeId?.ToString(),
                EmployeeDisplayCode = r.EmployeeDisplayCode,
                EmployeeFullName = r.EmployeeFullName,
            },
        ActorFullName = r.ActorFullName,
        Category = r.Category,
        Severity = r.Severity,
        IsFlagged = r.IsFlagged,
    };

    private sealed class AuditLogRow
    {
        public Guid Id { get; init; }
        public DateTimeOffset OccurredAt { get; init; }
        public required string ActionTitle { get; init; }
        public string? ActionDescription { get; init; }
        public Guid? EmployeeId { get; init; }
        public string? EmployeeDisplayCode { get; init; }
        public string? EmployeeFullName { get; init; }
        public required string ActorFullName { get; init; }
        public required string Category { get; init; }
        public required string Severity { get; init; }
        public bool IsFlagged { get; init; }
    }
}
