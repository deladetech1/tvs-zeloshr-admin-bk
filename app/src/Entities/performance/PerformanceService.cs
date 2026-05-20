using Dapper;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Pagination;

namespace ZelosHR.Api.Entities.Performance;

public class PerformanceService
{
    private readonly IDatabaseManager _database;
    private readonly EmployeesService _employees;

    public PerformanceService(IDatabaseManager database, EmployeesService employees)
    {
        _database = database;
        _employees = employees;
    }

    public async Task<Respons<PerformanceSummaryDto>> GetSummaryAsync(string tenantId, string orgId, CancellationToken ct)
    {
        await using var c = await _database.GetConnectionAsync(ct);
        var s = await c.QuerySingleAsync<PerformanceSummaryDto>(
            """
            SELECT
                COUNT(*) FILTER (WHERE status = 'Pending')::int AS Pending,
                COUNT(*) FILTER (WHERE status = 'In progress')::int AS InProgress,
                COUNT(*) FILTER (WHERE status = 'Completed')::int AS Completed,
                COUNT(*) FILTER (WHERE status != 'Completed' AND due_date < CURRENT_DATE)::int AS Overdue
            FROM zeloshr.zhr_performance_reviews WHERE tenant_id = @TenantId AND org_id = @OrgId
            """, new { TenantId = tenantId, OrgId = orgId });
        return Respons<PerformanceSummaryDto>.Ok(s);
    }

    public async Task<Respons<PerformanceListDto>> ListAsync(
        string? search, string? status, int page, int size, string tenantId, string orgId, CancellationToken ct)
    {
        var paging = PagedQuery.From(page, size);
        await using var c = await _database.GetConnectionAsync(ct);
        var conditions = new List<string> { "tenant_id = @TenantId", "org_id = @OrgId" };
        var p = new DynamicParameters(new { TenantId = tenantId, OrgId = orgId });
        if (!string.IsNullOrWhiteSpace(search) && search.Length >= 3)
        { conditions.Add("employee_full_name ILIKE @Search"); p.Add("Search", $"%{search}%"); }
        if (!string.IsNullOrWhiteSpace(status) && status != "all") { conditions.Add("status = @Status"); p.Add("Status", status); }
        var where = string.Join(" AND ", conditions);
        p.Add("Limit", paging.Size); p.Add("Offset", paging.Offset);
        var total = await c.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM zeloshr.zhr_performance_reviews WHERE {where}", p);
        var items = (await c.QueryAsync<PerformanceReviewListItemDto>(
            $"""
            SELECT id::text AS ReviewId, employee_id::text AS EmployeeId, employee_full_name AS EmployeeFullName,
                   review_period AS ReviewPeriod, reviewer_name AS ReviewerName, overall_rating AS OverallRating,
                   status AS Status, due_date AS DueDate
            FROM zeloshr.zhr_performance_reviews WHERE {where} ORDER BY due_date LIMIT @Limit OFFSET @Offset
            """, p)).ToList();
        var summary = (await GetSummaryAsync(tenantId, orgId, ct)).Data ?? new PerformanceSummaryDto();
        return Respons<PerformanceListDto>.Ok(new PerformanceListDto { Summary = summary, Items = items },
            pagination: new PaginationMeta { Page = paging.Page, Size = paging.Size, Total = total, HasNext = paging.Offset + items.Count < total });
    }

