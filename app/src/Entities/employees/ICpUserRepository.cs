using ZelosHR.Api.Entities.Users;

namespace ZelosHR.Api.Entities.Employees;

public sealed record CpUserDto(
    string Id,
    string FullName,
    string Email,
    string? Phone,
    bool IsActive,
    string? Gender = null,
    string? Dob = null,
    string? Address = null,
    string? ProfilePic = null,
    bool IsOwner = false);

public sealed record CpUserCheckResult(
    bool Exists,
    string? UserId,
    string? FullName,
    bool CanImport);

public sealed record CpUserEmailOwner(string TenantId, string UserId);

public sealed record CpUserContactOwner(string TenantId, string UserId);

public sealed record PlatformUserListRow(
    string Id,
    string TenantId,
    string Fullname,
    string Email,
    string Contact,
    string? Address,
    string? Gender,
    string? Dob,
    string? ProfilePic,
    bool CanLogin,
    string DeleteStatus,
    bool IsActive,
    bool IsOwner,
    string? Description,
    string? Cdate,
    string? Ctime,
    DateTimeOffset? Cdatetime);

public interface ICpUserRepository
{
    Task<CpUserDto?> FindByEmailAsync(string email, string tenantId, CancellationToken ct = default);

    /// <summary>Resolves platform user id + tenant for an email (cp_users.email is globally unique).</summary>
    Task<CpUserEmailOwner?> FindEmailOwnerAsync(string email, CancellationToken ct = default);

    /// <summary>Resolves platform user id + tenant for a contact (cp_users.contact is globally unique).</summary>
    Task<CpUserContactOwner?> FindContactOwnerAsync(
        string contact, string? excludeUserId = null, CancellationToken ct = default);
    Task<CpUserDto?> GetByIdAsync(string userId, string tenantId, CancellationToken ct = default);
    Task<IReadOnlyDictionary<string, CpUserDto>> GetByIdsAsync(
        IEnumerable<string> userIds, string tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<CpUserDto>> SearchAsync(string query, string tenantId, int limit = 20, CancellationToken ct = default);

    Task<(IReadOnlyList<PlatformUserListRow> Items, int Total)> ListPlatformMembersScopedAsync(
        GetUsersQuery query,
        string tenantId,
        int page,
        int pageSize,
        CancellationToken ct = default);
    Task<bool> IsLinkedToEmployeeAsync(string userId, string tenantId, CancellationToken ct = default);

    /// <summary>
    /// Creates cp_users + login_settings + user_locations + hr_employees in one transaction.
    /// </summary>
    Task<CpUserDto> ProvisionEmployeeUserAsync(ProvisionCpUserRequest request, CancellationToken ct = default);

    /// <summary>Updates identity columns on an existing cp_users row.</summary>
    Task<CpUserDto> UpdateIdentityAsync(
        string userId, string tenantId, CpUserIdentityData identity, CancellationToken ct = default);

    Task UpdateProfilePicAsync(
        string userId, string tenantId, string profilePicUrl, CancellationToken ct = default);

    /// <summary>Ensures human_resource.hr_employees exists for an existing platform user.</summary>
    Task EnsureHrMembershipAsync(
        string userId, string tenantId, string? createdBy, CancellationToken ct = default);

    /// <summary>Ensures cp_user_locations includes HR app + org for the tenant.</summary>
    Task EnsureUserLocationAsync(
        string userId,
        string tenantId,
        string orgId,
        string busId,
        string locId,
        CancellationToken ct = default);
}
