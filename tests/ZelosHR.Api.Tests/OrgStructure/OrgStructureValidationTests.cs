using FluentAssertions;
using ZelosHR.Api.Entities.OrgStructure;

namespace ZelosHR.Api.Tests.OrgStructure;

public class OrgStructureValidationTests
{
    [Fact]
    public void NormalizeOptionalCountryCode_uppercases_valid_iso_code() =>
        OrgStructureValidation.NormalizeOptionalCountryCode("gh").Should().Be("GH");

    [Fact]
    public void ValidateBranchLocationFields_rejects_invalid_country_code()
    {
        var errors = OrgStructureValidation.ValidateBranchLocationFields(null, null, "GHANA");
        errors.Should().ContainKey("country_code");
    }
}
