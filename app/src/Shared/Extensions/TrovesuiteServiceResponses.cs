using ZelosHR.Api.Entities.Shared;

namespace ZelosHR.Api.Shared.Extensions;

/// <summary>Maps Trovesuite.Package service results to ZelosHR <see cref="Respons{T}"/> envelopes.</summary>
internal static class TrovesuiteServiceResponses
{
    internal static Respons<object> FromServiceFailure(
        bool success,
        int statusCode,
        string? error,
        string? detail,
        string fallbackMessage,
        int defaultStatusCode = StatusCodes.Status401Unauthorized)
    {
        if (success)
            throw new ArgumentException("Expected a failed service result.", nameof(success));

        var message = FirstNonEmpty(error, detail, fallbackMessage);
        var code = statusCode > 0 ? statusCode : defaultStatusCode;
        return Respons<object>.Fail(message, statusCode: code);
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value.Trim();
        }

        return "Request failed.";
    }
}
