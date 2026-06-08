using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Entities.Files;

namespace ZelosHR.Api.Entities.Departments;

internal static class DepartmentHeadProfiles
{
    internal static string? ResolveStoredReference(DepartmentListRow row, CpUserDto? platformUser) =>
        platformUser?.ProfilePic ?? row.HeadProfilePhotoUrl;

    internal static async Task<IReadOnlyDictionary<string, DocumentReadDto?>> ResolveMapAsync(
        IEnumerable<DepartmentListRow> rows,
        IReadOnlyDictionary<string, CpUserDto> users,
        HrDocumentPresignedUrlService profileUrls,
        CancellationToken ct)
    {
        var refs = rows
            .Where(r => r.HeadId is not null)
            .Select(r =>
            {
                users.TryGetValue(r.HeadUserId ?? string.Empty, out var cp);
                return ResolveStoredReference(r, cp);
            })
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(r => r!.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        return refs.Count == 0
            ? new Dictionary<string, DocumentReadDto?>(StringComparer.Ordinal)
            : await profileUrls.ResolveDocumentReadsAsync(refs, ct);
    }

    internal static DocumentReadDto? ResolveForRow(
        DepartmentListRow row,
        IReadOnlyDictionary<string, CpUserDto> users,
        IReadOnlyDictionary<string, DocumentReadDto?> profileUrlMap)
    {
        if (row.HeadId is null)
            return null;

        users.TryGetValue(row.HeadUserId ?? string.Empty, out var cp);
        var stored = ResolveStoredReference(row, cp);
        return stored is null ? null : profileUrlMap.GetValueOrDefault(stored.Trim());
    }
}
