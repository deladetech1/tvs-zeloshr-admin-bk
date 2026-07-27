namespace ZelosHR.Api.Entities.EmployeePortal;

public sealed record EmployeePortalSubdomainListDto
{
    public required IReadOnlyList<EmployeePortalSubdomainReadDto> Items { get; init; }
}

public sealed record EmployeePortalSubdomainReadDto
{
    public required string Id { get; init; }
    public required string Subdomain { get; init; }
    public required string PortalUrl { get; init; }
    public required string TenantId { get; init; }
    public required string OrgId { get; init; }
    public required string BusId { get; init; }
    public required string LocId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public string? CreatedById { get; init; }
    public string? UpdatedById { get; init; }
    public string? CreatedBy { get; init; }
    public string? UpdatedBy { get; init; }
}

/// <summary>Public bootstrap payload — no auth required.</summary>
public sealed record EmployeePortalResolveDto
{
    public required string Subdomain { get; init; }
    public required string PortalUrl { get; init; }
    public required string TenantId { get; init; }
    public required string OrgId { get; init; }
    public required string BusId { get; init; }
    public required string LocId { get; init; }
    public required string AppId { get; init; }
}

public sealed class CreateEmployeePortalSubdomainDto
{
    public string? Subdomain { get; set; }
}

/// <summary>Full replacement — same shape as create, plus id from GET.</summary>
public sealed class UpdateEmployeePortalSubdomainDto
{
    public string? Id { get; set; }
    public string? Subdomain { get; set; }
}
