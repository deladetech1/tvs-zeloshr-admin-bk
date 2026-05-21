namespace ZelosHR.Api.Entities.Employees;

public sealed record CpUserDto(
    string Id,
    string FullName,
    string Email,
    string? Phone,
    bool IsActive);

public sealed record CpUserCheckResult(
    bool Exists,
    string? UserId,
    string? FullName,
    bool CanImport);

public interface ICpUserRepository
{
    Task<CpUserDto?> FindByEmailAsync(string email, string tenantId, CancellationToken ct = default);
    Task<CpUserDto?> GetByIdAsync(string userId, string tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<CpUserDto>> SearchAsync(string query, string tenantId, int limit = 20, CancellationToken ct = default);
    Task<bool> IsLinkedToEmployeeAsync(string userId, string tenantId, CancellationToken ct = default);
}
