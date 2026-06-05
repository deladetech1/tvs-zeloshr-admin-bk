using ZelosHR.Api.Entities.CustomFields;
using ZelosHR.Api.Entities.Employees;

namespace ZelosHR.Api.Tests.CustomFields;

public sealed class CustomFieldEntitySectionsTests
{
    [Fact]
    public void ForEntityType_employee_returns_five_sections_with_labels()
    {
        var sections = CustomFieldEntitySections.ForEntityType(CustomFieldEntityTypes.Employee);

        Assert.Equal(5, sections.Count);
        Assert.Contains(sections, s => s.Value == EmployeeCustomFieldSections.Identity && s.Label == "Identity");
        Assert.Contains(sections, s => s.Value == EmployeeCustomFieldSections.Compensation && s.Label == "Compensation");
    }

    [Fact]
    public void ForEntityType_department_returns_empty_sections()
    {
        var sections = CustomFieldEntitySections.ForEntityType(CustomFieldEntityTypes.Department);

        Assert.Empty(sections);
    }

    [Theory]
    [InlineData("employee", "employee")]
    [InlineData("EMPLOYEE", "employee")]
    [InlineData("invalid", null)]
    public void ResolveCanonicalEntityType_normalizes_input(string input, string? expected)
    {
        Assert.Equal(expected, CustomFieldEntitySections.ResolveCanonicalEntityType(input));
    }
}
