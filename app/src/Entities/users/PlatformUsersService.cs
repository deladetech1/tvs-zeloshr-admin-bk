using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Pagination;

namespace ZelosHR.Api.Entities.Users;

public sealed class PlatformUsersService
{
    private readonly ICpUserRepository _users;

    public PlatformUsersService(ICpUserRepository users) => _users = users;

    public async Task<Respons<IReadOnlyList<PlatformUserListItemDto>>> GetUsersAsync(
        GetUsersQuery query,
        string tenantId,
        CancellationToken ct = default)
    {
        var paging = PagedQuery.From(query.Page, query.Size);
        var (rows, total) = await _users.ListPlatformMembersScopedAsync(query, tenantId, paging.Page, paging.Size, ct);
        var items = rows.Select(MapRow).ToList();

        return Respons<IReadOnlyList<PlatformUserListItemDto>>.Ok(
            items,
            detail: items.Count > 0
                ? $"{items.Count} user(s) fetched successfully"
                : "No users found",
            pagination: new PaginationMeta
            {
                Page = paging.Page,
                Size = paging.Size,
                Total = total,
                HasNext = paging.Offset + items.Count < total,
            });
    }

    private static PlatformUserListItemDto MapRow(PlatformUserListRow row) => new()
    {
        Id = row.Id,
        TenantId = row.TenantId,
        Fullname = row.Fullname,
        Email = row.Email,
        Contact = row.Contact,
        Address = row.Address,
        Gender = row.Gender,
        Dob = row.Dob,
        ProfilePic = row.ProfilePic,
        CanLogin = row.CanLogin,
        DeleteStatus = row.DeleteStatus,
        IsActive = row.IsActive,
        IsOwner = row.IsOwner,
        Description = row.Description,
        Cdate = row.Cdate,
        Ctime = row.Ctime,
        Cdatetime = row.Cdatetime,
    };
}
