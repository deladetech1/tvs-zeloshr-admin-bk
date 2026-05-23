using ZelosHR.Api.Persistence.Entities;
using ZelosHR.Api.Shared.Formatting;

namespace ZelosHR.Api.Entities.Employees;

internal static class EmployeeMappingExtensions
{
    public static EmployeeReadDto ToReadDto(this EmployeeEntity entity) => new()
    {
        Id = entity.Id,
        EmployeeCode = entity.EmployeeCode,
        FirstName = entity.FirstName,
        MiddleName = entity.MiddleName,
        LastName = entity.LastName,
        FullName = NameFormatting.ResolveFullName(entity.FullName, entity.FirstName, entity.MiddleName, entity.LastName),
        Email = entity.PersonalEmail,
        Phone = entity.PersonalPhone,
        GhanaCardNumber = entity.GhanaCardNumber,
        JobTitle = entity.JobTitle,
        Department = entity.Department?.Name,
        DepartmentId = entity.DepartmentId?.ToString(),
        EmploymentType = entity.EmploymentType ?? "Full-time",
        LifecycleStatus = entity.LifecycleState,
        ContractType = entity.ContractType,
        ContractEndDate = entity.EmploymentStartDate,
        StartDate = entity.EmploymentStartDate,
        ProbationEndDate = entity.ProbationEndDate,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt,
    };

    public static EmployeeEntity ToEntity(
        this CreateEmployeeServiceWriteDto dto, string tenantId, string orgId, string employeeCode) =>
        new()
        {
            Id = Guid.NewGuid(),
            EmployeeCode = employeeCode,
            TenantId = tenantId,
            OrgId = orgId,
            FirstName = dto.FirstName.Trim(),
            MiddleName = string.IsNullOrWhiteSpace(dto.MiddleName) ? null : dto.MiddleName.Trim(),
            LastName = dto.LastName.Trim(),
            FullName = NameFormatting.BuildFullName(dto.FirstName, dto.MiddleName, dto.LastName),
            DateOfBirth = dto.DateOfBirth,
            Gender = dto.Gender.Trim(),
            Nationality = dto.Nationality.Trim(),
            GhanaCardNumber = NormalizeGhanaCard(dto.GhanaCardNumber),
            PersonalEmail = dto.PersonalEmail.Trim().ToLowerInvariant(),
            PersonalPhone = dto.PersonalPhone.Trim(),
            ResidentialAddress = dto.ResidentialAddress.Trim(),
            GhanaPostGps = dto.GhanaPostGps.Trim(),
            LifecycleState = EmployeeLifecycleStates.PreHire,
            EmploymentStatus = EmploymentStatusValues.Active,
        };

    public static EmployeeEntity ToEntity(this EmployeeWriteDto dto, string tenantId, string orgId, string employeeCode) =>
        new()
        {
            Id = Guid.NewGuid(),
            EmployeeCode = employeeCode,
            TenantId = tenantId,
            OrgId = orgId,
            FirstName = dto.FirstName.Trim(),
            MiddleName = string.IsNullOrWhiteSpace(dto.MiddleName) ? null : dto.MiddleName.Trim(),
            LastName = dto.LastName.Trim(),
            FullName = NameFormatting.BuildFullName(dto.FirstName, dto.MiddleName, dto.LastName),
            DateOfBirth = dto.DateOfBirth ?? DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-25)),
            Gender = dto.Gender?.Trim() ?? "Unknown",
            Nationality = dto.Nationality?.Trim() ?? "Ghanaian",
            GhanaCardNumber = NormalizeGhanaCard(dto.GhanaCardNumber ?? string.Empty),
            PersonalEmail = (dto.Email ?? string.Empty).Trim().ToLowerInvariant(),
            PersonalPhone = dto.Phone?.Trim() ?? string.Empty,
            ResidentialAddress = dto.ResidentialAddress?.Trim() ?? string.Empty,
            GhanaPostGps = dto.GhanaPostGps?.Trim() ?? string.Empty,
            LifecycleState = EmployeeLifecycleStates.PreHire,
            JobTitle = dto.JobTitle,
            DepartmentId = dto.DepartmentId,
            EmploymentType = dto.EmploymentType,
            ContractType = dto.ContractType,
            EmploymentStartDate = dto.StartDate,
            ProbationEndDate = dto.ProbationEndDate,
            EmploymentStatus = EmploymentStatusValues.Active,
        };

    public static string NormalizeGhanaCard(string value) => value.Trim().ToUpperInvariant();
}
