namespace ZelosHR.Api.Persistence.Entities;

/// <summary>Read-only map to core_platform.cp_users.</summary>
public sealed class CpUserEntity
{
    public string Id { get; set; } = default!;
    public string TenantId { get; set; } = default!;
    public string Fullname { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string Contact { get; set; } = default!;
    public bool IsActive { get; set; }
    public DateTimeOffset? Cdatetime { get; set; }
}
