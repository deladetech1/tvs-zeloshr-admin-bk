namespace ZelosHR.Api.Shared.Abstractions;

/// <summary>
/// Application-facing current user. Prefer Trovesuite.Package auth types when available;
/// this abstraction supports uplift tests and services until package surface is unified.
/// </summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Role { get; }
    bool HasPermission(string permission);
}
