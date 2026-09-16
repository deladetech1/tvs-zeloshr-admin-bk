using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Pagination;

namespace ZelosHR.Api.Entities.Devices;

public class DeviceService
{
    private readonly IDeviceRepository _devices;

    public DeviceService(IDeviceRepository devices) => _devices = devices;

    public async Task<Respons<IReadOnlyList<DeviceDto>>> ListAsync(
        string? search, int page, int size, string tenantId, string orgId, CancellationToken ct = default)
    {
        var paging = PagedQuery.From(page, size);
        var (items, total) = await _devices.ListScopedAsync(tenantId, orgId, search, paging.Page, paging.Size, ct);
        return Respons<IReadOnlyList<DeviceDto>>.Ok(
            items,
            pagination: new PaginationMeta
            {
                Page = paging.Page,
                Size = paging.Size,
                Total = total,
                HasNext = paging.Offset + items.Count < total,
            });
    }

    public async Task<Respons<DeviceDto>> GetByIdAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default)
    {
        var row = await _devices.GetByIdScopedAsync(id, tenantId, orgId, ct);
        return row is null
            ? Respons<DeviceDto>.Fail("Device not found.", statusCode: 404)
            : Respons<DeviceDto>.Ok(row);
    }

    public async Task<Respons<DeviceDto>> CreateAsync(
        CreateDeviceDto data, string tenantId, string orgId, string? actorUserId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(data.Name))
            return Respons<DeviceDto>.ValidationError(
                new Dictionary<string, string> { ["name"] = "name is required." });

        var id = await _devices.CreateScopedAsync(
            tenantId, orgId, data.Name.Trim(), data.Vendor?.Trim(), data.Model?.Trim(),
            data.Serial?.Trim(), data.Location?.Trim(), actorUserId, ct);
        return await GetByIdAsync(id, tenantId, orgId, ct);
    }

    public async Task<Respons<object>> DeleteAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default)
    {
        if (!await _devices.DeleteScopedAsync(id, tenantId, orgId, ct))
            return Respons<object>.Fail("Device not found.", statusCode: 404);
        return Respons<object>.Ok(new { device_id = id.ToString() }, "Device removed.");
    }
}
