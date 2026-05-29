namespace ZelosHR.Api.Entities.CustomFields;

/// <summary>Sort keys for <c>GET /custom-fields/list</c>.</summary>
public static class CustomFieldListSortOptions
{
    public static readonly IReadOnlyList<string> SortBy =
        ["label", "fieldkey", "entitytype", "fieldtype", "displayorder", "sectionorder", "updatedat", "createdat"];

    public static readonly IReadOnlyList<string> SortOrder = ["asc", "desc"];
}
