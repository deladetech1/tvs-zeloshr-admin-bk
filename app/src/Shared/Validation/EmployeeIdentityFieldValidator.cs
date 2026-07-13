using System.Net.Mail;
using System.Text.RegularExpressions;
using ZelosHR.Api.Entities.Employees;

namespace ZelosHR.Api.Shared.Validation;

/// <summary>
/// Shared identity field validation for employee create/update (E.164 phone, email, DOB, URLs).
/// </summary>
public static partial class EmployeeIdentityFieldValidator
{
    public const int MaxEmailLength = 255;
    public const int MaxPersonNameLength = 200;
    public const int MaxUrlLength = 500;

    private static readonly DateOnly MinDateOfBirth = new(1900, 1, 1);

    public static Dictionary<string, string>? ValidateForCreate(EmployeeAggregateIdentityDto identity)
    {
        var errors = new Dictionary<string, string>(StringComparer.Ordinal);

        ValidateFullName(identity.FullName, "identity.full_name", errors, required: true);
        ValidatePhoneRequired(identity.Phone, "identity.phone", errors);
        ValidateOptionalEmail(identity.PersonalEmail, "identity.personal_email", errors);
        ValidateOptionalEmail(identity.WorkEmail, "identity.work_email", errors);
        ValidateOptionalUrl(identity.LinkedInUrl, "identity.linked_in_url", errors);
        ValidateDateOfBirth(identity.DateOfBirth, "identity.date_of_birth", errors);
        ValidateGender(identity.Gender, "identity.gender", errors);
        ValidateOptionalPhone(identity.NextOfKinPhone, "identity.next_of_kin_phone", errors);
        ValidateEmergencyContacts(identity.Emergency, errors);

        return errors.Count == 0 ? null : errors;
    }

    public static Dictionary<string, string>? ValidateForUpdate(EmployeeAggregateIdentityDto? identity)
    {
        if (identity is null)
            return null;

        var errors = new Dictionary<string, string>(StringComparer.Ordinal);

        if (!string.IsNullOrWhiteSpace(identity.FullName))
            ValidateFullName(identity.FullName, "identity.full_name", errors, required: false);

        if (!string.IsNullOrWhiteSpace(identity.Phone))
            ValidatePhoneFormat(identity.Phone, "identity.phone", errors);

        ValidateOptionalEmail(identity.PersonalEmail, "identity.personal_email", errors);
        ValidateOptionalEmail(identity.WorkEmail, "identity.work_email", errors);
        ValidateOptionalUrl(identity.LinkedInUrl, "identity.linked_in_url", errors);
        ValidateDateOfBirth(identity.DateOfBirth, "identity.date_of_birth", errors);
        ValidateGender(identity.Gender, "identity.gender", errors);
        ValidateOptionalPhone(identity.NextOfKinPhone, "identity.next_of_kin_phone", errors);
        ValidateEmergencyContacts(identity.Emergency, errors);

        return errors.Count == 0 ? null : errors;
    }

    /// <summary>Validates identity fields present on registration/personal-contact patches.</summary>
    public static Dictionary<string, string>? ValidateRegistrationPatch(CreateEmployeeRequest dto)
    {
        var errors = new Dictionary<string, string>(StringComparer.Ordinal);

        if (!string.IsNullOrWhiteSpace(dto.FullName))
            ValidateFullName(dto.FullName, "identity.full_name", errors, required: false);

        if (!string.IsNullOrWhiteSpace(dto.Phone))
            ValidatePhoneFormat(dto.Phone, "identity.phone", errors);

        ValidateOptionalEmail(dto.PersonalEmail, "identity.personal_email", errors);
        ValidateOptionalEmail(dto.WorkEmail, "identity.work_email", errors);
        ValidateOptionalUrl(dto.LinkedInUrl, "identity.linked_in_url", errors);
        ValidateDateOfBirth(dto.DateOfBirth, "identity.date_of_birth", errors);
        ValidateGender(dto.Gender, "identity.gender", errors);
        ValidateOptionalPhone(dto.NextOfKinPhone, "identity.next_of_kin_phone", errors);

        return errors.Count == 0 ? null : errors;
    }

