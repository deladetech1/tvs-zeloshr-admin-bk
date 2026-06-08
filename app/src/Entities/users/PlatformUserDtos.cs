using Microsoft.AspNetCore.Mvc;
using ZelosHR.Api.Configs;
namespace ZelosHR.Api.Entities.Users;

/// <summary>Query filters for <c>GET /api/v1/users/get-users</c> (Core Platform parity).</summary>
public sealed class GetUsersQuery
{
    [FromQuery(Name = "is_active")]
    public bool? IsActive { get; init; }

    [FromQuery(Name = "delete_status")]
    [SwaggerAllowedValues(typeof(PlatformUserFieldOptions), nameof(PlatformUserFieldOptions.DeleteStatuses),
        Description = "Exact match on cp_users.delete_status.")]
    public string? DeleteStatus { get; init; }

    [FromQuery(Name = "can_login")]
    public bool? CanLogin { get; init; }

    public string? Email { get; init; }

    public string? Fullname { get; init; }

    [SwaggerAllowedValues(typeof(PlatformUserFieldOptions), nameof(PlatformUserFieldOptions.Genders))]
    public string? Gender { get; init; }

    [FromQuery(Name = "use_or")]
    public bool UseOr { get; init; }

    public int Page { get; init; } = 1;

    public int Size { get; init; } = 10;
}

public static class PlatformUserFieldOptions
{
    public static readonly IReadOnlyList<string> Genders = ["MALE", "FEMALE"];
    public static readonly IReadOnlyList<string> DeleteStatuses = ["NOT_DELETED", "DELETED", "PENDING"];
}

/// <summary>Platform user row — aligned with Core Platform <c>GET /users/get-users</c>.</summary>
public sealed class PlatformUserListItemDto
{
    public required string Id { get; init; }
    public required string TenantId { get; init; }
    public required string Fullname { get; init; }
    public required string Email { get; init; }
    public required string Contact { get; init; }
    public string? Address { get; init; }
    public string? Gender { get; init; }
    public string? Dob { get; init; }
    public string? ProfilePic { get; init; }
    public bool CanLogin { get; init; }
    public required string DeleteStatus { get; init; }
    public bool IsActive { get; init; }
    public bool IsOwner { get; init; }
    public string? Description { get; init; }
    public string? Cdate { get; init; }
    public string? Ctime { get; init; }
    public DateTimeOffset? Cdatetime { get; init; }
}
