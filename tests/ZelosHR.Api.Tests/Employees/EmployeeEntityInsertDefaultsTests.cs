using FluentAssertions;
using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Persistence.Entities;

namespace ZelosHR.Api.Tests.Employees;

public class EmployeeEntityInsertDefaultsTests
{
    [Fact]
    public void EnsureRequiredColumns_throws_when_lifecycle_not_set()
    {
        var entity = new EmployeeEntity
        {
            EmployeeCode = "ZEL-0001",
            TenantId = "tenant-a",
            OrgId = "org-a",
        };

        var act = () => EmployeeEntityInsertDefaults.EnsureRequiredColumns(entity);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*LifecycleState*");
    }

    [Fact]
    public void EnsureRequiredColumns_fills_json_defaults_when_lifecycle_pre_set()
    {
        var entity = new EmployeeEntity
        {
            EmployeeCode = "ZEL-0001",
            TenantId = "tenant-a",
            OrgId = "org-a",
            LifecycleState = EmployeeLifecycleStates.Draft,
            LifecycleStatus = "draft",
            EmploymentStatus = EmploymentStatusValues.Draft,
        };

        EmployeeEntityInsertDefaults.EnsureRequiredColumns(entity);

        entity.FullName.Should().BeEmpty();
        entity.LifecycleState.Should().Be(EmployeeLifecycleStates.Draft);
        entity.LifecycleStatus.Should().Be("draft");
        entity.EmploymentStatus.Should().Be(EmploymentStatusValues.Draft);
        entity.CustomFieldsData.Should().Be("{}");
        entity.DocumentIds.Should().BeEmpty();
        entity.JobTitle.Should().BeNull();
        entity.DepartmentId.Should().BeNull();
        entity.BranchId.Should().BeNull();
        entity.WorkArrangement.Should().BeNull();
    }
}
