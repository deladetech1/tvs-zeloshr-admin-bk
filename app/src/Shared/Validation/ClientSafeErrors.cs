namespace ZelosHR.Api.Shared.Validation;

/// <summary>Maps internal exceptions to short, client-safe API messages.</summary>
public static class ClientSafeErrors
{
    public const string QueryFilterFailed =
        "The search or status filters could not be applied. Try a different search term or fewer filters.";

    public const string UnexpectedRequest =
        "The request could not be processed. Check your input and try again.";

    public static bool IsEntityFrameworkQueryTranslationError(string? message) =>
        !string.IsNullOrWhiteSpace(message)
        && (message.Contains("could not be translated", StringComparison.OrdinalIgnoreCase)
            || message.Contains("LINQ expression", StringComparison.OrdinalIgnoreCase)
            || message.Contains("DbSet<", StringComparison.Ordinal));

    public static string SanitizeInvalidOperationMessage(string message)
    {
        if (IsEntityFrameworkQueryTranslationError(message))
            return QueryFilterFailed;

        var trimmed = message.Trim();
        if (trimmed.Length > 280
            || trimmed.Contains('\n', StringComparison.Ordinal)
            || trimmed.Contains("DbSet", StringComparison.Ordinal))
            return UnexpectedRequest;

        return trimmed.TrimEnd('.') + ".";
    }
}
