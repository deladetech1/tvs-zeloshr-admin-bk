using Dapper;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Pagination;

namespace ZelosHR.Api.Entities.Recruitment;

public class RecruitmentService
{
    private readonly IDatabaseManager _database;

    public RecruitmentService(IDatabaseManager database) => _database = database;

    public async Task<Respons<RecruitmentSummaryDto>> GetSummaryAsync(string tenantId, string orgId, CancellationToken ct)
    {
        await using var c = await _database.GetConnectionAsync(ct);
        var s = await c.QuerySingleAsync<RecruitmentSummaryDto>(
            """
            SELECT
                COUNT(*) FILTER (WHERE status = 'Open')::int AS OpenPostings,
                COALESCE(SUM(applicants_count) FILTER (WHERE status = 'Open'), 0)::int AS TotalApplicants,
                COUNT(*) FILTER (WHERE status = 'Open' AND closing_date <= CURRENT_DATE + 7)::int AS ClosingSoon,
                COUNT(*) FILTER (WHERE status = 'Closed')::int AS ClosedPostings
            FROM zeloshr.zhr_job_postings WHERE tenant_id = @TenantId AND org_id = @OrgId
            """, new { TenantId = tenantId, OrgId = orgId });
        return Respons<RecruitmentSummaryDto>.Ok(s);
    }

    public async Task<Respons<JobPostingListDto>> ListAsync(
        string? search, string? status, int page, int size, string tenantId, string orgId, CancellationToken ct)
    {
        var paging = PagedQuery.From(page, size);
        await using var c = await _database.GetConnectionAsync(ct);
        var conditions = new List<string> { "tenant_id = @TenantId", "org_id = @OrgId" };
        var p = new DynamicParameters(new { TenantId = tenantId, OrgId = orgId });
        if (!string.IsNullOrWhiteSpace(search) && search.Length >= 3) { conditions.Add("title ILIKE @Search"); p.Add("Search", $"%{search}%"); }
        if (!string.IsNullOrWhiteSpace(status) && status != "all") { conditions.Add("status = @Status"); p.Add("Status", status); }
        var where = string.Join(" AND ", conditions);
        p.Add("Limit", paging.Size); p.Add("Offset", paging.Offset);
        var total = await c.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM zeloshr.zhr_job_postings WHERE {where}", p);
        var items = (await c.QueryAsync<JobPostingListItemDto>(
            $"""
            SELECT id::text AS JobPostingId, title AS Title, department_name AS DepartmentName,
                   branch_name AS BranchName, employment_type AS EmploymentType, status AS Status,
                   applicants_count AS ApplicantsCount, posted_at AS PostedAt, closing_date AS ClosingDate
            FROM zeloshr.zhr_job_postings WHERE {where} ORDER BY posted_at DESC LIMIT @Limit OFFSET @Offset
            """, p)).ToList();
        var summary = (await GetSummaryAsync(tenantId, orgId, ct)).Data ?? new RecruitmentSummaryDto();
        return Respons<JobPostingListDto>.Ok(new JobPostingListDto { Summary = summary, Items = items },
            pagination: new PaginationMeta { Page = paging.Page, Size = paging.Size, Total = total, HasNext = paging.Offset + items.Count < total });
    }

