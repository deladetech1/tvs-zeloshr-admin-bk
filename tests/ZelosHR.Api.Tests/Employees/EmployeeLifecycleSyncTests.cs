using FluentAssertions;
using ZelosHR.Api.Persistence.Entities;

namespace ZelosHR.Api.Tests.Employees;

public class EmployeeLifecycleSyncTests
{
    [Fact]
    public void ApplyPostFinaliseDefaults_WhenDraftStatus_SetsPreHire()
    {
        var entity = new EmployeeEntity { EmploymentStatus = EmploymentStatusValues.Draft };

        EmployeeLifecycleSync.ApplyPostFinaliseDefaults(entity);

        entity.EmploymentStatus.Should().Be(EmploymentStatusValues.PreHire);
        entity.LifecycleState.Should().Be(EmployeeLifecycleStates.PreHire);
        entity.LifecycleStatus.Should().Be("pre_hire");
    }

    [Fact]
    public void ApplyPostFinaliseDefaults_WhenActiveStatus_PreservesActive()
    {
        var entity = new EmployeeEntity { EmploymentStatus = EmploymentStatusValues.Active };

        EmployeeLifecycleSync.ApplyPostFinaliseDefaults(entity);

        entity.EmploymentStatus.Should().Be(EmploymentStatusValues.Active);
        entity.LifecycleState.Should().Be(EmployeeLifecycleStates.Active);
        entity.LifecycleStatus.Should().Be("active");
    }
}
