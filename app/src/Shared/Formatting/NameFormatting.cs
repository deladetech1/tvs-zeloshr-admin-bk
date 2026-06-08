namespace ZelosHR.Api.Shared.Formatting;

public static class NameFormatting
{
    public static string ResolveFullName(
        string? fullName,
        string? firstName,
        string? middleName,
        string? lastName)
    {
        if (!string.IsNullOrWhiteSpace(fullName))
            return fullName.Trim();

        if (!string.IsNullOrWhiteSpace(firstName) && !string.IsNullOrWhiteSpace(lastName))
            return BuildFullName(firstName, middleName, lastName);

        return firstName?.Trim() ?? lastName?.Trim() ?? string.Empty;
    }

    public static string BuildFullName(string? firstName, string? middleName, string? lastName)
    {
        var first = firstName?.Trim() ?? string.Empty;
        var last = lastName?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(first) && string.IsNullOrEmpty(last))
            return string.Empty;
        if (string.IsNullOrEmpty(first))
            return last;
        if (string.IsNullOrEmpty(last))
            return first;

        if (string.IsNullOrWhiteSpace(middleName))
            return $"{first} {last}";

        return $"{first} {middleName.Trim()} {last}";
    }

    public static string BuildInitials(string firstName, string lastName)
    {
        var first = string.IsNullOrWhiteSpace(firstName) ? '?' : char.ToUpperInvariant(firstName.Trim()[0]);
        var last = string.IsNullOrWhiteSpace(lastName) ? '?' : char.ToUpperInvariant(lastName.Trim()[0]);
        return $"{first}{last}";
    }
}
