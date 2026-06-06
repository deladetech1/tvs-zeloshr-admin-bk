using FluentAssertions;
using ZelosHR.Api.Entities.Employees;

namespace ZelosHR.Api.Tests.Employees;

public class EmployeeAggregateCreateValidationTests
{
    [Fact]
    public void MinimalDraft_RequiresFullNameAndPhone()
    {
        var missingBoth = new CreateEmployeeAggregateRequest
        {
            Identity = new EmployeeAggregateIdentityDto { FullName = "", Phone = null },
        };

        var result = EmployeeAggregateCreateValidator.Validate(missingBoth);

        result.Should().NotBeNull();
        result!.Should().ContainKey("identity.full_name");
        result.Should().ContainKey("identity.phone");
    }

    [Fact]
    public void MinimalDraft_WithFullNameAndPhone_PassesValidation()
    {
        var request = new CreateEmployeeAggregateRequest
        {
            Identity = new EmployeeAggregateIdentityDto
            {
                FullName = "Ada Lovelace",
                Phone = "+233201234567",
            },
        };

        EmployeeAggregateCreateValidator.Validate(request).Should().BeNull();
    }
}
