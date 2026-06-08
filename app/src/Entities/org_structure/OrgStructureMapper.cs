using ZelosHR.Api.Entities.Branches;
using ZelosHR.Api.Entities.Departments;
using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Entities.Shared;

namespace ZelosHR.Api.Entities.OrgStructure;

internal static class OrgStructureMapper
{
    internal static DepartmentListItemDto ToDepartmentListItem(
        DepartmentListRow row,
        IReadOnlyDictionary<string, CpUserDto> users) =>
        new()
        {
            DepartmentId = row.Id.ToString(),
            Name = row.Name,
            Description = row.Description,
            ParentDepartmentId = row.ParentDepartmentId?.ToString(),
            ParentDepartmentName = row.ParentDepartmentName,
            HeadOfDepartment = DepartmentHeadMapper.Map(row, users),
            EmployeeCount = row.EmployeeCount,
            HeadcountCapacity = row.HeadcountCapacity,
            IsArchived = row.IsArchived,
            HierarchyLevel = row.ParentDepartmentId is null ? 0 : 1,
            CreatedAt = row.CreatedAt,
            UpdatedAt = row.UpdatedAt,
            CreatedById = row.CreatedBy,
            UpdatedById = row.UpdatedBy,
            CreatedBy = ResourceAuditMapper.ResolveDisplayName(row.CreatedBy, users),
            UpdatedBy = ResourceAuditMapper.ResolveDisplayName(row.UpdatedBy, users),
        };

    internal static BranchListItemDto ToBranchListItem(
        BranchListRow row,
        IReadOnlyDictionary<string, CpUserDto> users) =>
        new()
        {
            BranchId = row.Id.ToString(),
            Name = row.Name,
            Address = row.Address,
            Country = row.Country,
            Description = row.Description,
            EmployeeCount = row.EmployeeCount,
            IsArchived = row.IsArchived,
            CreatedAt = row.CreatedAt,
            UpdatedAt = row.UpdatedAt,
            CreatedById = row.CreatedBy,
            UpdatedById = row.UpdatedBy,
            CreatedBy = ResourceAuditMapper.ResolveDisplayName(row.CreatedBy, users),
            UpdatedBy = ResourceAuditMapper.ResolveDisplayName(row.UpdatedBy, users),
        };

    internal static CreateDepartmentResponseDto ToDepartmentMutation(
        DepartmentListRow row,
        IReadOnlyDictionary<string, CpUserDto> users) =>
        new()
        {
            DepartmentId = row.Id.ToString(),
            Name = row.Name,
            Description = row.Description,
            HeadOfDepartment = DepartmentHeadMapper.Map(row, users),
            CreatedAt = row.CreatedAt,
            UpdatedAt = row.UpdatedAt,
            CreatedById = row.CreatedBy,
            UpdatedById = row.UpdatedBy,
            CreatedBy = ResourceAuditMapper.ResolveDisplayName(row.CreatedBy, users),
            UpdatedBy = ResourceAuditMapper.ResolveDisplayName(row.UpdatedBy, users),
        };

    internal static BranchMutationResponseDto ToBranchMutation(
        BranchListRow row,
        IReadOnlyDictionary<string, CpUserDto> users) =>
        new()
        {
            BranchId = row.Id.ToString(),
            Name = row.Name,
            Address = row.Address,
            Country = row.Country,
            Description = row.Description,
            CreatedAt = row.CreatedAt,
            UpdatedAt = row.UpdatedAt,
            CreatedById = row.CreatedBy,
            UpdatedById = row.UpdatedBy,
            CreatedBy = ResourceAuditMapper.ResolveDisplayName(row.CreatedBy, users),
            UpdatedBy = ResourceAuditMapper.ResolveDisplayName(row.UpdatedBy, users),
        };

    internal static IEnumerable<string> CollectUserIds(IEnumerable<DepartmentListRow> rows) =>
        ResourceAuditMapper.CollectUserIds(
            rows.Select(r => new[] { r.CreatedBy, r.UpdatedBy, r.HeadUserId }));

    internal static IEnumerable<string> CollectUserIds(IEnumerable<BranchListRow> rows) =>
        ResourceAuditMapper.CollectUserIds(rows.Select(r => new[] { r.CreatedBy, r.UpdatedBy }));
}
