using ZelosHR.Api.Persistence.Entities;

namespace ZelosHR.Api.Entities.Departments;

public interface IDepartmentRepository
{
    Task<OrganisationSummaryDto> GetSummaryScopedAsync(
        string tenantId, string orgId, CancellationToken ct = default);

    Task<(IReadOnlyList<DepartmentListRow> Items, int Total)> ListScopedAsync(
        string tenantId,
        string orgId,
        string? search,
        string sortBy,
        string sortOrder,
        bool includeArchived,
        int page,
        int pageSize,
        CancellationToken ct = default);
}

public sealed record DepartmentListRow(
    Guid Id,
    string Name,
    Guid? ParentDepartmentId,
    string? ParentDepartmentName,
    bool IsArchived,
    Guid? HeadId,
    string? HeadFirstName,
    string? HeadLastName,
    string? HeadJobTitle,
    int EmployeeCount);
