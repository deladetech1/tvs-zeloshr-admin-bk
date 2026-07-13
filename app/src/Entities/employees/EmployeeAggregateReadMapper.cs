using ZelosHR.Api.Entities.Files;
using ZelosHR.Api.Entities.EmploymentTypes;
using ZelosHR.Api.Persistence.Entities;

namespace ZelosHR.Api.Entities.Employees;

internal sealed record ReportsToDisplay(
    Guid Id,
    string? Name,
    string? Position,
    DocumentReadDto? PhotoUrl);

internal sealed record EmploymentTypeDisplay(
    string Id,
    string Name,
    string? Description,
    string Type);

internal static class EmployeeAggregateReadMapper
{
    internal static Dictionary<string, string?>? CustomFieldsOrNull(Dictionary<string, string?>? fields) =>
        fields is { Count: > 0 } ? fields : null;

    internal static IReadOnlyList<DocumentReadDto>? DocumentsOrNull(
        IReadOnlyList<DocumentReadDto>? documents) =>
        documents is { Count: > 0 } ? documents : null;

    internal static bool HasEmployment(
        EmployeeEntity entity,
        Dictionary<string, string?>? customFields,
        bool isLineManager = false,
        bool isHeadOfDepartment = false) =>
        isLineManager
        || isHeadOfDepartment
        || !string.IsNullOrWhiteSpace(entity.JobTitle)
        || entity.DepartmentId is not null
        || entity.BranchId is not null
        || entity.EmploymentTypeId is not null
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
        || entity.NetSalary is not null
        || !string.IsNullOrWhiteSpace(entity.PayFrequency)
        || !string.IsNullOrWhiteSpace(entity.CurrencyId)
        || !string.IsNullOrWhiteSpace(entity.SsnitNumber)
        || entity.AnnualizedCost is not null
        || customFields is { Count: > 0 };

    internal static EmployeeAggregateIdentityReadDto BuildIdentity(
        string fullName,
        EmployeeEntity entity,
        CpUserDto? cp,
        string? workEmail,
        DocumentReadDto? profileUrl,
        IReadOnlyList<EmployeeIdentificationDto>? identifications,
        IReadOnlyList<EmployeeEmergencyContactDto>? emergency,
        Dictionary<string, string?>? customFields) =>
        new()
        {
            FullName = fullName,
            DateOfBirth = entity.DateOfBirth ?? ParseCpDob(cp?.Dob),
            Gender = entity.Gender ?? cp?.Gender,
            Country = entity.Nationality,
            MaritalStatus = entity.MaritalStatus,
            NextOfKinName = entity.NextOfKinName,
            NextOfKinPhone = entity.NextOfKinPhone,
            RelationshipToNextOfKin = entity.RelationshipToNextOfKin,
            PersonalEmail = entity.PersonalEmail,
            WorkEmail = workEmail,
            Phone = entity.Phone ?? entity.PersonalPhone ?? cp?.Phone,
            LinkedInUrl = entity.LinkedInUrl,
            ResidentialAddress = entity.ResidentialAddress ?? cp?.Address,
            ProfileUrl = profileUrl,
            Identifications = identifications is { Count: > 0 } ? identifications : null,
            Emergency = emergency is { Count: > 0 } ? emergency : null,
            CustomFields = CustomFieldsOrNull(customFields),
        };

    private static DateOnly? ParseCpDob(string? dob) =>
        DateOnly.TryParse(dob, out var parsed) ? parsed : null;

    internal static EmployeeAggregateEmploymentReadDto? BuildEmployment(
        EmployeeEntity entity,
        Dictionary<string, string?>? customFields,
        ReportsToDisplay? reportsTo = null,
        ReportsToDisplay? secondaryReportsTo = null,
        EmploymentTypeDisplay? employmentType = null,
        bool isLineManager = false,
        bool isHeadOfDepartment = false)
    {
        if (!HasEmployment(entity, customFields, isLineManager, isHeadOfDepartment))
            return null;

        return new EmployeeAggregateEmploymentReadDto
        {
            JobTitle = entity.JobTitle,
            DepartmentId = entity.DepartmentId,
            BranchId = entity.BranchId,
            DepartmentName = entity.Department?.Name,
            BranchName = entity.Branch?.Name,
            EmploymentType = employmentType is null
                ? null
                : new EmployeeEmploymentTypeRefDto
                {
                    Id = employmentType.Id,
                    Name = employmentType.Name,
                    Description = employmentType.Description,
                    Type = employmentType.Type,
                },
            EmploymentStatus = entity.EmploymentStatus,
            ContractType = entity.ContractType,
            WorkArrangement = entity.WorkArrangement,
            WorkLocation = entity.WorkLocation,
            PayGrade = entity.PayGrade,
            StartDate = entity.StartDate ?? entity.EmploymentStartDate,
            ProbationEndDate = entity.ProbationEndDate,
            WorkingHours = EmployeeWorkingHoursConverter.FromStorage(entity.WorkingHours),
            NoticePeriod = entity.NoticePeriod,
            ReportsTo = reportsTo is null
                ? null
                : new EmployeeReportsToRefDto
                {
                    Id = reportsTo.Id.ToString(),
                    Name = reportsTo.Name,
                    Position = reportsTo.Position,
                    PhotoUrl = reportsTo.PhotoUrl,
                },
            SecondaryReportsTo = secondaryReportsTo is null
                ? null
                : new EmployeeReportsToRefDto
                {
                    Id = secondaryReportsTo.Id.ToString(),
                    Name = secondaryReportsTo.Name,
                    Position = secondaryReportsTo.Position,
                    PhotoUrl = secondaryReportsTo.PhotoUrl,
                },
            IsLineManager = isLineManager,
            IsHeadOfDepartment = isHeadOfDepartment,
            CustomFields = CustomFieldsOrNull(customFields),
        };
    }

    internal static EmployeeAggregateCompensationReadDto? BuildCompensation(
        EmployeeEntity entity,
        Dictionary<string, string?>? customFields,
        IReadOnlyList<EmployeePaymentMethodDto>? payment,
        string? currencyCode,
        string? currencyName,
        string? currencySymbol)
    {
        if (!HasCompensation(entity, customFields) && payment is not { Count: > 0 })
            return null;

        return new EmployeeAggregateCompensationReadDto
        {
            GrossSalary = entity.GrossSalary,
            NetSalary = entity.NetSalary,
            SsnitInsuranceNumber = entity.SsnitNumber,
            PayFrequency = entity.PayFrequency,
            CurrencyId = entity.CurrencyId,
            CurrencyCode = currencyCode,
            CurrencyName = currencyName,
            CurrencySymbol = currencySymbol,
            AnnualizedCost = entity.AnnualizedCost,
            Payment = payment is { Count: > 0 } ? payment : null,
            CustomFields = CustomFieldsOrNull(customFields),
        };
    }

    internal static EmployeeMedicalReadDto? BuildMedical(EmployeeMedicalReadDto? medical)
    {
        if (medical is null)
            return null;

        if (medical.Id is null
            && string.IsNullOrWhiteSpace(medical.BloodGroup)
            && !medical.HasMedicalCondition
            && !medical.TakesRegularMedication
            && string.IsNullOrWhiteSpace(medical.DisabilityStatus)
            && !medical.RequiresAccommodation
            && string.IsNullOrWhiteSpace(medical.AccommodationDetails)
            && string.IsNullOrWhiteSpace(medical.EmergencyMedicalNotes)
            && medical.MedicalConditions is not { Count: > 0 }
            && medical.Allergies is not { Count: > 0 }
            && medical.Medications is not { Count: > 0 })
            return null;

        return medical;
    }
}
