using FluentAssertions;
using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Entities.Files;
using ZelosHR.Api.Persistence.Entities;

namespace ZelosHR.Api.Tests.Employees;

public class EmployeeAggregateReadMapperTests
{
    [Fact]
    public void BuildEmployment_includes_flat_reports_to_display_fields()
    {
        var managerId = Guid.NewGuid();
        var entity = new EmployeeEntity
        {
            Id = Guid.NewGuid(),
            EmployeeCode = "EMP-0001",
            TenantId = "t1",
            OrgId = "o1",
            FullName = "Ama Mensah",
            LifecycleState = EmployeeLifecycleStates.Active,
            LifecycleStatus = EmployeeLifecycleStates.Active,
            ReportsToId = managerId,
            JobTitle = "Software Engineer",
        };

        var photo = new DocumentReadDto
        {
            DocId = "doc_mgr",
            Name = "manager.jpg",
            PresignedUrl = "https://example.test/manager.jpg",
        };

        var employment = EmployeeAggregateReadMapper.BuildEmployment(
            entity,
            customFields: null,
            new ReportsToDisplay("Demo Admin", "Head of Engineering", photo));

        employment.Should().NotBeNull();
        employment!.ReportsToId.Should().Be(managerId);
        employment.ReportsToName.Should().Be("Demo Admin");
        employment.ReportsToPosition.Should().Be("Head of Engineering");
        employment.ReportsToPhotoUrl.Should().BeEquivalentTo(photo);
    }
}
