using System.Text.Json.Serialization;
using ZelosHR.Api.Shared.Validation;

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

    /// <summary>Alias for <see cref="Detail"/> / <see cref="Error"/> — not serialized; use <see cref="Detail"/> on the wire.</summary>
    [JsonIgnore]
    public string Message => Detail ?? Error ?? string.Empty;

    /// <summary>Validation or business rule errors (alias of field map values) — not serialized; use <see cref="FieldErrors"/>.</summary>
    [JsonIgnore]
    public IReadOnlyList<string>? Errors =>
        FieldErrors is null ? null : FieldErrors.Values.ToList();

    public static Respons<T> ValidationError(
        Dictionary<string, string> fieldErrors,
        string? summary = null,
        int statusCode = 400)
    {
        var normalized = ValidationErrors.NormalizeKeys(fieldErrors);
        var detail = summary ?? ValidationErrors.BuildSummary(normalized);

        return new()
        {
            Detail = detail,
            Success = false,
            StatusCode = statusCode,
            FieldErrors = normalized,
        };
    }

    public static Respons<T> Ok(T data, string? detail = null, int statusCode = 200, PaginationMeta? pagination = null) =>
        new()
        {
            Detail = detail,
            Data = data,
            Success = true,
            StatusCode = statusCode,
            Pagination = pagination,
        };

    public static Respons<T> Fail(string error, int statusCode = 400, string? detail = null)
    {
        var message = detail ?? error;
        return new()
        {
            Detail = message,
            Success = false,
            StatusCode = statusCode,
            Error = string.Equals(detail, error, StringComparison.Ordinal) || detail is null
                ? null
                : error,
        };
    }

    public static Respons<T> NotFound(string message = "Not found") =>
        Fail(message, statusCode: 404, detail: message);

    public static Respons<T> Forbidden(string message = "Forbidden") =>
        Fail(message, statusCode: 403, detail: message);

    public static Respons<T> EmptyUpdateRequest() =>
        ValidationError(ValidationErrors.EmptyUpdateRequest());
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
