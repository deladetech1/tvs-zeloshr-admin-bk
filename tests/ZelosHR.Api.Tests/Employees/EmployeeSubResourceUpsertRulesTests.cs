using FluentAssertions;
using ZelosHR.Api.Entities.Employees;

namespace ZelosHR.Api.Tests.Employees;

public sealed class EmployeeSubResourceUpsertRulesTests
{
    private static readonly Guid EducationId = Guid.Parse("55555555-5555-5555-5555-555555555501");

    [Fact]
    public void HasPersistedId_false_when_null()
    {
        EmployeeSubResourceUpsertRules.HasPersistedId(null).Should().BeFalse();
    }

    [Fact]
    public void HasPersistedId_true_when_uuid_set()
    {
        EmployeeSubResourceUpsertRules.HasPersistedId(EducationId).Should().BeTrue();
    }

    [Fact]
    public void ValidateDuplicateIds_education_rejects_repeated_id()
    {
        var items = new[]
        {
            new EmployeeEducationUpsertDto(EducationId, "UG", "BSc", null, null, null, false),
            new EmployeeEducationUpsertDto(EducationId, "UG", "MSc", null, null, null, false),
        };

        var errors = EmployeeSubResourceUpsertRules.ValidateDuplicateIds(items);

        errors.Should().NotBeNull();
        errors!.Should().ContainKey("education[1].id");
    }
}
