using ZelosHR.Api.Entities.Leave;

namespace ZelosHR.Api.Tests.Leave;

public class LeaveTypePolicyTests
{
    [Fact]
    public void ValidateCreate_requires_policy_fields_from_modal()
    {
        var errors = LeaveTypePolicy.ValidateCreate(new CreateLeaveTypeDto
        {
            Name = "Bereavement leave",
            DefaultEntitledDays = 5,
            AccrualMethod = LeaveAccrualMethods.FrontLoaded,
            AppliesToEmploymentTypes = ["Full-time", "Part-time", "Contract"],
        });

        Assert.Null(errors);
    }

    [Fact]
    public void ValidateCreate_rejects_missing_accrual_method()
    {
        var errors = LeaveTypePolicy.ValidateCreate(new CreateLeaveTypeDto
        {
            Name = "Annual Leave",
            DefaultEntitledDays = 21,
            AppliesToEmploymentTypes = ["Full-time"],
        });

        Assert.NotNull(errors);
        Assert.True(errors!.ContainsKey("accrual_method"));
    }

    [Fact]
    public void SerializeEmploymentTypes_round_trips()
    {
        var json = LeaveTypePolicy.SerializeEmploymentTypes(["Full-time", "Contract"]);
        var values = LeaveTypePolicy.DeserializeEmploymentTypes(json);

        Assert.Equal(["Full-time", "Contract"], values);
    }
}
