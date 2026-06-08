using FluentAssertions;
using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Persistence.Entities;

namespace ZelosHR.Api.Tests.Employees;

public class EmployeeStatusFilterTests
{
    private static readonly DateOnly Today = new(2026, 6, 8);

    [Fact]
    public void ResolveEngagement_WhenOnProbation_ReturnsActive()
    {
        var employee = new EmployeeEntity
        {
            LifecycleState = EmployeeLifecycleStates.Active,
            EmploymentStatus = EmploymentStatusValues.Probation,
        };

        EmployeeStatusFilter.ResolveEngagement(employee).Should().Be(EmployeeEngagementValues.Active);
        EmployeeStatusFilter.ResolveWorkStates(employee, Today).Should().Contain(EmployeeWorkStateValues.Probation);
    }

    [Fact]
    public void ResolveEngagement_WhenOnLeave_ReturnsActiveWithOnLeaveOverlay()
    {
        var employee = new EmployeeEntity
        {
            LifecycleState = EmployeeLifecycleStates.OnLeave,
            EmploymentStatus = EmploymentStatusValues.OnLeave,
        };

        EmployeeStatusFilter.ResolveEngagement(employee).Should().Be(EmployeeEngagementValues.Active);
        EmployeeStatusFilter.ResolveWorkStates(employee, Today).Should().Contain(EmployeeWorkStateValues.OnLeave);
    }

    [Fact]
    public void IsActiveEngagement_WhenProbation_ReturnsTrue()
    {
        var employee = new EmployeeEntity
        {
            LifecycleState = EmployeeLifecycleStates.Active,
            EmploymentStatus = EmploymentStatusValues.Probation,
        };

        EmployeeStatusFilter.IsActiveEngagement(employee).Should().BeTrue();
    }

    [Theory]
    [InlineData("probation", "active", "probation")]
    [InlineData("active", "active", null)]
    [InlineData("on_leave", "active", "on_leave")]
    public void ResolveStatusCommand_MapsListFilterCommands(
        string command, string expectedEngagement, string? expectedWorkState)
    {
        var (engagement, workStates) = EmployeeStatusFilter.ResolveStatusCommand(command);

        engagement.Should().Be(expectedEngagement);
        if (expectedWorkState is null)
            workStates.Should().BeEmpty();
        else
            workStates.Should().Contain(expectedWorkState);
    }
}
