namespace ZelosHR.Api.Entities.EmployeeIdFormat;

public sealed record EmployeeIdFormatListDto
{
    public required IReadOnlyList<EmployeeIdFormatReadDto> Items { get; init; }
}

public sealed record EmployeeIdFormatReadDto
{
    public required string Id { get; init; }
    public required string Prefix { get; init; }
    public required int DigitCount { get; init; }
    public required int StartingNumber { get; init; }
    public required string Separator { get; init; }
    public required bool AutoGenerate { get; init; }
    public required string NextIdPreview { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public string? CreatedById { get; init; }
    public string? UpdatedById { get; init; }
    public string? CreatedBy { get; init; }
    public string? UpdatedBy { get; init; }
}

public sealed class CreateEmployeeIdFormatDto
{
    public string? Prefix { get; set; }
    public int? DigitCount { get; set; }
    public int? StartingNumber { get; set; }
    public string? Separator { get; set; }
    public bool? AutoGenerate { get; set; }
}

/// <summary>Full replacement — same shape as create, plus id from GET.</summary>
public sealed class UpdateEmployeeIdFormatDto
{
    public string? Id { get; set; }
    public string? Prefix { get; set; }
    public int? DigitCount { get; set; }
    public int? StartingNumber { get; set; }
    public string? Separator { get; set; }
    public bool? AutoGenerate { get; set; }
}
