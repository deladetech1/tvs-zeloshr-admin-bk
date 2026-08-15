using FluentAssertions;
using ZelosHR.Api.Entities.EmployeePortal;
using ZelosHR.Api.Persistence.Entities;

namespace ZelosHR.Api.Tests.EmployeePortal;

public class EmployeePortalPasswordValidatorTests
{
    [Fact]
    public void ValidateDefault_accepts_strong_password()
    {
        EmployeePortalPasswordValidator.Validate("SecurePass1!", null).Should().BeEmpty();
    }

    [Fact]
    public void ValidateDefault_rejects_short_password()
    {
        EmployeePortalPasswordValidator.Validate("Ab1!", null)
            .Should().Contain(m => m.Contains("8 characters", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_honours_tenant_policy_min_length()
    {
        var policy = new CpPasswordPolicyEntity
        {
            EnforcePasswordPolicy = true,
            MinLength = 12,
            RequireUppercase = false,
            RequireLowercase = false,
            RequireNumbers = false,
            RequireSpecialChars = false,
        };

        EmployeePortalPasswordValidator.Validate("shortpass", policy)
            .Should().Contain(m => m.Contains("12 characters", StringComparison.OrdinalIgnoreCase));
    }
}

public class EmployeePortalSubdomainActivationUrlTests
{
    [Fact]
    public void BuildActivationUrl_uses_portal_domain()
    {
        EmployeePortalSubdomainRules.BuildActivationUrl("deladetech", "abc123", "dev.zeloshr.com")
            .Should().Be("https://deladetech.dev.zeloshr.com/activate/abc123");
    }

    [Fact]
    public void BuildPasswordResetUrl_uses_portal_domain()
    {
        EmployeePortalSubdomainRules.BuildPasswordResetUrl("deladetech", "abc123", "dev.zeloshr.com")
            .Should().Be("https://deladetech.dev.zeloshr.com/reset-password/abc123");
    }
}

public class EmployeePortalPasswordHasherTests
{
    [Fact]
    public void MatchesCurrentPassword_detects_same_password()
    {
        var hash = EmployeePortalPasswordHasher.Hash("SecurePass1!");
        EmployeePortalPasswordValidator.MatchesCurrentPassword("SecurePass1!", hash).Should().BeTrue();
    }

    [Fact]
    public void MatchesCurrentPassword_rejects_different_password()
    {
        var hash = EmployeePortalPasswordHasher.Hash("SecurePass1!");
        EmployeePortalPasswordValidator.MatchesCurrentPassword("DifferentPass1!", hash).Should().BeFalse();
    }
}
