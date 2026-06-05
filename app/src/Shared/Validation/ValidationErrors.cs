using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ZelosHR.Api.Shared.Validation;

/// <summary>
/// Normalizes validation messages and field paths for frontend-friendly <c>field_errors</c> responses.
/// </summary>
public static partial class ValidationErrors
{
    private static readonly JsonNamingPolicy SnakeCase = JsonNamingPolicy.SnakeCaseLower;

    public static Dictionary<string, string> FromModelState(ModelStateDictionary modelState)
    {
        var errors = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var (rawKey, entry) in modelState)
        {
            if (entry?.Errors is not { Count: > 0 })
                continue;

            var field = NormalizeFieldPath(rawKey);
            var message = HumanizeMessage(field, entry.Errors[0].ErrorMessage);

            if (!errors.ContainsKey(field))
                errors[field] = message;
        }

        return errors;
    }

    public static Dictionary<string, string> NormalizeKeys(IReadOnlyDictionary<string, string> fieldErrors)
    {
        var normalized = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var (key, message) in fieldErrors)
        {
            var field = NormalizeFieldPath(key);
            normalized[field] = string.IsNullOrWhiteSpace(message)
                ? $"Invalid value for '{field}'."
                : message.TrimEnd('.') + ".";
        }

        return normalized;
    }

    /// <summary>Human-readable summary for <c>detail</c> / <c>error</c> on validation responses.</summary>
    public static string BuildSummary(IReadOnlyDictionary<string, string> fieldErrors)
    {
        if (fieldErrors.Count == 0)
            return "Validation failed.";

        if (fieldErrors.Count == 1)
            return fieldErrors.Values.First();

        var fields = string.Join(", ", fieldErrors.Keys.Take(5));
        var suffix = fieldErrors.Count > 5 ? $" (+{fieldErrors.Count - 5} more)" : string.Empty;
        return $"Fix {fieldErrors.Count} validation errors: {fields}{suffix}.";
    }

    public static string HumanizeMessage(string field, string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return $"Invalid value for '{field}'.";

        var trimmed = message.Trim();

        if (IsJsonSyntaxError(trimmed))
            return "Request body is not valid JSON. Send snake_case JSON matching the endpoint schema.";

        if (trimmed.Contains("could not be converted", StringComparison.OrdinalIgnoreCase)
            || trimmed.Contains("is an invalid start", StringComparison.OrdinalIgnoreCase))
        {
            return string.IsNullOrWhiteSpace(field) || field == "request"
                ? "Request body has invalid field types or formats."
                : $"Invalid type or format for '{field}'.";
        }

        var requiredField = RequiredFieldPattern().Match(trimmed);
        if (requiredField.Success)
            return $"{FormatFieldLabel(requiredField.Groups[1].Value)} is required.";

        if (trimmed.Equals("The field is required.", StringComparison.OrdinalIgnoreCase))
            return $"{FormatFieldLabel(field)} is required.";

        if (trimmed.StartsWith("A value for the '", StringComparison.OrdinalIgnoreCase)
            && trimmed.EndsWith("' property was not provided.", StringComparison.OrdinalIgnoreCase))
        {
            var inner = trimmed["A value for the '".Length..^"' property was not provided.".Length];
            return $"{FormatFieldLabel(inner)} is required.";
        }

        return trimmed.TrimEnd('.') + ".";
    }

    public static string NormalizeFieldPath(string? rawKey)
    {
        if (string.IsNullOrWhiteSpace(rawKey) || rawKey is "$" or "request" or "body")
            return "request";

        var key = rawKey.Trim();
        if (key.StartsWith("body.", StringComparison.OrdinalIgnoreCase))
            key = key[5..];
        if (key.StartsWith("$.", StringComparison.Ordinal))
            key = key[2..];

        var segments = key.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length == 0)
            return "request";

        return string.Join('.', segments.Select(NormalizeSegment));
    }

    private static string NormalizeSegment(string segment)
    {
        if (segment.Contains('[', StringComparison.Ordinal))
            return segment;

        return SnakeCase.ConvertName(segment);
    }

    private static string FormatFieldLabel(string raw)
    {
        var normalized = NormalizeFieldPath(raw).Replace('_', ' ');
        if (string.IsNullOrWhiteSpace(normalized))
            return "Field";

        return char.ToUpperInvariant(normalized[0]) + normalized[1..];
    }

    private static bool IsJsonSyntaxError(string message) =>
        message.Contains("invalid JSON", StringComparison.OrdinalIgnoreCase)
        || message.Contains("'/' is an invalid start", StringComparison.OrdinalIgnoreCase)
        || message.Contains("Expected start of a property name", StringComparison.OrdinalIgnoreCase);

    public static Dictionary<string, string> RequiredQueryParam(string paramName) =>
        new(StringComparer.Ordinal)
        {
            [NormalizeFieldPath(paramName)] = $"{NormalizeFieldPath(paramName)} query parameter is required.",
        };

    public static Dictionary<string, string> EmptyUpdateRequest() =>
        new(StringComparer.Ordinal)
        {
            ["request"] = "Provide at least one field to update.",
        };

    [GeneratedRegex(@"^The\s+(.+?)\s+field is required\.?$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex RequiredFieldPattern();
}
