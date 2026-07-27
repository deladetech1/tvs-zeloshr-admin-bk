using FluentAssertions;
using ZelosHR.Api.Entities.EmployeePortal;

namespace ZelosHR.Api.Tests.EmployeePortal;

public class EmployeePortalSubdomainRulesTests
{
    [Theory]
    [InlineData("btl")]
    [InlineData("my-company")]
    [InlineData("acme123")]
    public void Validate_accepts_valid_subdomains(string subdomain)
    {
        EmployeePortalSubdomainRules.Validate(subdomain).Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("ab")]
    [InlineData("-bad")]
    [InlineData("bad-")]
    [InlineData("admin")]
    [InlineData("www")]
    public void Validate_rejects_invalid_subdomains(string subdomain)
    {
        EmployeePortalSubdomainRules.Validate(subdomain).Should().NotBeNull();
    }

    [Fact]
    public void Normalize_lowercases_and_trims()
    {
        EmployeePortalSubdomainRules.Normalize("  BTL  ").Should().Be("btl");
    }

    [Fact]
    public void PortalHost_builds_hostname()
    {
        EmployeePortalSubdomainRules.PortalHost("btl").Should().Be("btl.zeloshr.com");
    }
}
