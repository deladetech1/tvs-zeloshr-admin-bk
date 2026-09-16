namespace ZelosHR.Api.Entities.Devices;

public interface IDeviceRepository
{
    Task<(IReadOnlyList<DeviceDto> Items, int Total)> ListScopedAsync(
        string tenantId, string orgId, string? search, int page, int pageSize, CancellationToken ct = default);

    Task<DeviceDto?> GetByIdScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default);

    Task<Guid> CreateScopedAsync(
        string tenantId,
        string orgId,
        string name,
        string? vendor,
        string? model,
        string? serial,
        string? location,
        string? actorUserId,
        CancellationToken ct = default);

    Task<bool> DeleteScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default);
}
