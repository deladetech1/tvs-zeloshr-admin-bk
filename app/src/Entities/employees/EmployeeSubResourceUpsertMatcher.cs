namespace ZelosHR.Api.Entities.Employees;

/// <summary>Target row to update plus duplicate rows to remove (same content, no <c>id</c> on write).</summary>
internal readonly record struct SubResourceUpsertResolution(Guid? UpdateId, IReadOnlyList<Guid> DuplicateIdsToRemove);

/// <summary>
/// Resolves upsert targets when clients round-trip education/certification rows without <c>id</c>.
/// </summary>
internal static class EmployeeSubResourceUpsertMatcher
{
    public static SubResourceUpsertResolution ResolveEducation(
        EmployeeEducationUpsertDto incoming,
        IReadOnlyList<EmployeeEducationDto> existing,
        ISet<Guid> consumedIds)
    {
        if (incoming.Id is { } explicitId && explicitId != Guid.Empty)
            return new(explicitId, []);

        var matches = existing
            .Where(row => !consumedIds.Contains(row.Id) && EducationContentEquals(row, incoming))
            .ToList();

        if (matches.Count == 0)
            return new(null, []);

        return new(matches[0].Id, matches.Skip(1).Select(row => row.Id).ToList());
    }

    public static SubResourceUpsertResolution ResolveCertification(
        EmployeeCertificationUpsertDto incoming,
        IReadOnlyList<EmployeeCertificationDto> existing,
        ISet<Guid> consumedIds)
    {
        if (incoming.Id is { } explicitId && explicitId != Guid.Empty)
            return new(explicitId, []);

        var matches = existing
            .Where(row => !consumedIds.Contains(row.Id) && CertificationContentEquals(row, incoming))
            .ToList();

        if (matches.Count == 0)
            return new(null, []);

        return new(matches[0].Id, matches.Skip(1).Select(row => row.Id).ToList());
    }

    private static bool EducationContentEquals(EmployeeEducationDto existing, EmployeeEducationUpsertDto incoming) =>
        TextEquals(existing.Institution, incoming.Institution)
        && TextEquals(existing.Degree, incoming.Degree)
        && TextEquals(existing.FieldOfStudy, incoming.FieldOfStudy)
        && existing.StartDate == incoming.StartDate
        && existing.EndDate == incoming.EndDate
        && existing.IsCurrent == incoming.IsCurrent;

    private static bool CertificationContentEquals(
        EmployeeCertificationDto existing,
        EmployeeCertificationUpsertDto incoming) =>
        TextEquals(existing.Name, incoming.Name)
        && TextEquals(existing.IssuingBody, incoming.IssuingBody)
        && existing.IssueDate == incoming.IssueDate
        && existing.ExpiryDate == incoming.ExpiryDate
        && TextEquals(existing.CredentialUrl, incoming.CredentialUrl);

    private static bool TextEquals(string? left, string? right) =>
        string.Equals(Normalize(left), Normalize(right), StringComparison.OrdinalIgnoreCase);

    private static string Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
}
