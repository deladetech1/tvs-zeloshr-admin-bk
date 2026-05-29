namespace ZelosHR.Api.Entities.Employees;

/// <summary>Section names for employee custom field definitions (<c>section_name</c> on definitions).</summary>
public static class EmployeeCustomFieldSections
{
    public const string Identity = "identity";
    public const string Employment = "employment";
    public const string Compensation = "compensation";
    public const string Education = "education";
    public const string Certification = "certification";

    public static readonly IReadOnlyList<string> All =
        [Identity, Employment, Compensation, Education, Certification];
}
