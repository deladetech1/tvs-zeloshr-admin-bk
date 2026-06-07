using ZelosHR.Api.Entities.OrgStructure;
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

    Task<IReadOnlyList<DepartmentListRow>> GetOrgChartScopedAsync(
        string tenantId, string orgId, CancellationToken ct = default);

    Task<Guid> CreateScopedAsync(
        string tenantId,
        string orgId,
        string name,
        Guid? parentDepartmentId,
        Guid? headOfDepartmentId,
        string? description,
        CancellationToken ct = default);

    Task<bool> ExistsActiveScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default);

    Task<string?> UpdateScopedAsync(
        Guid id,
        string tenantId,
        string orgId,
        string? name,
        Guid? parentDepartmentId,
        Guid? headOfDepartmentId,
        string? description,
        bool updateDescription,
        CancellationToken ct = default);

    Task<OrgStructureDeleteResult> DeleteScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default);
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