    public async Task<Respons<JobPostingListItemDto>> GetByIdAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default) =>
        await QueryOneAsync(id, tenantId, orgId, ct);

    public async Task<Respons<JobPostingListItemDto>> CreateAsync(
        CreateJobPostingDto data, string tenantId, string orgId, CancellationToken ct = default)
    {
        var status = string.IsNullOrWhiteSpace(data.Status) ? "Open" : data.Status.Trim();
        var postedAt = data.PostedAt ?? DateOnly.FromDateTime(DateTime.UtcNow);

        await using var c = await _database.GetConnectionAsync(ct);
        var id = await c.QuerySingleAsync<Guid>(
            """
            INSERT INTO zeloshr.zhr_job_postings (
                tenant_id, org_id, title, department_name, branch_name, employment_type,
                status, applicants_count, posted_at, closing_date
            )
            VALUES (
                @TenantId, @OrgId, @Title, @Dept, @Branch, @EmpType,
                @Status, 0, @PostedAt, @ClosingDate
            )
            RETURNING id
            """,
            new
            {
                TenantId = tenantId,
                OrgId = orgId,
                Title = data.Title!.Trim(),
                Dept = string.IsNullOrWhiteSpace(data.DepartmentName) ? null : data.DepartmentName.Trim(),
                Branch = string.IsNullOrWhiteSpace(data.BranchName) ? null : data.BranchName.Trim(),
                EmpType = string.IsNullOrWhiteSpace(data.EmploymentType) ? null : data.EmploymentType.Trim(),
                Status = status,
                PostedAt = postedAt,
                data.ClosingDate,
            });

        return await QueryOneAsync(id, tenantId, orgId, ct);
    }

    public async Task<Respons<JobPostingListItemDto>> UpdateAsync(
        Guid id, UpdateJobPostingDto data, string tenantId, string orgId, CancellationToken ct = default)
    {
        var sets = new List<string>();
        var p = new DynamicParameters(new { Id = id, TenantId = tenantId, OrgId = orgId });
        if (!string.IsNullOrWhiteSpace(data.Title)) { sets.Add("title = @Title"); p.Add("Title", data.Title.Trim()); }
        if (data.DepartmentName is not null) { sets.Add("department_name = @Dept"); p.Add("Dept", string.IsNullOrWhiteSpace(data.DepartmentName) ? null : data.DepartmentName.Trim()); }
        if (data.BranchName is not null) { sets.Add("branch_name = @Branch"); p.Add("Branch", string.IsNullOrWhiteSpace(data.BranchName) ? null : data.BranchName.Trim()); }
        if (data.EmploymentType is not null) { sets.Add("employment_type = @EmpType"); p.Add("EmpType", string.IsNullOrWhiteSpace(data.EmploymentType) ? null : data.EmploymentType.Trim()); }
        if (data.PostedAt.HasValue) { sets.Add("posted_at = @PostedAt"); p.Add("PostedAt", data.PostedAt.Value); }
        if (data.ClosingDate.HasValue) { sets.Add("closing_date = @ClosingDate"); p.Add("ClosingDate", data.ClosingDate); }
        if (!string.IsNullOrWhiteSpace(data.Status)) { sets.Add("status = @Status"); p.Add("Status", data.Status.Trim()); }
        if (data.ApplicantsCount.HasValue) { sets.Add("applicants_count = @Applicants"); p.Add("Applicants", data.ApplicantsCount.Value); }
        if (sets.Count == 0)
            return Respons<JobPostingListItemDto>.Fail("No fields to update.", statusCode: 400);

        await using var c = await _database.GetConnectionAsync(ct);
        if (await c.ExecuteAsync(
                $"UPDATE zeloshr.zhr_job_postings SET {string.Join(", ", sets)} WHERE id = @Id AND tenant_id = @TenantId AND org_id = @OrgId", p) == 0)
            return Respons<JobPostingListItemDto>.Fail("Job posting not found.", statusCode: 404);

        return await QueryOneAsync(id, tenantId, orgId, ct);
    }

    public async Task<Respons<object>> DeleteAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default)
    {
        await using var c = await _database.GetConnectionAsync(ct);
        var n = await c.ExecuteAsync(
            "DELETE FROM zeloshr.zhr_job_postings WHERE id = @Id AND tenant_id = @TenantId AND org_id = @OrgId",
            new { Id = id, TenantId = tenantId, OrgId = orgId });
        return n == 0
            ? Respons<object>.Fail("Job posting not found.", statusCode: 404)
            : Respons<object>.Ok(new { jobPostingId = id.ToString() }, "Job posting deleted.");
    }

    private async Task<Respons<JobPostingListItemDto>> QueryOneAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct)
    {
        await using var c = await _database.GetConnectionAsync(ct);
        var row = await c.QuerySingleOrDefaultAsync<JobPostingListItemDto>(
            """
            SELECT id::text AS JobPostingId, title AS Title, department_name AS DepartmentName,
                   branch_name AS BranchName, employment_type AS EmploymentType, status AS Status,
                   applicants_count AS ApplicantsCount, posted_at AS PostedAt, closing_date AS ClosingDate
            FROM zeloshr.zhr_job_postings
            WHERE id = @Id AND tenant_id = @TenantId AND org_id = @OrgId
            """,
            new { Id = id, TenantId = tenantId, OrgId = orgId });
        return row is null
            ? Respons<JobPostingListItemDto>.Fail("Job posting not found.", statusCode: 404)
            : Respons<JobPostingListItemDto>.Ok(row);
    }
}
