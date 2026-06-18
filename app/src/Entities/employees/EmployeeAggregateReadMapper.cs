using ZelosHR.Api.Entities.Files;
using ZelosHR.Api.Persistence.Entities;

namespace ZelosHR.Api.Entities.Employees;

internal sealed record ReportsToDisplay(
    string? Name,
    string? Position,
    DocumentReadDto? PhotoUrl);

internal static class EmployeeAggregateReadMapper
{
    internal static Dictionary<string, string?>? CustomFieldsOrNull(Dictionary<string, string?>? fields) =>
        fields is { Count: > 0 } ? fields : null;

    internal static IReadOnlyList<DocumentReadDto>? DocumentsOrNull(
        IReadOnlyList<DocumentReadDto>? documents) =>
        documents is { Count: > 0 } ? documents : null;

    internal static bool HasEmployment(EmployeeEntity entity, Dictionary<string, string?>? customFields) =>
        !string.IsNullOrWhiteSpace(entity.JobTitle)
        || entity.DepartmentId is not null
        || entity.BranchId is not null
        || !string.IsNullOrWhiteSpace(entity.EmploymentType)
        || !string.IsNullOrWhiteSpace(entity.EmploymentStatus)
        || !string.IsNullOrWhiteSpace(entity.ContractType)
        || !string.IsNullOrWhiteSpace(entity.WorkArrangement)
        || !string.IsNullOrWhiteSpace(entity.WorkLocation)
        || !string.IsNullOrWhiteSpace(entity.PayGrade)
        || entity.StartDate is not null
        || entity.EmploymentStartDate is not null
        || entity.ProbationEndDate is not null
        || !string.IsNullOrWhiteSpace(entity.WorkingHours)
        || !string.IsNullOrWhiteSpace(entity.NoticePeriod)
        || entity.ReportsToId is not null
        || entity.DottedLineManagerId is not null
        || customFields is { Count: > 0 };

    internal static bool HasCompensation(EmployeeEntity entity, Dictionary<string, string?>? customFields) =>
        entity.GrossSalary is not null
        || !string.IsNullOrWhiteSpace(entity.PayFrequency)
        || !string.IsNullOrWhiteSpace(entity.CurrencyId)
        || entity.AnnualizedCost is not null
        || customFields is { Count: > 0 };

    internal static EmployeeAggregateIdentityReadDto BuildIdentity(
        string fullName,
        EmployeeEntity entity,
        CpUserDto? cp,
        string? workEmail,
        DocumentReadDto? profileUrl,
        Dictionary<string, string?>? customFields) =>
        new()
        {
            FullName = fullName,
            DateOfBirth = entity.DateOfBirth ?? ParseCpDob(cp?.Dob),
            Gender = entity.Gender ?? cp?.Gender,
            Country = entity.Nationality,
            IdType = entity.NationalityIdType,
            IdIssueDate = entity.IdIssueDate,
            IdExpiryDate = entity.IdExpiryDate,
            IdNumber = entity.IdNumber,
            PersonalEmail = entity.PersonalEmail,
            WorkEmail = workEmail,
            Phone = entity.Phone ?? entity.PersonalPhone ?? cp?.Phone,
            LinkedInUrl = entity.LinkedInUrl,
            ResidentialAddress = entity.ResidentialAddress ?? cp?.Address,
            ProfileUrl = profileUrl,
            CustomFields = CustomFieldsOrNull(customFields),
        };

    private static DateOnly? ParseCpDob(string? dob) =>
        DateOnly.TryParse(dob, out var parsed) ? parsed : null;

    internal static EmployeeAggregateEmploymentReadDto? BuildEmployment(
        EmployeeEntity entity,
        Dictionary<string, string?>? customFields,
        ReportsToDisplay? reportsTo = null)
    {
        if (!HasEmployment(entity, customFields))
            return null;

        return new EmployeeAggregateEmploymentReadDto
        {
            JobTitle = entity.JobTitle,
            DepartmentId = entity.DepartmentId,
            BranchId = entity.BranchId,
            DepartmentName = entity.Department?.Name,
            BranchName = entity.Branch?.Name,
            EmploymentType = entity.EmploymentType,
            EmploymentStatus = entity.EmploymentStatus,
            ContractType = entity.ContractType,
            WorkArrangement = entity.WorkArrangement,
            WorkLocation = entity.WorkLocation,
            PayGrade = entity.PayGrade,
            StartDate = entity.StartDate ?? entity.EmploymentStartDate,
            ProbationEndDate = entity.ProbationEndDate,
            WorkingHours = entity.WorkingHours,
            NoticePeriod = entity.NoticePeriod,
            ReportsToId = entity.ReportsToId,
            DottedLineManagerId = entity.DottedLineManagerId,
            ReportsToName = reportsTo?.Name,
            ReportsToPosition = reportsTo?.Position,
            ReportsToPhotoUrl = reportsTo?.PhotoUrl,
            CustomFields = CustomFieldsOrNull(customFields),
        };
    }

    internal static EmployeeAggregateCompensationReadDto? BuildCompensation(
        EmployeeEntity entity,
        Dictionary<string, string?>? customFields,
        string? currencyCode,
        string? currencyName,
        string? currencySymbol)
    {
        if (!HasCompensation(entity, customFields))
            return null;

        return new EmployeeAggregateCompensationReadDto
        {
            GrossSalary = entity.GrossSalary,
            PayFrequency = entity.PayFrequency,
            CurrencyId = entity.CurrencyId,
            CurrencyCode = currencyCode,
            CurrencyName = currencyName,
            CurrencySymbol = currencySymbol,
            AnnualizedCost = entity.AnnualizedCost,
            CustomFields = CustomFieldsOrNull(customFields),
        };
    }
}
