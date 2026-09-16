namespace ZelosHR.Api.Entities.Devices;

public sealed class DeviceDto
{
    public required string DeviceId { get; init; }
    public required string Name { get; init; }
    public string? Vendor { get; init; }
    public string? Model { get; init; }
    public string? Serial { get; init; }
    public string? Location { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public string? CreatedById { get; init; }
    public string? UpdatedById { get; init; }
    public string? CreatedBy { get; init; }
    public string? UpdatedBy { get; init; }
}

public sealed class CreateDeviceDto
{
    public string Name { get; set; } = default!;
    public string? Vendor { get; set; }
    public string? Model { get; set; }
    public string? Serial { get; set; }
    public string? Location { get; set; }
}
