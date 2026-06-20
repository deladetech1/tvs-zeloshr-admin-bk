using FluentAssertions;
using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Entities.Files;
using ZelosHR.Api.Persistence.Entities;

namespace ZelosHR.Api.Tests.Employees;

public class EmployeeAggregateReadMapperTests
{
    [Fact]
    public void BuildEmployment_includes_nested_reports_to_ref()
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
            new ReportsToDisplay(managerId, "Demo Admin", "Head of Engineering", photo));

        employment.Should().NotBeNull();
        employment!.ReportsToId.Should().BeNull();
        employment.ReportsTo.Should().NotBeNull();
        employment.ReportsTo!.Id.Should().Be(managerId.ToString());
        employment.ReportsTo.Name.Should().Be("Demo Admin");
        employment.ReportsTo.Position.Should().Be("Head of Engineering");
        employment.ReportsTo.PhotoUrl.Should().BeEquivalentTo(photo);
    }

    [Fact]
    public void BuildEmployment_includes_nested_employment_type_ref()
    {
        var typeId = Guid.NewGuid();
        var entity = new EmployeeEntity
        {
            Id = Guid.NewGuid(),
            EmployeeCode = "EMP-0001",
            TenantId = "t1",
            OrgId = "o1",
            FullName = "Ama Mensah",
            LifecycleState = EmployeeLifecycleStates.Active,
            LifecycleStatus = EmployeeLifecycleStates.Active,
            EmploymentTypeId = typeId,
            JobTitle = "Software Engineer",
        };

        var employment = EmployeeAggregateReadMapper.BuildEmployment(
            entity,
            customFields: null,
            reportsTo: null,
            employmentType: new EmploymentTypeDisplay(
                typeId.ToString(),
                "Full-time",
                "Standard salaried employment",
                "default"));

        employment.Should().NotBeNull();
        employment!.EmploymentType.Should().NotBeNull();
        employment.EmploymentType!.Id.Should().Be(typeId.ToString());
        employment.EmploymentType.Name.Should().Be("Full-time");
        employment.EmploymentType.Type.Should().Be("default");
    }
}
