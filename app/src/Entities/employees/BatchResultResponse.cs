using ZelosHR.Api.Entities.Shared;

namespace ZelosHR.Api.Entities.Employees;

/// <summary>Maps row-level batch outcomes to HTTP status and envelope <see cref="Respons{T}.Success"/>.</summary>
internal static class BatchResultResponse
{
    public static Respons<T> FromRowCounts<T>(T data, int successCount, int failureCount, string operationName)
    {
        if (failureCount == 0)
            return Respons<T>.Ok(data);

        if (successCount == 0)
        {
            return new Respons<T>
            {
                Data = data,
                Success = false,
                StatusCode = StatusCodes.Status422UnprocessableEntity,
                Detail = $"{operationName} completed with no successful rows.",
            };
        }

        return new Respons<T>
        {
            Data = data,
            Success = false,
            StatusCode = StatusCodes.Status207MultiStatus,
            Detail = $"{operationName} completed with {successCount} successful and {failureCount} failed row(s).",
        };
    }
}
