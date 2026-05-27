using System.Text.Json;

namespace ZelosHR.Api.Entities.Employees;

internal static class EmployeeAggregateMapper
{
    public static CreateEmployeeRequest ToWizardRequest(CreateEmployeeAggregateRequest aggregate) =>
        new()
        {
            FullName = aggregate.Identity.FullName,
            DateOfBirth = aggregate.Identity.DateOfBirth,
            Gender = aggregate.Identity.Gender,
            Nationality = aggregate.Identity.Nationality,
            NationalityIdType = aggregate.Identity.NationalityIdType,
            IdNumber = aggregate.Identity.IdNumber,
            PersonalEmail = aggregate.Identity.PersonalEmail,
            WorkEmail = aggregate.Identity.WorkEmail,
            Phone = aggregate.Identity.Phone,
            LinkedInUrl = aggregate.Identity.LinkedInUrl,
            ResidentialAddress = aggregate.Identity.ResidentialAddress,
            GpsAddress = aggregate.Identity.GpsAddress,
            State = aggregate.Identity.State,
            JobTitle = aggregate.Employment?.JobTitle,
            DepartmentId = aggregate.Employment?.DepartmentId,
            BranchId = aggregate.Employment?.BranchId,
            EmploymentType = aggregate.Employment?.EmploymentType,
            WorkArrangement = aggregate.Employment?.WorkArrangement,
            WorkLocation = aggregate.Employment?.WorkLocation,
            PayGrade = aggregate.Employment?.PayGrade,
            StartDate = aggregate.Employment?.StartDate,
            ProbationEndDate = aggregate.Employment?.ProbationEndDate,
            WorkingHours = aggregate.Employment?.WorkingHours,
            NoticePeriod = aggregate.Employment?.NoticePeriod,
            ReportsToId = aggregate.Employment?.ReportsToId,
            DottedLineManagerId = aggregate.Employment?.DottedLineManagerId,
            GrossSalary = aggregate.Compensation?.GrossSalary,
            PayFrequency = aggregate.Compensation?.PayFrequency,
            SalaryEffectiveFrom = aggregate.Compensation?.SalaryEffectiveFrom,
            Currency = aggregate.Compensation?.Currency,
            SsnitNumber = aggregate.Compensation?.SsnitNumber,
            TinNumber = aggregate.Compensation?.TinNumber,
            Tier2PensionProvider = aggregate.Compensation?.Tier2PensionProvider,
            Tier3PensionProvider = aggregate.Compensation?.Tier3PensionProvider,
            PaymentMethod = aggregate.Compensation?.PaymentMethod,
            BankAccountNumber = aggregate.Compensation?.BankAccountNumber,
            MobileMoneyNumber = aggregate.Compensation?.MobileMoneyNumber,
            Finalise = string.Equals(aggregate.Status, "finalised", StringComparison.OrdinalIgnoreCase),
        };

    public static string SerializeCustomFields(Dictionary<string, string?>? fields) =>
        fields is null || fields.Count == 0
            ? "{}"
            : JsonSerializer.Serialize(fields);

    public static Dictionary<string, string?> DeserializeCustomFields(string? json)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "{}")
            return new Dictionary<string, string?>();

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string?>>(json) ?? new Dictionary<string, string?>();
        }
        catch
        {
            return new Dictionary<string, string?>();
        }
    }
}
