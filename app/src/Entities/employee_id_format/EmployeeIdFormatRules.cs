using ZelosHR.Api.Persistence.Entities;

namespace ZelosHR.Api.Entities.EmployeeIdFormat;

internal static class EmployeeIdFormatRules
{
    internal const int MaxAttempts = 10;
    internal const int MaxPrefixLength = 20;
    internal const int MinDigitCount = 1;
    internal const int MaxDigitCount = 10;
    internal const int MaxEmployeeCodeLength = 32;

    internal static string Format(EmployeeIdFormatEntity settings, long sequence)
    {
        var prefix = settings.Prefix.Trim();
        var sep = EmployeeIdFormatSeparator.ToDisplayCharacter(settings.Separator);
        var padded = sequence.ToString($"D{settings.DigitCount}");
        return $"{prefix}{sep}{padded}";
    }

    internal static long ComputeNextSequence(
        IEnumerable<string> existingCodes,
        EmployeeIdFormatEntity settings)
    {
        var maxParsed = 0L;
        foreach (var code in existingCodes)
        {
            if (TryParseSequence(code, settings, out var parsed) && parsed > maxParsed)
                maxParsed = parsed;
        }

        var floor = Math.Max(1, settings.StartingNumber);
        return Math.Max(maxParsed, floor - 1L) + 1L;
    }

    internal static bool TryParseSequence(
        string employeeCode,
        EmployeeIdFormatEntity settings,
        out long sequence)
    {
        sequence = 0;
        if (string.IsNullOrWhiteSpace(employeeCode))
            return false;

        var prefix = settings.Prefix.Trim();
        var code = employeeCode.Trim();
        if (!code.StartsWith(prefix, StringComparison.Ordinal))
            return false;

        var remainder = code[prefix.Length..];
        var sep = EmployeeIdFormatSeparator.ToDisplayCharacter(settings.Separator);
        if (sep.Length > 0)
        {
            if (!remainder.StartsWith(sep, StringComparison.Ordinal))
                return false;
            remainder = remainder[sep.Length..];
        }

        if (remainder.Length == 0 || !remainder.All(char.IsDigit))
            return false;

        return long.TryParse(remainder, out sequence);
    }

    internal static bool FitsMaxLength(EmployeeIdFormatEntity settings)
    {
        var sep = EmployeeIdFormatSeparator.ToDisplayCharacter(settings.Separator);
        var sample = Format(settings, (long)Math.Pow(10, settings.DigitCount) - 1);
        return sample.Length <= MaxEmployeeCodeLength;
    }
}
