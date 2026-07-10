using ZelosHR.Api.Persistence.Entities;

namespace ZelosHR.Api.Entities.Employees;

public interface IEmployeeEmergencyContactRepository
{
    Task<bool> EmployeeExistsAsync(Guid employeeId, string tenantId, string orgId, CancellationToken ct = default);
    Task<IReadOnlyList<EmployeeEmergencyContactEntity>> ListByEmployeeAsync(
        Guid employeeId, string tenantId, string orgId, CancellationToken ct = default);
    Task<EmployeeEmergencyContactEntity?> GetByIdAsync(
        Guid id, Guid employeeId, string tenantId, string orgId, CancellationToken ct = default);
    Task<EmployeeEmergencyContactEntity> AddAsync(EmployeeEmergencyContactEntity entity, CancellationToken ct = default);
    Task UpdateAsync(EmployeeEmergencyContactEntity entity, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, Guid employeeId, string tenantId, string orgId, CancellationToken ct = default);
}

public interface IEmployeePaymentMethodRepository
{
    Task<bool> EmployeeExistsAsync(Guid employeeId, string tenantId, string orgId, CancellationToken ct = default);
    Task<IReadOnlyList<EmployeePaymentMethodEntity>> ListByEmployeeAsync(
        Guid employeeId, string tenantId, string orgId, CancellationToken ct = default);
    Task<EmployeePaymentMethodEntity?> GetByIdAsync(
        Guid id, Guid employeeId, string tenantId, string orgId, CancellationToken ct = default);
    Task<EmployeePaymentMethodEntity> AddAsync(EmployeePaymentMethodEntity entity, CancellationToken ct = default);
    Task UpdateAsync(EmployeePaymentMethodEntity entity, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, Guid employeeId, string tenantId, string orgId, CancellationToken ct = default);
}

public interface IEmployeeMedicalProfileRepository
{
    Task<bool> EmployeeExistsAsync(Guid employeeId, string tenantId, string orgId, CancellationToken ct = default);
    Task<EmployeeMedicalProfileEntity?> GetByEmployeeAsync(
        Guid employeeId, string tenantId, string orgId, CancellationToken ct = default);
    Task<EmployeeMedicalProfileEntity> AddAsync(EmployeeMedicalProfileEntity entity, CancellationToken ct = default);
    Task UpdateAsync(EmployeeMedicalProfileEntity entity, CancellationToken ct = default);
}

public interface IEmployeeMedicalConditionRepository
{
    Task<bool> EmployeeExistsAsync(Guid employeeId, string tenantId, string orgId, CancellationToken ct = default);
    Task<IReadOnlyList<EmployeeMedicalConditionEntity>> ListByEmployeeAsync(
        Guid employeeId, string tenantId, string orgId, CancellationToken ct = default);
    Task<EmployeeMedicalConditionEntity?> GetByIdAsync(
        Guid id, Guid employeeId, string tenantId, string orgId, CancellationToken ct = default);
    Task<EmployeeMedicalConditionEntity> AddAsync(EmployeeMedicalConditionEntity entity, CancellationToken ct = default);
    Task UpdateAsync(EmployeeMedicalConditionEntity entity, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, Guid employeeId, string tenantId, string orgId, CancellationToken ct = default);
}

public interface IEmployeeAllergyRepository
{
    Task<bool> EmployeeExistsAsync(Guid employeeId, string tenantId, string orgId, CancellationToken ct = default);
    Task<IReadOnlyList<EmployeeAllergyEntity>> ListByEmployeeAsync(
        Guid employeeId, string tenantId, string orgId, CancellationToken ct = default);
    Task<EmployeeAllergyEntity?> GetByIdAsync(
        Guid id, Guid employeeId, string tenantId, string orgId, CancellationToken ct = default);
    Task<EmployeeAllergyEntity> AddAsync(EmployeeAllergyEntity entity, CancellationToken ct = default);
    Task UpdateAsync(EmployeeAllergyEntity entity, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, Guid employeeId, string tenantId, string orgId, CancellationToken ct = default);
}

public interface IEmployeeMedicationRepository
{
    Task<bool> EmployeeExistsAsync(Guid employeeId, string tenantId, string orgId, CancellationToken ct = default);
    Task<IReadOnlyList<EmployeeMedicationEntity>> ListByEmployeeAsync(
        Guid employeeId, string tenantId, string orgId, CancellationToken ct = default);
    Task<EmployeeMedicationEntity?> GetByIdAsync(
        Guid id, Guid employeeId, string tenantId, string orgId, CancellationToken ct = default);
    Task<EmployeeMedicationEntity> AddAsync(EmployeeMedicationEntity entity, CancellationToken ct = default);
    Task UpdateAsync(EmployeeMedicationEntity entity, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, Guid employeeId, string tenantId, string orgId, CancellationToken ct = default);
}

public interface IEmployeeSkillRepository
{
    Task<bool> EmployeeExistsAsync(Guid employeeId, string tenantId, string orgId, CancellationToken ct = default);
    Task<IReadOnlyList<EmployeeSkillEntity>> ListByEmployeeAsync(
        Guid employeeId, string tenantId, string orgId, CancellationToken ct = default);
    Task<EmployeeSkillEntity?> GetByIdAsync(
        Guid id, Guid employeeId, string tenantId, string orgId, CancellationToken ct = default);
    Task<EmployeeSkillEntity> AddAsync(EmployeeSkillEntity entity, CancellationToken ct = default);
    Task UpdateAsync(EmployeeSkillEntity entity, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, Guid employeeId, string tenantId, string orgId, CancellationToken ct = default);
}

public interface IEmployeeExperienceRepository
{
    Task<bool> EmployeeExistsAsync(Guid employeeId, string tenantId, string orgId, CancellationToken ct = default);
    Task<IReadOnlyList<EmployeeExperienceEntity>> ListByEmployeeAsync(
        Guid employeeId, string tenantId, string orgId, CancellationToken ct = default);
    Task<EmployeeExperienceEntity?> GetByIdAsync(
        Guid id, Guid employeeId, string tenantId, string orgId, CancellationToken ct = default);
    Task<EmployeeExperienceEntity> AddAsync(EmployeeExperienceEntity entity, CancellationToken ct = default);
    Task UpdateAsync(EmployeeExperienceEntity entity, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, Guid employeeId, string tenantId, string orgId, CancellationToken ct = default);
}

public interface IEmployeeReferralRepository
{
    Task<bool> EmployeeExistsAsync(Guid employeeId, string tenantId, string orgId, CancellationToken ct = default);
    Task<IReadOnlyList<EmployeeReferralEntity>> ListByEmployeeAsync(
        Guid employeeId, string tenantId, string orgId, CancellationToken ct = default);
    Task<EmployeeReferralEntity?> GetByIdAsync(
        Guid id, Guid employeeId, string tenantId, string orgId, CancellationToken ct = default);
    Task<EmployeeReferralEntity> AddAsync(EmployeeReferralEntity entity, CancellationToken ct = default);
    Task UpdateAsync(EmployeeReferralEntity entity, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, Guid employeeId, string tenantId, string orgId, CancellationToken ct = default);
}
