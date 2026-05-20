using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Pagination;

namespace ZelosHR.Api.Entities.Recruitment;

public class RecruitmentService
{
    private readonly IRecruitmentRepository _recruitment;

    public RecruitmentService(IRecruitmentRepository recruitment) => _recruitment = recruitment;

    public async Task<Respons<RecruitmentSummaryDto>> GetSummaryAsync(
        string tenantId, string orgId, CancellationToken ct = default)
    {
        var summary = await _recruitment.GetSummaryScopedAsync(tenantId, orgId, ct);
        return Respons<RecruitmentSummaryDto>.Ok(summary);
    }

    public async Task<Respons<JobPostingListDto>> ListAsync(
        string? search, string? status, int page, int size, string tenantId, string orgId, CancellationToken ct)
    {
        var paging = PagedQuery.From(page, size);
        var (items, total) = await _recruitment.ListScopedAsync(
            tenantId, orgId, search, status, paging.Page, paging.Size, ct);
        var summary = await _recruitment.GetSummaryScopedAsync(tenantId, orgId, ct);

        return Respons<JobPostingListDto>.Ok(
            new JobPostingListDto { Summary = summary, Items = items },
            pagination: new PaginationMeta
            {
                Page = paging.Page,
                Size = paging.Size,
                Total = total,
                HasNext = paging.Offset + items.Count < total,
            });
    }

    public Task<Respons<JobPostingListItemDto>> GetByIdAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default) =>
        QueryOneAsync(id, tenantId, orgId, ct);

    public async Task<Respons<JobPostingListItemDto>> CreateAsync(
        CreateJobPostingDto data, string tenantId, string orgId, CancellationToken ct = default)
    {
        var status = string.IsNullOrWhiteSpace(data.Status) ? "Open" : data.Status.Trim();
        var postedAt = data.PostedAt ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var id = await _recruitment.CreateScopedAsync(
            tenantId,
            orgId,
            data.Title!.Trim(),
            string.IsNullOrWhiteSpace(data.DepartmentName) ? null : data.DepartmentName.Trim(),
            string.IsNullOrWhiteSpace(data.BranchName) ? null : data.BranchName.Trim(),
            string.IsNullOrWhiteSpace(data.EmploymentType) ? null : data.EmploymentType.Trim(),
            status,
            postedAt,
            data.ClosingDate,
            ct);

        return await QueryOneAsync(id, tenantId, orgId, ct);
    }

    public async Task<Respons<JobPostingListItemDto>> UpdateAsync(
        Guid id, UpdateJobPostingDto data, string tenantId, string orgId, CancellationToken ct = default)
    {
        var hasTitle = !string.IsNullOrWhiteSpace(data.Title);
        var hasDept = data.DepartmentName is not null;
        var hasBranch = data.BranchName is not null;
        var hasEmpType = data.EmploymentType is not null;
        var hasPostedAt = data.PostedAt.HasValue;
        var hasClosingDate = data.ClosingDate.HasValue;
        var hasStatus = !string.IsNullOrWhiteSpace(data.Status);
        var hasApplicants = data.ApplicantsCount.HasValue;
        if (!hasTitle && !hasDept && !hasBranch && !hasEmpType && !hasPostedAt && !hasClosingDate && !hasStatus && !hasApplicants)
            return Respons<JobPostingListItemDto>.Fail("No fields to update.", statusCode: 400);

        var updated = await _recruitment.UpdateScopedAsync(
            id,
            tenantId,
            orgId,
            hasTitle ? data.Title : null,
            hasDept ? data.DepartmentName : null,
            hasBranch ? data.BranchName : null,
            hasEmpType ? data.EmploymentType : null,
            hasPostedAt ? data.PostedAt : null,
            hasClosingDate ? data.ClosingDate : null,
            hasStatus ? data.Status : null,
            hasApplicants ? data.ApplicantsCount : null,
            ct);

        if (updated is null)
        {
            var exists = await _recruitment.GetByIdScopedAsync(id, tenantId, orgId, ct);
            return exists is null
                ? Respons<JobPostingListItemDto>.Fail("Job posting not found.", statusCode: 404)
                : Respons<JobPostingListItemDto>.Fail("No fields to update.", statusCode: 400);
        }

        return Respons<JobPostingListItemDto>.Ok(updated);
    }

    public async Task<Respons<object>> DeleteAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default)
    {
        if (!await _recruitment.DeleteScopedAsync(id, tenantId, orgId, ct))
            return Respons<object>.Fail("Job posting not found.", statusCode: 404);
        return Respons<object>.Ok(new { jobPostingId = id.ToString() }, "Job posting deleted.");
    }

    private async Task<Respons<JobPostingListItemDto>> QueryOneAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct)
    {
        var row = await _recruitment.GetByIdScopedAsync(id, tenantId, orgId, ct);
        return row is null
            ? Respons<JobPostingListItemDto>.Fail("Job posting not found.", statusCode: 404)
            : Respons<JobPostingListItemDto>.Ok(row);
    }
}
