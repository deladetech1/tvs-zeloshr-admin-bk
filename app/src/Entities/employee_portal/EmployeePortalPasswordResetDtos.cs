namespace ZelosHR.Api.Entities.EmployeePortal;

public sealed record EmployeePasswordResetValidateDto
{
    public bool IsValid { get; init; }
    public bool IsExpired { get; init; }
    public bool IsNotActivated { get; init; }
    public string? FirstName { get; init; }
    public string? CompanyName { get; init; }
    public string? Subdomain { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
}

public sealed record EmployeePasswordResetSetPasswordResultDto
{
    public required string PortalUrl { get; init; }
    public required string WorkEmail { get; init; }
}

public sealed record EmployeePasswordResetRequestResultDto
{
    public required string Message { get; init; }
}

public sealed class EmployeePasswordResetSetPasswordDto
{
    public string? Token { get; set; }
    public string? Password { get; set; }
    public string? ConfirmPassword { get; set; }
}

public sealed class EmployeePasswordResetRequestDto
{
    public string? Subdomain { get; set; }
    public string? WorkEmail { get; set; }
}
