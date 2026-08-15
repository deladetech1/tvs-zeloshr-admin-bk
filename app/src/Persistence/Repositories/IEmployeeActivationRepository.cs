using ZelosHR.Api.Persistence.Entities;

namespace ZelosHR.Api.Persistence.Repositories;

public sealed record EmployeeActivationTokenRow(
    CpOtpEntity Otp,
    CpUserEntity User,
    bool HasPassword);

public interface IEmployeeActivationRepository
{
    Task<string> CreateActivationTokenAsync(
        string tenantId,
        string userId,
        string email,
        string token,
        string? createdBy,
        string cdate,
        string ctime,
        DateTimeOffset cdatetime,
        CancellationToken ct = default);

    Task DeactivateUserActivationTokensAsync(
        string tenantId,
        string userId,
        CancellationToken ct = default);

    Task<EmployeeActivationTokenRow?> FindActiveTokenAsync(string token, CancellationToken ct = default);

    Task<CpPasswordPolicyEntity?> GetActivePasswordPolicyAsync(string tenantId, CancellationToken ct = default);

    Task SetUserPasswordAsync(
        string tenantId,
        string userId,
        string hashedPassword,
        CancellationToken ct = default);

    Task ConsumeTokenAsync(string otpId, string tenantId, CancellationToken ct = default);

    Task<bool> UserHasPasswordAsync(string tenantId, string userId, CancellationToken ct = default);
}
