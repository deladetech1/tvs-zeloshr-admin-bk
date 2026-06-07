using ZelosHR.Api.Entities.Employees;

namespace ZelosHR.Api.Entities.Shared;

/// <summary>Resolves platform user ids to display names for standard audit fields on API responses.</summary>
public static class ResourceAuditMapper
{
    public static string? ResolveDisplayName(
        string? userId,
        IReadOnlyDictionary<string, CpUserDto> users)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return null;

        return users.TryGetValue(userId, out var user) ? user.FullName : null;
    }

    public static IEnumerable<string> CollectUserIds(params IEnumerable<string?[]> sources) =>
        sources.SelectMany(batch => batch)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!);
}
