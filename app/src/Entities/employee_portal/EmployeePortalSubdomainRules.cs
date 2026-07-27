using System.Text.RegularExpressions;

namespace ZelosHR.Api.Entities.EmployeePortal;

internal static partial class EmployeePortalSubdomainRules
{
    public const int MinLength = 3;
    public const int MaxLength = 63;

    private static readonly HashSet<string> Reserved = new(StringComparer.OrdinalIgnoreCase)
    {
        "www", "admin", "api", "app", "mail", "ftp", "static", "assets", "cdn",
        "dev", "staging", "prod", "production", "test", "zeloshr", "zelos", "portal",
        "employee", "employees", "login", "auth", "dashboard", "support", "help",
        "status", "docs", "swagger", "health", "root", "null", "undefined",
    };

    [GeneratedRegex("^[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?$", RegexOptions.CultureInvariant)]
    private static partial Regex SubdomainPattern();

    internal static string Normalize(string value) => value.Trim().ToLowerInvariant();

    internal static Dictionary<string, string>? Validate(string? subdomain)
    {
        if (string.IsNullOrWhiteSpace(subdomain))
            return new Dictionary<string, string> { ["subdomain"] = "Subdomain is required." };

        var normalized = Normalize(subdomain);

        if (normalized.Length is < MinLength or > MaxLength)
        {
            return new Dictionary<string, string>
            {
                ["subdomain"] = $"Subdomain must be between {MinLength} and {MaxLength} characters.",
            };
        }

        if (!SubdomainPattern().IsMatch(normalized))
        {
            return new Dictionary<string, string>
            {
                ["subdomain"] =
                    "Subdomain must use lowercase letters, digits, and hyphens only, and must start and end with a letter or digit.",
            };
        }

        if (Reserved.Contains(normalized))
        {
            return new Dictionary<string, string>
            {
                ["subdomain"] = "Subdomain is reserved and cannot be used.",
            };
        }

        return null;
    }

    internal static string PortalHost(string subdomain) => $"{subdomain}.zeloshr.com";
}
