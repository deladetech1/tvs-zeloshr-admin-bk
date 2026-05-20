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

    /// <summary>Alias for <see cref="Detail"/> / <see cref="Error"/> for uplift-spec compatibility.</summary>
    public string Message => Detail ?? Error ?? string.Empty;

    /// <summary>Validation or business rule errors (alias of field map values).</summary>
    public IReadOnlyList<string>? Errors =>
        FieldErrors is null ? null : FieldErrors.Values.ToList();

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

    public static Respons<T> NotFound(string message = "Not found") =>
        Fail(message, statusCode: 404, detail: message);

    public static Respons<T> Forbidden(string message = "Forbidden") =>
        Fail(message, statusCode: 403, detail: message);
}

public class PaginationMeta
{
    public int Page { get; set; }
    public int Size { get; set; }
    public int Total { get; set; }
    public bool HasNext { get; set; }

    public int PageSize
    {
        get => Size;
        set => Size = value;
    }

    public int TotalCount
    {
        get => Total;
        set => Total = value;
    }

    public int TotalPages => Size > 0 ? (int)Math.Ceiling((double)Total / Size) : 0;
}

public class ResponseException : Exception
{
    public Respons<object> Response { get; }

    public ResponseException(Respons<object> response) : base(response.Error ?? response.Detail)
    {
        Response = response;
    }
}
