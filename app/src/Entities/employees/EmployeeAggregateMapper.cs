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

    public static CreateEmployeeRequest ToWizardRequest(UpdateEmployeeAggregateRequest update) =>
        new()
        {
            FullName = update.Identity?.FullName ?? string.Empty,
            DateOfBirth = update.Identity?.DateOfBirth,
            Gender = update.Identity?.Gender,
            Nationality = update.Identity?.Nationality,
            NationalityIdType = update.Identity?.NationalityIdType,
            IdNumber = update.Identity?.IdNumber,
            PersonalEmail = update.Identity?.PersonalEmail,
            WorkEmail = update.Identity?.WorkEmail,
            Phone = update.Identity?.Phone,
            LinkedInUrl = update.Identity?.LinkedInUrl,
            ResidentialAddress = update.Identity?.ResidentialAddress,
            GpsAddress = update.Identity?.GpsAddress,
            State = update.Identity?.State,
            JobTitle = update.Employment?.JobTitle,
            DepartmentId = update.Employment?.DepartmentId,
            BranchId = update.Employment?.BranchId,
            EmploymentType = update.Employment?.EmploymentType,
            WorkArrangement = update.Employment?.WorkArrangement,
            WorkLocation = update.Employment?.WorkLocation,
            PayGrade = update.Employment?.PayGrade,
            StartDate = update.Employment?.StartDate,
            ProbationEndDate = update.Employment?.ProbationEndDate,
            WorkingHours = update.Employment?.WorkingHours,
            NoticePeriod = update.Employment?.NoticePeriod,
            ReportsToId = update.Employment?.ReportsToId,
            DottedLineManagerId = update.Employment?.DottedLineManagerId,
            GrossSalary = update.Compensation?.GrossSalary,
            PayFrequency = update.Compensation?.PayFrequency,
            SalaryEffectiveFrom = update.Compensation?.SalaryEffectiveFrom,
            Currency = update.Compensation?.Currency,
            SsnitNumber = update.Compensation?.SsnitNumber,
            TinNumber = update.Compensation?.TinNumber,
            Tier2PensionProvider = update.Compensation?.Tier2PensionProvider,
            Tier3PensionProvider = update.Compensation?.Tier3PensionProvider,
            PaymentMethod = update.Compensation?.PaymentMethod,
            BankAccountNumber = update.Compensation?.BankAccountNumber,
            MobileMoneyNumber = update.Compensation?.MobileMoneyNumber,
        };

    public static EmployeeEducationWriteDto ToEducationWrite(EmployeeEducationUpsertDto dto) =>
        new(dto.Institution, dto.Degree, dto.FieldOfStudy, dto.StartYear, dto.EndYear, dto.IsCurrent);

    public static EmployeeCertificationWriteDto ToCertificationWrite(EmployeeCertificationUpsertDto dto) =>
        new(dto.Name, dto.IssuingBody, dto.IssueDate, dto.ExpiryDate, dto.CredentialId);

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
