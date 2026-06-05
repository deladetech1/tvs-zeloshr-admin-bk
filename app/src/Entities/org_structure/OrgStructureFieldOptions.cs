namespace ZelosHR.Api.Entities.OrgStructure;

/// <summary>Allow-lists for org-structure query params and OpenAPI pipe-separated hints.</summary>
public static class OrgStructureFieldOptions
{
    public static readonly IReadOnlyList<string> DepartmentSortBy = ["name", "employeeCount"];
    public static readonly IReadOnlyList<string> SortOrder = ["asc", "desc"];
    public static readonly IReadOnlyList<string> NodeTypes = ["department"];
    public static readonly IReadOnlyList<string> IncludeArchived = ["false", "true"];
}
