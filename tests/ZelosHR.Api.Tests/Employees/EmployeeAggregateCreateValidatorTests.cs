using FluentAssertions;
using ZelosHR.Api.Entities.Employees;

namespace ZelosHR.Api.Tests.Employees;

public class EmployeeAggregateCreateValidatorTests
{
    [Fact]
    public void DraftCreate_WithOnSiteAndNoBranch_DoesNotRequireBranch()
    {
        var request = new CreateEmployeeAggregateRequest
        {
            Identity = new EmployeeAggregateIdentityDto
            {
                FullName = "Ada Lovelace",
                Phone = "+233201234567",
            },
            Employment = new EmployeeAggregateEmploymentDto
            {
                WorkArrangement = "on_site",
            },
        };

        EmployeeAggregateCreateValidator.Validate(request).Should().BeNull();
    }
}
