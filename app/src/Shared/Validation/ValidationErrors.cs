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

    private static readonly Dictionary<string, string> GuidFieldHints = new(StringComparer.Ordinal)
    {
        ["employment.branch_id"] =
            "Branch must be selected from your branch list (UUID). If none applies, remove branch_id from the request.",
        ["employment.department_id"] =
            "Department must be selected from your department list (UUID).",
        ["employment.reports_to_id"] =
            "Reports-to must be an existing employee id (UUID).",
        ["employment.dotted_line_manager_id"] =
            "Dotted-line manager must be an existing employee id (UUID).",
        ["compensation.currency_id"] =
            "Currency must be selected from GET /api/v1/currencies/list (use the id field, not a code).",
    };

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

        return PruneRedundantRequestError(errors);
    }

    public static Dictionary<string, string> NormalizeKeys(IReadOnlyDictionary<string, string> fieldErrors)
    {
        var normalized = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var (key, message) in fieldErrors)
        {
            var field = NormalizeFieldPath(key);
            normalized[field] = string.IsNullOrWhiteSpace(message)
                ? DefaultInvalidMessage(field)
                : message.TrimEnd('.') + ".";
        }

        return PruneRedundantRequestError(normalized);
    }

    /// <summary>Human-readable summary for <c>detail</c> / <c>error</c> on validation responses.</summary>
    public static string BuildSummary(IReadOnlyDictionary<string, string> fieldErrors)
    {
        if (fieldErrors.Count == 0)
            return "Some fields are invalid. Check field_errors and try again.";

        var messages = fieldErrors.Values
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .Select(m => m.TrimEnd('.') + ".")
            .ToList();

        if (messages.Count == 1)
            return messages[0];

        if (messages.Count == 2)
            return $"{messages[0]} {messages[1]}";

        return $"{messages[0]} {messages[1]} (+{messages.Count - 2} more — see field_errors).";
    }

    public static string HumanizeMessage(string field, string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return DefaultInvalidMessage(field);

        var trimmed = message.Trim();

        if (IsJsonSyntaxError(trimmed))
            return "The request body is not valid JSON. Use snake_case property names (for example identity.full_name).";

        if (IsTypeConversionError(trimmed))
        {
            if (GuidFieldHints.TryGetValue(field, out var hint))
                return hint;

            if (field.EndsWith("_id", StringComparison.Ordinal) && field.Contains('.'))
                return $"{FormatFieldLabel(field)} must be a valid UUID, or omitted if optional.";

            return field is "request" or ""
                ? "The request body has one or more fields with the wrong type. See field_errors for details."
                : DefaultInvalidMessage(field);
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

        if (trimmed.Equals("The request field is required.", StringComparison.OrdinalIgnoreCase)
            || trimmed.Contains("request body is required", StringComparison.OrdinalIgnoreCase))
            return "Send a JSON request body.";

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
        if (string.IsNullOrWhiteSpace(normalized) || normalized == "request")
            return "Request body";

        return char.ToUpperInvariant(normalized[0]) + normalized[1..];
    }

    private static string DefaultInvalidMessage(string field) =>
        field is "request" or ""
            ? "The request body is invalid. Check field_errors for details."
            : $"{FormatFieldLabel(field)} is invalid.";

    private static bool IsTypeConversionError(string message) =>
        message.Contains("could not be converted", StringComparison.OrdinalIgnoreCase)
        || message.Contains("is an invalid start", StringComparison.OrdinalIgnoreCase)
        || message.Contains("is not a valid GUID", StringComparison.OrdinalIgnoreCase)
        || message.Contains("is not a valid DateTime", StringComparison.OrdinalIgnoreCase);

    private static bool IsJsonSyntaxError(string message) =>
        message.Contains("invalid JSON", StringComparison.OrdinalIgnoreCase)
        || message.Contains("'/' is an invalid start", StringComparison.OrdinalIgnoreCase)
        || message.Contains("Expected start of a property name", StringComparison.OrdinalIgnoreCase);

    private static Dictionary<string, string> PruneRedundantRequestError(Dictionary<string, string> errors)
    {
        if (errors.Count <= 1 || !errors.TryGetValue("request", out var requestMessage))
            return errors;

        var hasSpecificFields = errors.Keys.Any(k => !string.Equals(k, "request", StringComparison.Ordinal));
        if (!hasSpecificFields)
            return errors;

        var isRedundant = requestMessage.Contains("invalid", StringComparison.OrdinalIgnoreCase)
            || requestMessage.Contains("required", StringComparison.OrdinalIgnoreCase)
            || requestMessage.Contains("wrong type", StringComparison.OrdinalIgnoreCase);

        if (!isRedundant)
            return errors;

        var pruned = new Dictionary<string, string>(errors, StringComparer.Ordinal);
        pruned.Remove("request");
        return pruned;
    }

    public static Dictionary<string, string> RequiredQueryParam(string paramName) =>
        new(StringComparer.Ordinal)
        {
            [NormalizeFieldPath(paramName)] = $"{FormatFieldLabel(paramName)} is required in the query string.",
        };

    public static Dictionary<string, string> EmptyUpdateRequest() =>
        new(StringComparer.Ordinal)
        {
            ["request"] = "Provide at least one field to update.",
        };

    [GeneratedRegex(@"^The\s+(.+?)\s+field is required\.?$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex RequiredFieldPattern();
}
