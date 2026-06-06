using FluentAssertions;
using ZelosHR.Api.Entities.Employees;

namespace ZelosHR.Api.Tests.Employees;

public sealed class EmployeeEducationSectionTests
{
    private static readonly EmployeeEducationDto Existing = new(
        Guid.Parse("55555555-5555-5555-5555-555555555501"),
        Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
        "University of Ghana",
        "BSc",
        "Computer Science",
        null,
        null,
        false);

    [Fact]
    public void ValidateForCreate_requires_institution_when_section_sent()
    {
        var errors = EmployeeEducationSection.ValidateForCreate(new EmployeeAggregateEducationDto
        {
            Degree = "BSc",
        });

        errors.Should().NotBeNull();
        errors!.Should().ContainKey("education.institution");
    }

    [Fact]
    public void ToWrite_merges_partial_patch_with_existing_row()
    {
        var write = EmployeeEducationSection.ToWrite(
            new EmployeeAggregateEducationDto { Degree = "MSc" },
            Existing);

        write.Institution.Should().Be("University of Ghana");
        write.Degree.Should().Be("MSc");
        write.FieldOfStudy.Should().Be("Computer Science");
    }

    [Fact]
    public void ToWrite_requires_institution_on_create()
    {
        var write = EmployeeEducationSection.ToWrite(
            new EmployeeAggregateEducationDto
            {
                Institution = "KNUST",
                Degree = "BSc",
            },
            existing: null);

        write.Institution.Should().Be("KNUST");
        write.Degree.Should().Be("BSc");
    }
}
