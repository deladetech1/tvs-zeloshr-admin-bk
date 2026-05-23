namespace ZelosHR.Api.Persistence.Repositories;

/// <summary>Validates Trove session headers against core_platform tables.</summary>
public interface IPlatformContextRepository
{
    /// <summary>
    /// Ensures org/bus/loc/app exist for the tenant and the user may access that context.
    /// </summary>
    Task<bool> ValidateSessionContextAsync(
        string tenantId,
        string? userId,
        string orgId,
        string busId,
        string locId,
        string appId,
        CancellationToken ct = default);
}
