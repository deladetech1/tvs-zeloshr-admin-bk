using FluentAssertions;
using ZelosHR.Api.Entities.Employees;

namespace ZelosHR.Api.Tests.Employees;

public class EmployeeOnboardingInviteEligibilityTests
{
    [Theory]
    [InlineData("Pre-hire")]
    [InlineData("pre-hire")]
    [InlineData("Probation")]
    [InlineData("Active")]
    [InlineData("On Leave")]
    [InlineData("on leave")]
    public void IsEligible_accepts_workforce_statuses(string status)
    {
        EmployeeOnboardingInviteEligibility.IsEligible(status).Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Draft")]
    [InlineData("Suspended")]
    [InlineData("Terminated")]
    [InlineData("Resigned")]
    [InlineData("Inactive")]
    public void IsEligible_rejects_other_statuses(string? status)
    {
        EmployeeOnboardingInviteEligibility.IsEligible(status).Should().BeFalse();
    }
}
