using ZelosHR.Api.Entities.Shared;

namespace ZelosHR.Api.Entities.Employees;

/// <summary>Maps row-level batch outcomes to HTTP status and envelope <see cref="Respons{T}.Success"/>.</summary>
internal static class BatchResultResponse
{
    public static Respons<T> FromRowCounts<T>(
        T data,
        int successCount,
        int failureCount,
        string operationName,
        IReadOnlyList<string>? failureMessages = null)
    {
        if (failureCount == 0)
            return Respons<T>.Ok(data);

        var detail = BuildDetail(successCount, failureCount, operationName, failureMessages);

        if (successCount == 0)
        {
            return new Respons<T>
            {
                Data = data,
                Success = false,
                StatusCode = StatusCodes.Status422UnprocessableEntity,
                Detail = detail,
            };
        }

        return new Respons<T>
        {
            Data = data,
            Success = false,
            StatusCode = StatusCodes.Status207MultiStatus,
            Detail = detail,
        };
    }

    private static string BuildDetail(
        int successCount,
        int failureCount,
        string operationName,
        IReadOnlyList<string>? failureMessages)
    {
        var errors = failureMessages?
            .Where(message => !string.IsNullOrWhiteSpace(message))
            .Select(message => message.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList() ?? [];

        if (errors.Count == 1)
            return errors[0];

        if (errors.Count > 1)
            return string.Join("; ", errors);

        return successCount == 0
            ? $"{operationName} completed with no successful rows."
            : $"{operationName} completed with {successCount} successful and {failureCount} failed row(s).";
    }
}
