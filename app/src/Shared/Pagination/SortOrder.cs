namespace ZelosHR.Api.Shared.Pagination;

public static class SortOrder
{
    public const string Asc = "asc";
    public const string Desc = "desc";

    public static string Normalize(string? value) =>
        string.Equals(value, Desc, StringComparison.OrdinalIgnoreCase) ? Desc : Asc;
}
