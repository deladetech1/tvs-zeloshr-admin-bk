using FluentAssertions;
using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Persistence.Entities;

namespace ZelosHR.Api.Tests.Employees;

public class EmployeeEntityInsertDefaultsTests
{
    [Fact]
    public void EnsureRequiredColumns_fills_not_null_columns_for_minimal_draft()
    {
        var entity = new EmployeeEntity
        {
            EmployeeCode = "ZEL-0001",
            TenantId = "tenant-a",
            OrgId = "org-a",
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
