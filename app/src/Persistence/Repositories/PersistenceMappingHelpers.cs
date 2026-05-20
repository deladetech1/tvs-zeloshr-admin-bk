namespace ZelosHR.Api.Persistence.Repositories;

internal static class PersistenceMappingHelpers
{
    public static string? FormatTime(TimeOnly? time) => time?.ToString("HH:mm");

    public static TimeOnly? ParseTime(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null
        : TimeOnly.TryParse(value.Trim(), out var parsed) ? parsed : null;
}
