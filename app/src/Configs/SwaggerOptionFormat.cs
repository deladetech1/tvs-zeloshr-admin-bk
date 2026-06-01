namespace ZelosHR.Api.Configs;

/// <summary>Consistent pipe-separated option lists for Swagger (frontend dropdown hints).</summary>
internal static class SwaggerOptionFormat
{
    internal static string Join(IReadOnlyList<string> values) => string.Join(" | ", values);

    internal static string Join(IEnumerable<string> values) => string.Join(" | ", values);

    internal static string Allowed(IReadOnlyList<string> values) => $"Allowed: {Join(values)}";

    internal static string? Append(string? existing, string addition)
    {
        if (string.IsNullOrWhiteSpace(addition))
            return existing;
        return string.IsNullOrWhiteSpace(existing) ? addition : $"{existing.TrimEnd()} {addition}";
    }
}
