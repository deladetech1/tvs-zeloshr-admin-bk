using Microsoft.AspNetCore.Mvc.ModelBinding;
using ZelosHR.Api.Shared.Validation;

namespace ZelosHR.Api.Tests.Shared;

public class ValidationErrorsTests
{
    [Fact]
    public void FromModelState_HumanizesRequiredFieldMessage()
    {
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("Identity.FullName", "The FullName field is required.");

        var errors = ValidationErrors.FromModelState(modelState);

        Assert.Single(errors);
        Assert.Equal("identity.full_name", errors.Keys.First());
        Assert.Equal("Full name is required.", errors["identity.full_name"]);
    }

    [Fact]
    public void BuildSummary_SingleField_ReturnsFieldMessage()
    {
        var errors = new Dictionary<string, string> { ["name"] = "Department name is required." };

        var summary = ValidationErrors.BuildSummary(errors);

        Assert.Equal("Department name is required.", summary);
    }

    [Fact]
    public void BuildSummary_MultipleFields_ListsFieldNames()
    {
        var errors = new Dictionary<string, string>
        {
            ["identity.full_name"] = "Full name is required.",
            ["status"] = "Status must be 'draft' or 'finalised'.",
        };

        var summary = ValidationErrors.BuildSummary(errors);

        Assert.Contains("2 validation errors", summary);
        Assert.Contains("identity.full_name", summary);
        Assert.Contains("status", summary);
    }

    [Fact]
    public void NormalizeFieldPath_StripsBodyPrefixAndSnakeCases()
    {
        Assert.Equal("identity.full_name", ValidationErrors.NormalizeFieldPath("body.Identity.FullName"));
        Assert.Equal("employment.department_id", ValidationErrors.NormalizeFieldPath("employment.departmentId"));
    }

    [Fact]
    public void ValidationErrorEnvelope_UsesClearDetail()
    {
        var response = ZelosHR.Api.Entities.Shared.Respons<object>.ValidationError(
            new Dictionary<string, string> { ["identity.full_name"] = "Full name is required." });

        Assert.Equal("Full name is required.", response.Detail);
        Assert.Equal("identity.full_name", response.FieldErrors!.Keys.First());
    }
}
