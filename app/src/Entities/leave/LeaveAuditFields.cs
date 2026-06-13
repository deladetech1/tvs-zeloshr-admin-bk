namespace ZelosHR.Api.Entities.Leave;

/// <summary>Standard audit fields on leave resource responses (see <c>docs/API_CONTRACTS.md</c>).</summary>
public static class LeaveAuditFields
{
    public static (
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt,
        string? CreatedById,
        string? UpdatedById,
        string? CreatedBy,
        string? UpdatedBy) Map(
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        string? createdById,
        string? updatedById,
        IReadOnlyDictionary<string, string> userNames) =>
        (
            createdAt,
            updatedAt,
            createdById,
            updatedById,
            ResolveName(createdById, userNames),
            ResolveName(updatedById, userNames));

    public static string? ResolveName(string? userId, IReadOnlyDictionary<string, string> userNames)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return null;
        return userNames.TryGetValue(userId, out var name) ? name : null;
    }

    public static IEnumerable<string> CollectUserIds(
        IEnumerable<string?> createdByIds,
        IEnumerable<string?> updatedByIds) =>
        createdByIds.Concat(updatedByIds).Where(id => !string.IsNullOrWhiteSpace(id)).Select(id => id!);
}
