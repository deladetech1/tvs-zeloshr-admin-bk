using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Formatting;
using ZelosHR.Api.Shared.Pagination;

namespace ZelosHR.Api.Entities.Onboarding;

public class OnboardingService
{
    private readonly IOnboardingRepository _onboarding;
    private readonly IEmployeeRepository _employees;

    public OnboardingService(IOnboardingRepository onboarding, IEmployeeRepository employees)
    {
        _onboarding = onboarding;
        _employees = employees;
    }

    public async Task<Respons<OnboardingSummaryDto>> GetSummaryAsync(
        string tenantId, string orgId, CancellationToken ct)
    {
        var summary = await _onboarding.GetSummaryScopedAsync(tenantId, orgId, ct);
        return Respons<OnboardingSummaryDto>.Ok(summary);
    }

    public async Task<Respons<OnboardingListDto>> ListAsync(
        string? search, string? status, int page, int size, string tenantId, string orgId, CancellationToken ct)
    {
        var paging = PagedQuery.From(page, size);
        var (items, total) = await _onboarding.ListScopedAsync(
            tenantId, orgId, search, status, paging.Page, paging.Size, ct);
        var summary = await _onboarding.GetSummaryScopedAsync(tenantId, orgId, ct);

        return Respons<OnboardingListDto>.Ok(
            new OnboardingListDto { Summary = summary, Items = items },
            pagination: new PaginationMeta
            {
                Page = paging.Page,
                Size = paging.Size,
                Total = total,
                HasNext = paging.Offset + items.Count < total,
            });
    }

    public Task<Respons<OnboardingTaskListItemDto>> GetByIdAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default) =>
        QueryOneAsync(id, tenantId, orgId, ct);

    public async Task<Respons<OnboardingTaskListItemDto>> CreateAsync(
        CreateOnboardingTaskDto data, string tenantId, string orgId, CancellationToken ct = default)
    {
        var emp = await _employees.GetByIdScopedAsync(data.EmployeeId, tenantId, orgId, ct);
        if (emp is null)
            return Respons<OnboardingTaskListItemDto>.ValidationError(
                new Dictionary<string, string> { ["employeeId"] = "Employee not found." });

        var status = string.IsNullOrWhiteSpace(data.Status) ? "Pending" : data.Status.Trim();
        var fullName = NameFormatting.BuildFullName(emp.FirstName, emp.MiddleName, emp.LastName);

        var id = await _onboarding.CreateScopedAsync(
            tenantId,
            orgId,
            data.EmployeeId,
            fullName,
            data.TaskName!.Trim(),
            data.Category!.Trim(),
            data.DueDate,
            status,
            string.IsNullOrWhiteSpace(data.AssignedTo) ? null : data.AssignedTo.Trim(),
            ct);

        return await QueryOneAsync(id, tenantId, orgId, ct);
    }

    public async Task<Respons<OnboardingTaskListItemDto>> UpdateAsync(
        Guid id, UpdateOnboardingTaskDto data, string tenantId, string orgId, CancellationToken ct = default)
    {
        var hasTaskName = !string.IsNullOrWhiteSpace(data.TaskName);
        var hasCategory = !string.IsNullOrWhiteSpace(data.Category);
        var hasDueDate = data.DueDate.HasValue;
        var hasStatus = !string.IsNullOrWhiteSpace(data.Status);
        var hasAssignedTo = data.AssignedTo is not null;
        if (!hasTaskName && !hasCategory && !hasDueDate && !hasStatus && !hasAssignedTo)
            return Respons<OnboardingTaskListItemDto>.EmptyUpdateRequest();

        var updated = await _onboarding.UpdateScopedAsync(
            id,
            tenantId,
            orgId,
            hasTaskName ? data.TaskName : null,
            hasCategory ? data.Category : null,
            hasDueDate ? data.DueDate : null,
            hasStatus ? data.Status : null,
            hasAssignedTo ? data.AssignedTo : null,
            ct);

        if (updated is null)
        {
            var exists = await _onboarding.GetByIdScopedAsync(id, tenantId, orgId, ct);
            return exists is null
                ? Respons<OnboardingTaskListItemDto>.Fail("Onboarding task not found.", statusCode: 404)
                : Respons<OnboardingTaskListItemDto>.EmptyUpdateRequest();
        }

        return Respons<OnboardingTaskListItemDto>.Ok(updated);
    }

    public async Task<Respons<object>> DeleteAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default)
    {
        if (!await _onboarding.DeleteScopedAsync(id, tenantId, orgId, ct))
            return Respons<object>.Fail("Onboarding task not found.", statusCode: 404);
        return Respons<object>.Ok(new { taskId = id.ToString() }, "Onboarding task deleted.");
    }

    private async Task<Respons<OnboardingTaskListItemDto>> QueryOneAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct)
    {
        var row = await _onboarding.GetByIdScopedAsync(id, tenantId, orgId, ct);
        return row is null
            ? Respons<OnboardingTaskListItemDto>.Fail("Onboarding task not found.", statusCode: 404)
            : Respons<OnboardingTaskListItemDto>.Ok(row);
    }
}
