using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Formatting;
using ZelosHR.Api.Shared.Pagination;

namespace ZelosHR.Api.Entities.LifecycleEvents;

public class LifecycleEventsService
{
    private readonly ILifecycleEventRepository _lifecycleEvents;
    private readonly IEmployeeRepository _employees;

    public LifecycleEventsService(ILifecycleEventRepository lifecycleEvents, IEmployeeRepository employees)
    {
        _lifecycleEvents = lifecycleEvents;
        _employees = employees;
    }

    public async Task<Respons<LifecycleEventSummaryDto>> GetSummaryAsync(
        string tenantId,
        string orgId,
        CancellationToken ct = default)
    {
        var summary = await _lifecycleEvents.GetSummaryScopedAsync(tenantId, orgId, ct);
        return Respons<LifecycleEventSummaryDto>.Ok(summary);
    }

    public async Task<Respons<LifecycleEventListDto>> ListAsync(
        string? search,
        string? eventType,
        string? urgency,
        string? department,
        int page,
        int size,
        string tenantId,
        string orgId,
        CancellationToken ct = default)
    {
        var paging = PagedQuery.From(page, size);
        var (items, total) = await _lifecycleEvents.ListScopedAsync(
            tenantId, orgId, search, eventType, urgency, department, paging.Page, paging.Size, ct);
        var summary = await _lifecycleEvents.GetSummaryScopedAsync(tenantId, orgId, ct);

        return Respons<LifecycleEventListDto>.Ok(
            new LifecycleEventListDto { Summary = summary, Items = items },
            pagination: new PaginationMeta
            {
                Page = paging.Page,
                Size = paging.Size,
                Total = total,
                HasNext = paging.Offset + items.Count < total,
            });
    }

    public Task<Respons<LifecycleEventListItemDto>> GetByIdAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default) =>
        QueryOneAsync(id, tenantId, orgId, ct);

    public async Task<Respons<LifecycleEventListItemDto>> CreateAsync(
        CreateLifecycleEventDto data, string tenantId, string orgId, CancellationToken ct = default)
    {
        var emp = await _employees.GetByIdScopedAsync(data.EmployeeId, tenantId, orgId, ct);
        if (emp is null)
            return Respons<LifecycleEventListItemDto>.ValidationError(
                new Dictionary<string, string> { ["employeeId"] = "Employee not found." });

        var status = string.IsNullOrWhiteSpace(data.Status) ? "Pending" : data.Status.Trim();
        var urgency = string.IsNullOrWhiteSpace(data.Urgency) ? "Upcoming" : data.Urgency.Trim();
        var fullName = NameFormatting.BuildFullName(emp.FirstName, emp.MiddleName, emp.LastName);

        var id = await _lifecycleEvents.CreateScopedAsync(
            tenantId,
            orgId,
            data.EmployeeId,
            fullName,
            data.EventType!.Trim(),
            emp.Department?.Name,
            emp.Branch?.Name,
            data.DueDate,
            status,
            urgency,
            ct);

        return await QueryOneAsync(id, tenantId, orgId, ct);
    }

    public async Task<Respons<LifecycleEventListItemDto>> UpdateAsync(
        Guid id, UpdateLifecycleEventDto data, string tenantId, string orgId, CancellationToken ct = default)
    {
        var hasEventType = !string.IsNullOrWhiteSpace(data.EventType);
        var hasDueDate = data.DueDate.HasValue;
        var hasStatus = !string.IsNullOrWhiteSpace(data.Status);
        var hasUrgency = !string.IsNullOrWhiteSpace(data.Urgency);
        if (!hasEventType && !hasDueDate && !hasStatus && !hasUrgency)
            return Respons<LifecycleEventListItemDto>.Fail("No fields to update.", statusCode: 400);

        var updated = await _lifecycleEvents.UpdateScopedAsync(
            id,
            tenantId,
            orgId,
            hasEventType ? data.EventType : null,
            hasDueDate ? data.DueDate : null,
            hasStatus ? data.Status : null,
            hasUrgency ? data.Urgency : null,
            ct);

        if (updated is null)
        {
            var exists = await _lifecycleEvents.GetByIdScopedAsync(id, tenantId, orgId, ct);
            return exists is null
                ? Respons<LifecycleEventListItemDto>.Fail("Lifecycle event not found.", statusCode: 404)
                : Respons<LifecycleEventListItemDto>.Fail("No fields to update.", statusCode: 400);
        }

        if (hasStatus)
            await TrySyncEmployeeLifecycleStateAsync(
                updated.EmployeeId, data.Status!, tenantId, orgId, ct);

        return Respons<LifecycleEventListItemDto>.Ok(updated);
    }

    public async Task<Respons<object>> DeleteAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default)
    {
        if (!await _lifecycleEvents.DeleteScopedAsync(id, tenantId, orgId, ct))
            return Respons<object>.Fail("Lifecycle event not found.", statusCode: 404);
        return Respons<object>.Ok(new { lifecycleEventId = id.ToString() }, "Lifecycle event deleted.");
    }

    private async Task<Respons<LifecycleEventListItemDto>> QueryOneAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct)
    {
        var row = await _lifecycleEvents.GetByIdScopedAsync(id, tenantId, orgId, ct);
        return row is null
            ? Respons<LifecycleEventListItemDto>.Fail("Lifecycle event not found.", statusCode: 404)
            : Respons<LifecycleEventListItemDto>.Ok(row);
    }

    private async Task TrySyncEmployeeLifecycleStateAsync(
        string employeeId,
        string eventStatus,
        string tenantId,
        string orgId,
        CancellationToken ct)
    {
        if (!Guid.TryParse(employeeId, out var empId))
            return;

        var lifecycleState = MapEventStatusToEmployeeLifecycleState(eventStatus);
        if (lifecycleState is null)
            return;

        var entity = await _employees.GetByIdScopedForUpdateAsync(empId, tenantId, orgId, ct);
        if (entity is null)
            return;

        entity.LifecycleState = lifecycleState;
        await _employees.UpdateAsync(entity, ct);
    }

    private static string? MapEventStatusToEmployeeLifecycleState(string eventStatus) =>
        eventStatus.Trim() switch
        {
            "Completed" => EmployeeLifecycleStates.Active,
            "On Leave" => EmployeeLifecycleStates.OnLeave,
            "Suspended" => EmployeeLifecycleStates.Suspended,
            "Resigned" => EmployeeLifecycleStates.Resigned,
            "Terminated" => EmployeeLifecycleStates.Terminated,
            _ => null,
        };
}
