namespace ZelosHR.Api.Entities.Employees;

/// <summary>
/// Resolves upsert targets when clients round-trip education/certification rows without <c>id</c>.
/// </summary>
internal static class EmployeeSubResourceUpsertMatcher
{
    public static Guid? ResolveEducationId(
        EmployeeEducationUpsertDto incoming,
        IReadOnlyList<EmployeeEducationDto> existing,
        ISet<Guid> consumedIds)
    {
        if (incoming.Id is { } explicitId)
            return explicitId;

        var matches = existing
            .Where(row => !consumedIds.Contains(row.Id) && EducationContentEquals(row, incoming))
            .ToList();

        return matches.Count == 1 ? matches[0].Id : null;
    }

    public static Guid? ResolveCertificationId(
        EmployeeCertificationUpsertDto incoming,
        IReadOnlyList<EmployeeCertificationDto> existing,
        ISet<Guid> consumedIds)
    {
        if (incoming.Id is { } explicitId)
            return explicitId;

        var matches = existing
            .Where(row => !consumedIds.Contains(row.Id) && CertificationContentEquals(row, incoming))
            .ToList();

        return matches.Count == 1 ? matches[0].Id : null;
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
