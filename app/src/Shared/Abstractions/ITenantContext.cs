namespace ZelosHR.Api.Shared.Abstractions;

public interface ITenantContext
{
    string TenantId { get; }
    string OrgId { get; }
    string? UserId { get; }
    IReadOnlyList<string> Permissions { get; }
}
