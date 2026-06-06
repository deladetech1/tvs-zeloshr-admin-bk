using FluentAssertions;
using ZelosHR.Api.Entities.Employees;

namespace ZelosHR.Api.Tests.Employees;

public class WorkArrangementRulesTests
{
    [Theory]
    [InlineData("onsite", "on_site")]
    [InlineData("on-site", "on_site")]
    [InlineData("ON_SITE", "on_site")]
    [InlineData("remote", "remote")]
    public void Normalize_maps_aliases(string input, string expected) =>
        WorkArrangementRules.Normalize(input).Should().Be(expected);

    [Fact]
    public void On_site_requires_branch()
    {
        var errors = WorkArrangementRules.ValidateBranchForArrangement("on_site", null);
        errors.Should().ContainKey("employment.branch_id");
    }

    [Fact]
    public void Remote_prohibits_branch()
    {
        var errors = WorkArrangementRules.ValidateBranchForArrangement("remote", Guid.NewGuid());
        errors.Should().ContainKey("employment.branch_id");
    }

    [Fact]
    public void Hybrid_allows_missing_branch()
    {
        WorkArrangementRules.ValidateBranchForArrangement("hybrid", null).Should().BeNull();
    }
}
