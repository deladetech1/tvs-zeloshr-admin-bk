using FluentAssertions;
using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Shared.Validation;

namespace ZelosHR.Api.Tests.Employees;

public class EmployeeIdentityFieldValidatorTests
{
    private static EmployeeAggregateIdentityDto ValidIdentity(
        string? phone = "+233201234567",
        string? personalEmail = "ada.personal@example.com",
        string? workEmail = "ada.work@example.com",
        string? linkedInUrl = "https://linkedin.com/in/adalovelace",
        string? gender = "female",
        DateOnly? dateOfBirth = null,
        IReadOnlyList<EmployeeEmergencyContactUpsertDto>? emergency = null) =>
        new()
        {
            FullName = "Ada Lovelace",
            Phone = phone,
            PersonalEmail = personalEmail,
            WorkEmail = workEmail,
            LinkedInUrl = linkedInUrl,
            Gender = gender,
            DateOfBirth = dateOfBirth ?? new DateOnly(1990, 5, 15),
            Emergency = emergency,
        };

    [Theory]
    [InlineData("+233201234567")]
    [InlineData("+1 202 555 0100")]
    [InlineData("+44-20-7946-0958")]
    public void ValidateForCreate_AcceptsE164PhoneVariants(string phone)
    {
        var identity = ValidIdentity(phone: phone);

        EmployeeIdentityFieldValidator.ValidateForCreate(identity).Should().BeNull();
    }

    [Theory]
    [InlineData("0201234567")]
    [InlineData("233201234567")]
    [InlineData("+")]
    [InlineData("+0123456789")]
    public void ValidateForCreate_RejectsInvalidPhone(string phone)
    {
        var identity = ValidIdentity(phone: phone);

        var errors = EmployeeIdentityFieldValidator.ValidateForCreate(identity);

        errors.Should().NotBeNull();
        errors!.Should().ContainKey("identity.phone");
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("missing-at-sign.com")]
    [InlineData("spaces in@email.com")]
    public void ValidateForCreate_RejectsInvalidEmail(string email)
    {
        var identity = ValidIdentity(personalEmail: email);

        var errors = EmployeeIdentityFieldValidator.ValidateForCreate(identity);

        errors.Should().NotBeNull();
        errors!.Should().ContainKey("identity.personal_email");
    }

    [Fact]
    public void ValidateForCreate_RejectsFutureDateOfBirth()
    {
        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var identity = ValidIdentity(dateOfBirth: tomorrow);

        var errors = EmployeeIdentityFieldValidator.ValidateForCreate(identity);

        errors.Should().NotBeNull();
        errors!.Should().ContainKey("identity.date_of_birth");
    }

    [Fact]
    public void ValidateForCreate_RejectsInvalidGender()
    {
        var identity = ValidIdentity(gender: "unknown");

        var errors = EmployeeIdentityFieldValidator.ValidateForCreate(identity);

        errors.Should().NotBeNull();
        errors!.Should().ContainKey("identity.gender");
    }

    [Fact]
    public void ValidateForCreate_RejectsInvalidLinkedInUrl()
    {
        var identity = ValidIdentity(linkedInUrl: "ftp://linkedin.com/in/ada");

        var errors = EmployeeIdentityFieldValidator.ValidateForCreate(identity);

        errors.Should().NotBeNull();
        errors!.Should().ContainKey("identity.linked_in_url");
    }

    [Fact]
    public void ValidateForCreate_RequiresEmergencyContactName()
    {
        var identity = ValidIdentity(emergency:
        [
            new EmployeeEmergencyContactUpsertDto(null, "", "+233503448860", null),
        ]);

        var errors = EmployeeIdentityFieldValidator.ValidateForCreate(identity);

        errors.Should().NotBeNull();
        errors!.Should().ContainKey("identity.emergency[0].emergency_contact_name");
    }

    [Fact]
    public void ValidateForUpdate_SkipsNullIdentity()
    {
        EmployeeIdentityFieldValidator.ValidateForUpdate(null).Should().BeNull();
    }

    [Fact]
    public void ValidateForUpdate_OnlyValidatesProvidedFields()
    {
        var identity = new EmployeeAggregateIdentityDto { Phone = "invalid" };

        var errors = EmployeeIdentityFieldValidator.ValidateForUpdate(identity);

        errors.Should().NotBeNull();
        errors!.Should().ContainKey("identity.phone");
        errors.Should().NotContainKey("identity.full_name");
    }

    [Fact]
    public void ValidateRegistrationPatch_RejectsInvalidWorkEmail()
    {
        var dto = new CreateEmployeeRequest { WorkEmail = "bad-email" };

        var errors = EmployeeIdentityFieldValidator.ValidateRegistrationPatch(dto);

        errors.Should().NotBeNull();
        errors!.Should().ContainKey("identity.work_email");
    }

    [Theory]
    [InlineData("+233 20 123 4567", "+233201234567")]
    [InlineData("+1-202-555-0100", "+12025550100")]
    public void NormalizePhone_StripsFormatting(string input, string expected)
    {
        EmployeeIdentityFieldValidator.NormalizePhone(input).Should().Be(expected);
    }
}
