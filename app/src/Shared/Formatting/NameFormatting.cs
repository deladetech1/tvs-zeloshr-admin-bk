namespace ZelosHR.Api.Shared.Formatting;

public static class NameFormatting
{
    public static string BuildFullName(string firstName, string? middleName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(middleName))
            return $"{firstName.Trim()} {lastName.Trim()}";

        return $"{firstName.Trim()} {middleName.Trim()} {lastName.Trim()}";
    }

    public static string BuildInitials(string firstName, string lastName)
    {
        var first = string.IsNullOrWhiteSpace(firstName) ? '?' : char.ToUpperInvariant(firstName.Trim()[0]);
        var last = string.IsNullOrWhiteSpace(lastName) ? '?' : char.ToUpperInvariant(lastName.Trim()[0]);
        return $"{first}{last}";
    }
}
