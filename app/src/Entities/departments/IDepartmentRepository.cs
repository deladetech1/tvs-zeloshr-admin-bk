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

    Task<Guid> CreateScopedAsync(
        string tenantId,
        string orgId,
        string name,
        Guid? parentDepartmentId,
        Guid? headOfDepartmentId,
        string? description,
        int? headcountCapacity,
        string? actedBy,
        CancellationToken ct = default);

    Task<DepartmentListRow?> GetActiveScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default);

    Task<bool> ExistsActiveScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default);

    Task<string?> UpdateScopedAsync(
        Guid id,
        string tenantId,
        string orgId,
        string? name,
        Guid? parentDepartmentId,
        Guid? headOfDepartmentId,
        bool updateHeadOfDepartment,
        string? description,
        bool updateDescription,
        int? headcountCapacity,
        bool updateHeadcountCapacity,
        string? actedBy,
        CancellationToken ct = default);

    Task<OrgStructureDeleteResult> DeleteScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default);
}

public sealed record DepartmentListRow(
    Guid Id,
    string Name,
    string? Description,
    Guid? ParentDepartmentId,
    string? ParentDepartmentName,
    bool IsArchived,
    Guid? HeadId,
    string? HeadUserId,
    string? HeadFullName,
    string? HeadFirstName,
    string? HeadLastName,
    string? HeadJobTitle,
    string? HeadProfilePhotoUrl,
    int EmployeeCount,
    int? HeadcountCapacity,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string? CreatedBy,
    string? UpdatedBy);
