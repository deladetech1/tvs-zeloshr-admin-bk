namespace ZelosHR.Api.Entities.Shared;

/// <summary>Standard API response envelope for all business endpoints.</summary>
/// <typeparam name="T">Payload type in <see cref="Data"/>.</typeparam>
public class Respons<T>
{
    public string? Detail { get; set; }
    public T? Data { get; set; }
    public bool Success { get; set; }
    public int StatusCode { get; set; }
    public string? Error { get; set; }
    public Dictionary<string, string>? FieldErrors { get; set; }
    public PaginationMeta? Pagination { get; set; }

    public static Respons<T> ValidationError(
        Dictionary<string, string> fieldErrors,
        string error = "Validation failed",
        int statusCode = 400) =>
        new()
        {
            Detail = "Validation failed",
            Success = false,
            StatusCode = statusCode,
            Error = error,
            FieldErrors = fieldErrors,
        };

    public static Respons<T> Ok(T data, string detail = "Success", int statusCode = 200, PaginationMeta? pagination = null) =>
        new()
        {
            Detail = detail,
            Data = data,
            Success = true,
            StatusCode = statusCode,
            Pagination = pagination,
        };

    public static Respons<T> Fail(string error, int statusCode = 400, string? detail = null) =>
        new()
        {
            Detail = detail ?? "Error",
            Success = false,
            StatusCode = statusCode,
            Error = error,
        };
}

public class PaginationMeta
{
    public int Page { get; set; }
    public int Size { get; set; }
    public int Total { get; set; }
    public bool HasNext { get; set; }
}

public class ResponseException : Exception
{
    public Respons<object> Response { get; }

    public ResponseException(Respons<object> response) : base(response.Error ?? response.Detail)
    {
        Response = response;
    }
}
