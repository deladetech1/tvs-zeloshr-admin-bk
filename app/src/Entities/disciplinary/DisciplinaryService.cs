using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Formatting;
using ZelosHR.Api.Shared.Pagination;

namespace ZelosHR.Api.Entities.Disciplinary;

public class DisciplinaryService
{
    private readonly IDisciplinaryRepository _disciplinary;
    private readonly IEmployeeRepository _employees;

    public DisciplinaryService(IDisciplinaryRepository disciplinary, IEmployeeRepository employees)
    {
        _disciplinary = disciplinary;
        _employees = employees;
    }

    public async Task<Respons<DisciplinarySummaryDto>> GetSummaryAsync(
        string tenantId, string orgId, CancellationToken ct)
    {
        var summary = await _disciplinary.GetSummaryScopedAsync(tenantId, orgId, ct);
        return Respons<DisciplinarySummaryDto>.Ok(summary);
    }

    public async Task<Respons<DisciplinaryListDto>> ListAsync(
        string? search, string? status, string? severity,
        int page, int size, string tenantId, string orgId, CancellationToken ct)
    {
        var paging = PagedQuery.From(page, size);
        var (items, total) = await _disciplinary.ListScopedAsync(
            tenantId, orgId, search, status, severity, paging.Page, paging.Size, ct);
        var summary = await _disciplinary.GetSummaryScopedAsync(tenantId, orgId, ct);

        return Respons<DisciplinaryListDto>.Ok(
            new DisciplinaryListDto { Summary = summary, Items = items },
            pagination: new PaginationMeta
            {
                Page = paging.Page,
                Size = paging.Size,
                Total = total,
                HasNext = paging.Offset + items.Count < total,
            });
    }

    public Task<Respons<DisciplinaryCaseListItemDto>> GetByIdAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default) =>
        QueryOneAsync(id, tenantId, orgId, ct);

    public async Task<Respons<DisciplinaryCaseListItemDto>> CreateAsync(
        CreateDisciplinaryCaseDto data, string tenantId, string orgId, CancellationToken ct = default)
    {
        var emp = await _employees.GetByIdScopedAsync(data.EmployeeId, tenantId, orgId, ct);
        if (emp is null)
            return Respons<DisciplinaryCaseListItemDto>.ValidationError(
                new Dictionary<string, string> { ["employeeId"] = "Employee not found." });

        var status = string.IsNullOrWhiteSpace(data.Status) ? "Open" : data.Status.Trim();
        var openedAt = data.OpenedAt ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var fullName = NameFormatting.BuildFullName(emp.FirstName, emp.MiddleName, emp.LastName);

        var id = await _disciplinary.CreateScopedAsync(
            tenantId,
            orgId,
            data.EmployeeId,
            fullName,
            data.CaseType!.Trim(),
            data.Severity!.Trim(),
            status,
            openedAt,
            string.IsNullOrWhiteSpace(data.Description) ? null : data.Description.Trim(),
            ct);

        return await QueryOneAsync(id, tenantId, orgId, ct);
    }

    public async Task<Respons<DisciplinaryCaseListItemDto>> UpdateAsync(
        Guid id, UpdateDisciplinaryCaseDto data, string tenantId, string orgId, CancellationToken ct = default)
    {
        var hasCaseType = !string.IsNullOrWhiteSpace(data.CaseType);
        var hasSeverity = !string.IsNullOrWhiteSpace(data.Severity);
        var hasOpenedAt = data.OpenedAt.HasValue;
        var hasDescription = data.Description is not null;
        var hasStatus = !string.IsNullOrWhiteSpace(data.Status);
        if (!hasCaseType && !hasSeverity && !hasOpenedAt && !hasDescription && !hasStatus)
            return Respons<DisciplinaryCaseListItemDto>.Fail("No fields to update.", statusCode: 400);

        var updated = await _disciplinary.UpdateScopedAsync(
            id,
            tenantId,
            orgId,
            hasCaseType ? data.CaseType : null,
            hasSeverity ? data.Severity : null,
            hasOpenedAt ? data.OpenedAt : null,
            hasDescription ? data.Description : null,
            hasStatus ? data.Status : null,
            ct);

        if (updated is null)
        {
            var exists = await _disciplinary.GetByIdScopedAsync(id, tenantId, orgId, ct);
            return exists is null
                ? Respons<DisciplinaryCaseListItemDto>.Fail("Disciplinary case not found.", statusCode: 404)
                : Respons<DisciplinaryCaseListItemDto>.Fail("No fields to update.", statusCode: 400);
        }

        return Respons<DisciplinaryCaseListItemDto>.Ok(updated);
    }

    public async Task<Respons<object>> DeleteAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default)
    {
        if (!await _disciplinary.DeleteScopedAsync(id, tenantId, orgId, ct))
            return Respons<object>.Fail("Disciplinary case not found.", statusCode: 404);
        return Respons<object>.Ok(new { caseId = id.ToString() }, "Disciplinary case deleted.");
    }

    private async Task<Respons<DisciplinaryCaseListItemDto>> QueryOneAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct)
    {
        var row = await _disciplinary.GetByIdScopedAsync(id, tenantId, orgId, ct);
        return row is null
            ? Respons<DisciplinaryCaseListItemDto>.Fail("Disciplinary case not found.", statusCode: 404)
            : Respons<DisciplinaryCaseListItemDto>.Ok(row);
    }
}
