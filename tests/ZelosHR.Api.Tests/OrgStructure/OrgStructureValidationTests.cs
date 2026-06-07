using FluentAssertions;
using ZelosHR.Api.Entities.OrgStructure;

namespace ZelosHR.Api.Tests.OrgStructure;

public class OrgStructureValidationTests
{
    [Fact]
    public void ValidateBranchFields_rejects_long_address()
    {
        var errors = OrgStructureValidation.ValidateBranchFields(new string('a', 501), null, null);
        errors.Should().ContainKey("address");
    }

    [Fact]
    public void ValidateBranchFields_rejects_long_country()
    {
        var errors = OrgStructureValidation.ValidateBranchFields(null, new string('a', 101), null);
        errors.Should().ContainKey("country");
    }

    [Fact]
    public void ValidateDepartmentDescription_rejects_long_value()
    {
        var errors = OrgStructureValidation.ValidateDepartmentDescription(new string('a', 501));
        errors.Should().ContainKey("description");
    }
}
