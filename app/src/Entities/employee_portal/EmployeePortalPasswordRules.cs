using System.Text;
using System.Text.RegularExpressions;
using ZelosHR.Api.Persistence.Entities;

namespace ZelosHR.Api.Entities.EmployeePortal;

internal static partial class EmployeePortalPasswordHasher
{
    private const int BcryptWorkFactor = 12;

    /// <summary>Matches Core Platform passlib bcrypt behaviour (truncate UTF-8 to 72 bytes before hash).</summary>
    public static string Hash(string password)
    {
        var bytes = Encoding.UTF8.GetBytes(password);
        if (bytes.Length > 72)
            password = Encoding.UTF8.GetString(bytes, 0, 72);

        return BCrypt.Net.BCrypt.HashPassword(password, BcryptWorkFactor);
    }
}

internal static partial class EmployeePortalPasswordValidator
{
    private const string DefaultSpecialChars = "!@#$%^&*()_+-=[]{}|;:,.<>?";

    public static IReadOnlyList<string> Validate(string password, CpPasswordPolicyEntity? policy)
    {
        if (policy is null || !policy.EnforcePasswordPolicy)
            return ValidateDefault(password);

        var errors = new List<string>();
        if (password.Length < policy.MinLength)
            errors.Add($"Password must be at least {policy.MinLength} characters.");

        if (policy.RequireUppercase && !password.Any(char.IsUpper))
            errors.Add("Password must include at least one uppercase letter.");

        if (policy.RequireLowercase && !password.Any(char.IsLower))
            errors.Add("Password must include at least one lowercase letter.");

        if (policy.RequireNumbers && !password.Any(char.IsDigit))
            errors.Add("Password must include at least one number.");

        if (policy.RequireSpecialChars)
        {
            var specials = string.IsNullOrWhiteSpace(policy.SpecialCharsList)
                ? DefaultSpecialChars
                : policy.SpecialCharsList;
            if (!password.Any(c => specials.Contains(c)))
                errors.Add("Password must include at least one special character.");
        }

        return errors;
    }

    private static IReadOnlyList<string> ValidateDefault(string password)
    {
        var errors = new List<string>();
        if (password.Length < 8)
            errors.Add("Password must be at least 8 characters.");
        if (!password.Any(char.IsUpper))
            errors.Add("Password must include at least one uppercase letter.");
        if (!password.Any(char.IsLower))
            errors.Add("Password must include at least one lowercase letter.");
        if (!password.Any(char.IsDigit))
            errors.Add("Password must include at least one number.");
        if (!SpecialCharPattern().IsMatch(password))
            errors.Add("Password must include at least one special character.");

        return errors;
    }

    [GeneratedRegex(@"[!@#$%^&*()_+\-=\[\]{}|;:,.<>?\\/`~""']")]
    private static partial Regex SpecialCharPattern();
}
