namespace ZelosHR.Api.Shared.Abstractions;

public interface ITenantContext
{
    string TenantId { get; }
    string OrgId { get; }
    string AppId { get; }
    string BusId { get; }
    string LocId { get; }
    string? UserId { get; }
    IReadOnlyList<string> Permissions { get; }
}
