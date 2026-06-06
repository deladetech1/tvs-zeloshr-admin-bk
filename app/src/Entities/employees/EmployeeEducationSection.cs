namespace ZelosHR.Api.Entities.Employees;

/// <summary>Merge partial <c>education</c> payloads and map to persistence writes (single row per employee).</summary>
internal static class EmployeeEducationSection
{
    internal static Dictionary<string, string>? ValidateForCreate(EmployeeAggregateEducationDto? education)
    {
        if (education is null)
            return null;

        if (string.IsNullOrWhiteSpace(education.Institution))
        {
            return new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["education.institution"] = "Institution is required.",
            };
        }

        return null;
    }

    internal static EmployeeEducationWriteDto ToWrite(
        EmployeeAggregateEducationDto patch,
        EmployeeEducationDto? existing)
    {
        if (existing is null)
        {
            return new EmployeeEducationWriteDto(
                patch.Institution!.Trim(),
                patch.Degree,
                patch.FieldOfStudy,
                patch.StartDate,
                patch.EndDate,
                patch.IsCurrent ?? false,
                patch.CustomFields);
        }

        return new EmployeeEducationWriteDto(
            Coalesce(patch.Institution, existing.Institution),
            patch.Degree ?? existing.Degree,
            patch.FieldOfStudy ?? existing.FieldOfStudy,
            patch.StartDate ?? existing.StartDate,
            patch.EndDate ?? existing.EndDate,
            patch.IsCurrent ?? existing.IsCurrent,
            patch.CustomFields ?? existing.CustomFields);
    }

    internal static EmployeeAggregateEducationDto FromRead(
        EmployeeEducationDto row,
        Dictionary<string, string?>? customFields) =>
        new()
        {
            Institution = row.Institution,
            Degree = row.Degree,
            FieldOfStudy = row.FieldOfStudy,
            StartDate = row.StartDate,
            EndDate = row.EndDate,
            IsCurrent = row.IsCurrent,
            CustomFields = EmployeeAggregateReadMapper.CustomFieldsOrNull(customFields),
        };

    private static string Coalesce(string? patch, string existing) =>
        string.IsNullOrWhiteSpace(patch) ? existing : patch.Trim();
}
