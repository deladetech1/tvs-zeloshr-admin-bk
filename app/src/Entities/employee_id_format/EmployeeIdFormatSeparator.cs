namespace ZelosHR.Api.Entities.EmployeeIdFormat;

public static class EmployeeIdFormatSeparator
{
    public const string Hyphen = "hyphen";
    public const string None = "none";
    public const string Underscore = "underscore";
    public const string Slash = "slash";

    internal static readonly IReadOnlySet<string> Allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Hyphen, None, Underscore, Slash,
    };

    internal static string ToDisplayCharacter(string separator) =>
        separator.ToLowerInvariant() switch
        {
            Hyphen => "-",
            Underscore => "_",
            Slash => "/",
            None => string.Empty,
            _ => throw new ArgumentOutOfRangeException(nameof(separator), separator, "Unsupported separator."),
        };
}
