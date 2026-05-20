namespace ZelosHR.Api.Entities.Shared;

public class AuthBaseWriteDto
{
    public string? Token { get; set; }
    public required string UserId { get; set; }
    public required string TenantId { get; set; }
}

public class GlobalWriteBaseDto
{
    public required string OrgId { get; set; }
    public required string BusId { get; set; }
    public required string LocId { get; set; }
}
