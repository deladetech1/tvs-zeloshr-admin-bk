namespace ZelosHR.Api.Entities.Employees;

/// <summary>
/// Section names for employee custom field definitions (<c>section_name</c> on definitions).
/// Used when defining fields via <c>POST /custom-fields/add</c>; values are sent on the matching
/// employee aggregate section's <c>custom_fields</c> object.
/// </summary>
public static class EmployeeCustomFieldSections
{
    public const string Identity = "employee-directory-identity";
    public const string Employment = "employee-directory-employment";
    public const string Compensation = "employee-directory-compensation";
    public const string Education = "employee-directory-education";
    public const string Certification = "employee-directory-certification";

    public static readonly IReadOnlyList<string> All =
        [Identity, Employment, Compensation, Education, Certification];

    /// <summary>Maps legacy short section names stored before the employee-directory prefix.</summary>
    public static readonly IReadOnlyDictionary<string, string> LegacyAliases =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["identity"] = Identity,
            ["employment"] = Employment,
            ["compensation"] = Compensation,
            ["education"] = Education,
            ["certification"] = Certification,
        };
}
