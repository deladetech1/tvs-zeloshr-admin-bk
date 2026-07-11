using FluentAssertions;
using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Persistence.Entities;

namespace ZelosHR.Api.Tests.Employees;

public class EmployeeLifecycleSyncTests
{
    [Fact]
    public void SyncFromEmploymentStatus_WhenDraftStatus_PreservesDraft()
    {
        var entity = new EmployeeEntity { EmploymentStatus = EmploymentStatusValues.Draft };

        EmployeeLifecycleSync.SyncFromEmploymentStatus(entity, EmploymentStatusValues.Draft);

        entity.EmploymentStatus.Should().Be(EmploymentStatusValues.Draft);
        entity.LifecycleState.Should().Be(EmployeeLifecycleStates.Draft);
        entity.LifecycleStatus.Should().Be("draft");
    }

    [Fact]
    public void SyncFromEmploymentStatus_WhenPreHireStatus_PreservesPreHire()
    {
        var entity = new EmployeeEntity();

        EmployeeLifecycleSync.SyncFromEmploymentStatus(entity, EmploymentStatusValues.PreHire);

        entity.EmploymentStatus.Should().Be(EmploymentStatusValues.PreHire);
        entity.LifecycleState.Should().Be(EmployeeLifecycleStates.PreHire);
        entity.LifecycleStatus.Should().Be("pre_hire");
    }

    [Fact]
    public void SyncFromEmploymentStatus_WhenActiveStatus_PreservesActive()
    {
        var entity = new EmployeeEntity { EmploymentStatus = EmploymentStatusValues.Active };

        EmployeeLifecycleSync.SyncFromEmploymentStatus(entity, EmploymentStatusValues.Active);

        entity.EmploymentStatus.Should().Be(EmploymentStatusValues.Active);
        entity.LifecycleState.Should().Be(EmployeeLifecycleStates.Active);
        entity.LifecycleStatus.Should().Be("active");
    }
}
