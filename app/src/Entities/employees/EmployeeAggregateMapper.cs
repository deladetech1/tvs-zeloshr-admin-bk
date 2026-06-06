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
            Country = aggregate.Identity.Country,
            IdType = aggregate.Identity.IdType,
            IdIssueDate = aggregate.Identity.IdIssueDate,
            IdExpiryDate = aggregate.Identity.IdExpiryDate,
            IdNumber = aggregate.Identity.IdNumber,
            PersonalEmail = aggregate.Identity.PersonalEmail,
            WorkEmail = aggregate.Identity.WorkEmail,
            Phone = aggregate.Identity.Phone,
            LinkedInUrl = aggregate.Identity.LinkedInUrl,
            ResidentialAddress = aggregate.Identity.ResidentialAddress,
            ProfileUrl = aggregate.Identity.ProfileUrl,
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
            CurrencyId = aggregate.Compensation?.CurrencyId,
            Finalise = string.Equals(aggregate.Status, "finalised", StringComparison.OrdinalIgnoreCase),
        };

    public static CreateEmployeeRequest ToWizardRequest(UpdateEmployeeAggregateRequest update) =>
        new()
        {
            FullName = update.Identity?.FullName ?? string.Empty,
            DateOfBirth = update.Identity?.DateOfBirth,
            Gender = update.Identity?.Gender,
            Country = update.Identity?.Country,
            IdType = update.Identity?.IdType,
            IdIssueDate = update.Identity?.IdIssueDate,
            IdExpiryDate = update.Identity?.IdExpiryDate,
            IdNumber = update.Identity?.IdNumber,
            PersonalEmail = update.Identity?.PersonalEmail,
            WorkEmail = update.Identity?.WorkEmail,
            Phone = update.Identity?.Phone,
            LinkedInUrl = update.Identity?.LinkedInUrl,
            ResidentialAddress = update.Identity?.ResidentialAddress,
            ProfileUrl = update.Identity?.ProfileUrl,
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
            CurrencyId = update.Compensation?.CurrencyId,
        };

    public static EmployeeCertificationWriteDto ToCertificationWrite(EmployeeCertificationUpsertDto dto) =>
        new(dto.Name, dto.IssuingBody, dto.IssueDate, dto.ExpiryDate, dto.CredentialUrl, dto.CustomFields);

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