    public async Task<Respons<PerformanceReviewListItemDto>> GetByIdAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default) =>
        await QueryOneAsync(id, tenantId, orgId, ct);

    public async Task<Respons<PerformanceReviewListItemDto>> CreateAsync(
        CreatePerformanceReviewDto data, string tenantId, string orgId, CancellationToken ct = default)
    {
        var emp = await _employees.ResolveEmployeeDisplayAsync(data.EmployeeId, tenantId, orgId, ct);
        if (emp is null)
            return Respons<PerformanceReviewListItemDto>.ValidationError(
                new Dictionary<string, string> { ["employeeId"] = "Employee not found." });

        var status = string.IsNullOrWhiteSpace(data.Status) ? "Pending" : data.Status.Trim();

        await using var c = await _database.GetConnectionAsync(ct);
        var id = await c.QuerySingleAsync<Guid>(
            """
            INSERT INTO zeloshr.zhr_performance_reviews (
                tenant_id, org_id, employee_id, employee_full_name, review_period, reviewer_name, status, due_date
            )
            VALUES (
                @TenantId, @OrgId, @EmployeeId, @FullName, @ReviewPeriod, @Reviewer, @Status, @DueDate
            )
            RETURNING id
            """,
            new
            {
                TenantId = tenantId,
                OrgId = orgId,
                EmployeeId = data.EmployeeId,
                FullName = emp.FullName,
                ReviewPeriod = data.ReviewPeriod!.Trim(),
                Reviewer = string.IsNullOrWhiteSpace(data.ReviewerName) ? null : data.ReviewerName.Trim(),
                Status = status,
                data.DueDate,
            });

        return await QueryOneAsync(id, tenantId, orgId, ct);
    }

    public async Task<Respons<PerformanceReviewListItemDto>> UpdateAsync(
        Guid id, UpdatePerformanceReviewDto data, string tenantId, string orgId, CancellationToken ct = default)
    {
        var sets = new List<string>();
        var p = new DynamicParameters(new { Id = id, TenantId = tenantId, OrgId = orgId });
        if (!string.IsNullOrWhiteSpace(data.ReviewPeriod)) { sets.Add("review_period = @ReviewPeriod"); p.Add("ReviewPeriod", data.ReviewPeriod.Trim()); }
        if (data.DueDate.HasValue) { sets.Add("due_date = @DueDate"); p.Add("DueDate", data.DueDate.Value); }
        if (data.ReviewerName is not null) { sets.Add("reviewer_name = @Reviewer"); p.Add("Reviewer", string.IsNullOrWhiteSpace(data.ReviewerName) ? null : data.ReviewerName.Trim()); }
        if (data.OverallRating is not null) { sets.Add("overall_rating = @Rating"); p.Add("Rating", string.IsNullOrWhiteSpace(data.OverallRating) ? null : data.OverallRating.Trim()); }
        if (!string.IsNullOrWhiteSpace(data.Status)) { sets.Add("status = @Status"); p.Add("Status", data.Status.Trim()); }
        if (sets.Count == 0)
            return Respons<PerformanceReviewListItemDto>.Fail("No fields to update.", statusCode: 400);

        await using var c = await _database.GetConnectionAsync(ct);
        if (await c.ExecuteAsync(
                $"UPDATE zeloshr.zhr_performance_reviews SET {string.Join(", ", sets)} WHERE id = @Id AND tenant_id = @TenantId AND org_id = @OrgId", p) == 0)
            return Respons<PerformanceReviewListItemDto>.Fail("Performance review not found.", statusCode: 404);

        return await QueryOneAsync(id, tenantId, orgId, ct);
    }

    public async Task<Respons<object>> DeleteAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default)
    {
        await using var c = await _database.GetConnectionAsync(ct);
        var n = await c.ExecuteAsync(
            "DELETE FROM zeloshr.zhr_performance_reviews WHERE id = @Id AND tenant_id = @TenantId AND org_id = @OrgId",
            new { Id = id, TenantId = tenantId, OrgId = orgId });
        return n == 0
            ? Respons<object>.Fail("Performance review not found.", statusCode: 404)
            : Respons<object>.Ok(new { reviewId = id.ToString() }, "Performance review deleted.");
    }

    private async Task<Respons<PerformanceReviewListItemDto>> QueryOneAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct)
    {
        await using var c = await _database.GetConnectionAsync(ct);
        var row = await c.QuerySingleOrDefaultAsync<PerformanceReviewListItemDto>(
            """
            SELECT id::text AS ReviewId, employee_id::text AS EmployeeId, employee_full_name AS EmployeeFullName,
                   review_period AS ReviewPeriod, reviewer_name AS ReviewerName, overall_rating AS OverallRating,
                   status AS Status, due_date AS DueDate
            FROM zeloshr.zhr_performance_reviews
            WHERE id = @Id AND tenant_id = @TenantId AND org_id = @OrgId
            """,
            new { Id = id, TenantId = tenantId, OrgId = orgId });
        return row is null
            ? Respons<PerformanceReviewListItemDto>.Fail("Performance review not found.", statusCode: 404)
            : Respons<PerformanceReviewListItemDto>.Ok(row);
    }
}
