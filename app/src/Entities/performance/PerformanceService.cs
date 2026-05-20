using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Formatting;
using ZelosHR.Api.Shared.Pagination;

namespace ZelosHR.Api.Entities.Performance;

public class PerformanceService
{
    private readonly IPerformanceRepository _performance;
    private readonly IEmployeeRepository _employees;

    public PerformanceService(IPerformanceRepository performance, IEmployeeRepository employees)
    {
        _performance = performance;
        _employees = employees;
    }

    public async Task<Respons<PerformanceSummaryDto>> GetSummaryAsync(
        string tenantId, string orgId, CancellationToken ct)
    {
        var summary = await _performance.GetSummaryScopedAsync(tenantId, orgId, ct);
        return Respons<PerformanceSummaryDto>.Ok(summary);
    }

    public async Task<Respons<PerformanceListDto>> ListAsync(
        string? search, string? status, int page, int size, string tenantId, string orgId, CancellationToken ct)
    {
        var paging = PagedQuery.From(page, size);
        var (items, total) = await _performance.ListScopedAsync(
            tenantId, orgId, search, status, paging.Page, paging.Size, ct);
        var summary = await _performance.GetSummaryScopedAsync(tenantId, orgId, ct);

        return Respons<PerformanceListDto>.Ok(
            new PerformanceListDto { Summary = summary, Items = items },
            pagination: new PaginationMeta
            {
                Page = paging.Page,
                Size = paging.Size,
                Total = total,
                HasNext = paging.Offset + items.Count < total,
            });
    }

    public Task<Respons<PerformanceReviewListItemDto>> GetByIdAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default) =>
        QueryOneAsync(id, tenantId, orgId, ct);

    public async Task<Respons<PerformanceReviewListItemDto>> CreateAsync(
        CreatePerformanceReviewDto data, string tenantId, string orgId, CancellationToken ct = default)
    {
        var emp = await _employees.GetByIdScopedAsync(data.EmployeeId, tenantId, orgId, ct);
        if (emp is null)
            return Respons<PerformanceReviewListItemDto>.ValidationError(
                new Dictionary<string, string> { ["employeeId"] = "Employee not found." });

        var status = string.IsNullOrWhiteSpace(data.Status) ? "Pending" : data.Status.Trim();
        var fullName = NameFormatting.BuildFullName(emp.FirstName, emp.MiddleName, emp.LastName);

        var id = await _performance.CreateScopedAsync(
            tenantId,
            orgId,
            data.EmployeeId,
            fullName,
            data.ReviewPeriod!.Trim(),
            string.IsNullOrWhiteSpace(data.ReviewerName) ? null : data.ReviewerName.Trim(),
            status,
            data.DueDate,
            ct);

        return await QueryOneAsync(id, tenantId, orgId, ct);
    }

    public async Task<Respons<PerformanceReviewListItemDto>> UpdateAsync(
        Guid id, UpdatePerformanceReviewDto data, string tenantId, string orgId, CancellationToken ct = default)
    {
        var hasReviewPeriod = !string.IsNullOrWhiteSpace(data.ReviewPeriod);
        var hasDueDate = data.DueDate.HasValue;
        var hasReviewer = data.ReviewerName is not null;
        var hasRating = data.OverallRating is not null;
        var hasStatus = !string.IsNullOrWhiteSpace(data.Status);
        if (!hasReviewPeriod && !hasDueDate && !hasReviewer && !hasRating && !hasStatus)
            return Respons<PerformanceReviewListItemDto>.Fail("No fields to update.", statusCode: 400);

        var updated = await _performance.UpdateScopedAsync(
            id,
            tenantId,
            orgId,
            hasReviewPeriod ? data.ReviewPeriod : null,
            hasDueDate ? data.DueDate : null,
            hasReviewer ? data.ReviewerName : null,
            hasRating ? data.OverallRating : null,
            hasStatus ? data.Status : null,
            ct);

        if (updated is null)
        {
            var exists = await _performance.GetByIdScopedAsync(id, tenantId, orgId, ct);
            return exists is null
                ? Respons<PerformanceReviewListItemDto>.Fail("Performance review not found.", statusCode: 404)
                : Respons<PerformanceReviewListItemDto>.Fail("No fields to update.", statusCode: 400);
        }

        return Respons<PerformanceReviewListItemDto>.Ok(updated);
    }

    public async Task<Respons<object>> DeleteAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default)
    {
        if (!await _performance.DeleteScopedAsync(id, tenantId, orgId, ct))
            return Respons<object>.Fail("Performance review not found.", statusCode: 404);
        return Respons<object>.Ok(new { reviewId = id.ToString() }, "Performance review deleted.");
    }

    private async Task<Respons<PerformanceReviewListItemDto>> QueryOneAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct)
    {
        var row = await _performance.GetByIdScopedAsync(id, tenantId, orgId, ct);
        return row is null
            ? Respons<PerformanceReviewListItemDto>.Fail("Performance review not found.", statusCode: 404)
            : Respons<PerformanceReviewListItemDto>.Ok(row);
    }
}