    internal static string? NormalizePhone(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        var builder = new System.Text.StringBuilder(trimmed.Length);
        foreach (var ch in trimmed)
        {
            if (char.IsDigit(ch) || (ch == '+' && builder.Length == 0))
                builder.Append(ch);
        }

        return builder.Length == 0 ? null : builder.ToString();
    }

    private static void ValidateFullName(
        string? value,
        string field,
        Dictionary<string, string> errors,
        bool required)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            if (required)
                errors[field] = "Full name is required.";
            return;
        }

        var trimmed = value.Trim();
        if (trimmed.Length < 2)
            errors[field] = "Full name must be at least 2 characters.";
        else if (trimmed.Length > MaxPersonNameLength)
            errors[field] = $"Full name must be at most {MaxPersonNameLength} characters.";
    }

    private static void ValidatePhoneRequired(string? value, string field, Dictionary<string, string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors[field] = "Phone is required.";
            return;
        }

        ValidatePhoneFormat(value, field, errors);
    }

    private static void ValidateOptionalPhone(string? value, string field, Dictionary<string, string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        ValidatePhoneFormat(value, field, errors);
    }

    private static void ValidatePhoneFormat(string value, string field, Dictionary<string, string> errors)
    {
        var normalized = NormalizePhone(value);
        if (normalized is null || !E164PhonePattern().IsMatch(normalized))
        {
            errors[field] =
                "Phone must be in international E.164 format (e.g. +233201234567). "
                + "Use a leading + and country code with no spaces.";
        }
    }

    private static void ValidateOptionalEmail(string? value, string field, Dictionary<string, string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        if (!IsValidEmail(value))
            errors[field] = "Enter a valid email address.";
    }

    private static bool IsValidEmail(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.Length > MaxEmailLength || trimmed.Contains(' ', StringComparison.Ordinal))
            return false;

        try
        {
            var address = new MailAddress(trimmed);
            return address.Address.Equals(trimmed, StringComparison.OrdinalIgnoreCase)
                && trimmed.Contains('@')
                && trimmed.Contains('.')
                && trimmed.IndexOf('@') < trimmed.LastIndexOf('.');
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static void ValidateOptionalUrl(string? value, string field, Dictionary<string, string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        var trimmed = value.Trim();
        if (trimmed.Length > MaxUrlLength)
        {
            errors[field] = $"URL must be at most {MaxUrlLength} characters.";
            return;
        }

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            errors[field] = "Enter a valid http or https URL.";
        }
    }

    private static void ValidateDateOfBirth(DateOnly? value, string field, Dictionary<string, string> errors)
    {
        if (value is null)
            return;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (value.Value > today)
            errors[field] = "Date of birth cannot be in the future.";
        else if (value.Value < MinDateOfBirth)
            errors[field] = "Date of birth is not valid.";
    }

    private static void ValidateGender(string? value, string field, Dictionary<string, string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        var normalized = value.Trim().ToLowerInvariant();
        if (!EmployeeFieldOptions.Genders.Contains(normalized))
            errors[field] = "Gender must be one of: male, female, other.";
    }

    private static void ValidateEmergencyContacts(
        IReadOnlyList<EmployeeEmergencyContactUpsertDto>? contacts,
        Dictionary<string, string> errors)
    {
        if (contacts is null)
            return;

        for (var i = 0; i < contacts.Count; i++)
        {
            var contact = contacts[i];
            if (string.IsNullOrWhiteSpace(contact.EmergencyContactName))
            {
                errors[$"identity.emergency[{i}].emergency_contact_name"] =
                    "Emergency contact name is required.";
            }

            ValidateOptionalPhone(
                contact.EmergencyContactPhone,
                $"identity.emergency[{i}].emergency_contact_phone",
                errors);
        }
    }

    [GeneratedRegex(@"^\+[1-9]\d{7,14}$", RegexOptions.CultureInvariant)]
    private static partial Regex E164PhonePattern();
}
