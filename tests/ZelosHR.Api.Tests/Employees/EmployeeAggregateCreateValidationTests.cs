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

    [Fact]
    public void MinimalDraft_WithInvalidPhone_FailsValidation()
    {
        var request = new CreateEmployeeAggregateRequest
        {
            Identity = new EmployeeAggregateIdentityDto
            {
                FullName = "Ada Lovelace",
                Phone = "0201234567",
            },
        };

        var result = EmployeeAggregateCreateValidator.Validate(request);

        result.Should().NotBeNull();
        result!.Should().ContainKey("identity.phone");
    }

    [Fact]
    public void MinimalDraft_WithInvalidPersonalEmail_FailsValidation()
    {
        var request = new CreateEmployeeAggregateRequest
        {
            Identity = new EmployeeAggregateIdentityDto
            {
                FullName = "Ada Lovelace",
                Phone = "+233201234567",
                PersonalEmail = "not-valid",
            },
        };

        var result = EmployeeAggregateCreateValidator.Validate(request);

        result.Should().NotBeNull();
        result!.Should().ContainKey("identity.personal_email");
    }
}
