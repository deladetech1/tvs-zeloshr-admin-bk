using Microsoft.EntityFrameworkCore;
using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Persistence.Entities;

namespace ZelosHR.Api.Persistence.Repositories;

internal static class EmployeeSubResourceRepositoryHelpers
{
    internal static Task<bool> EmployeeExistsAsync(
        ZelosHrDbContext db, Guid employeeId, string tenantId, string orgId, CancellationToken ct) =>
        db.Employees.AsNoTracking()
            .AnyAsync(e => e.Id == employeeId && e.TenantId == tenantId && e.OrgId == orgId && !e.IsDeleted, ct);

    internal static async Task<bool> DeleteChildAsync<T>(
        ZelosHrDbContext db,
        DbSet<T> set,
        Guid id,
        Guid employeeId,
        string tenantId,
        string orgId,
        CancellationToken ct) where T : class
    {
        if (!await EmployeeExistsAsync(db, employeeId, tenantId, orgId, ct))
            return false;

        var rows = await set.Where(x => EF.Property<Guid>(x, "Id") == id && EF.Property<Guid>(x, "EmployeeId") == employeeId)
            .ExecuteDeleteAsync(ct);
        return rows > 0;
    }
}

public sealed class EmployeeEmergencyContactRepository(ZelosHrDbContext db) : IEmployeeEmergencyContactRepository
{
    public Task<bool> EmployeeExistsAsync(Guid employeeId, string tenantId, string orgId, CancellationToken ct = default) =>
        EmployeeSubResourceRepositoryHelpers.EmployeeExistsAsync(db, employeeId, tenantId, orgId, ct);

    public async Task<IReadOnlyList<EmployeeEmergencyContactEntity>> ListByEmployeeAsync(
        Guid employeeId, string tenantId, string orgId, CancellationToken ct = default) =>
        await db.EmployeeEmergencyContacts.AsNoTracking()
            .Where(x => x.EmployeeId == employeeId)
            .OrderBy(x => x.EmergencyContactName)
            .ToListAsync(ct);

    public Task<EmployeeEmergencyContactEntity?> GetByIdAsync(
        Guid id, Guid employeeId, string tenantId, string orgId, CancellationToken ct = default) =>
        db.EmployeeEmergencyContacts.FirstOrDefaultAsync(x => x.Id == id && x.EmployeeId == employeeId, ct);

    public async Task<EmployeeEmergencyContactEntity> AddAsync(EmployeeEmergencyContactEntity entity, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        entity.CreatedAt = now;
        entity.UpdatedAt = now;
        db.EmployeeEmergencyContacts.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task UpdateAsync(EmployeeEmergencyContactEntity entity, CancellationToken ct = default)
    {
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        db.EmployeeEmergencyContacts.Update(entity);
        await db.SaveChangesAsync(ct);
    }

    public Task<bool> DeleteAsync(Guid id, Guid employeeId, string tenantId, string orgId, CancellationToken ct = default) =>
        EmployeeSubResourceRepositoryHelpers.DeleteChildAsync(
            db, db.EmployeeEmergencyContacts, id, employeeId, tenantId, orgId, ct);
}

public sealed class EmployeePaymentMethodRepository(ZelosHrDbContext db) : IEmployeePaymentMethodRepository
{
    public Task<bool> EmployeeExistsAsync(Guid employeeId, string tenantId, string orgId, CancellationToken ct = default) =>
        EmployeeSubResourceRepositoryHelpers.EmployeeExistsAsync(db, employeeId, tenantId, orgId, ct);

    public async Task<IReadOnlyList<EmployeePaymentMethodEntity>> ListByEmployeeAsync(
        Guid employeeId, string tenantId, string orgId, CancellationToken ct = default) =>
        await db.EmployeePaymentMethods.AsNoTracking()
            .Where(x => x.EmployeeId == employeeId)
            .OrderByDescending(x => x.IsPrimary)
            .ThenBy(x => x.PaymentMode)
            .ToListAsync(ct);

    public Task<EmployeePaymentMethodEntity?> GetByIdAsync(
        Guid id, Guid employeeId, string tenantId, string orgId, CancellationToken ct = default) =>
        db.EmployeePaymentMethods.FirstOrDefaultAsync(x => x.Id == id && x.EmployeeId == employeeId, ct);

    public async Task<EmployeePaymentMethodEntity> AddAsync(EmployeePaymentMethodEntity entity, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        entity.CreatedAt = now;
        entity.UpdatedAt = now;
        db.EmployeePaymentMethods.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task UpdateAsync(EmployeePaymentMethodEntity entity, CancellationToken ct = default)
    {
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        db.EmployeePaymentMethods.Update(entity);
        await db.SaveChangesAsync(ct);
    }

    public Task<bool> DeleteAsync(Guid id, Guid employeeId, string tenantId, string orgId, CancellationToken ct = default) =>
        EmployeeSubResourceRepositoryHelpers.DeleteChildAsync(
            db, db.EmployeePaymentMethods, id, employeeId, tenantId, orgId, ct);
}

public sealed class EmployeeMedicalProfileRepository(ZelosHrDbContext db) : IEmployeeMedicalProfileRepository
{
    public Task<bool> EmployeeExistsAsync(Guid employeeId, string tenantId, string orgId, CancellationToken ct = default) =>
        EmployeeSubResourceRepositoryHelpers.EmployeeExistsAsync(db, employeeId, tenantId, orgId, ct);

    public Task<EmployeeMedicalProfileEntity?> GetByEmployeeAsync(
        Guid employeeId, string tenantId, string orgId, CancellationToken ct = default) =>
        db.EmployeeMedicalProfiles.AsNoTracking()
            .FirstOrDefaultAsync(x => x.EmployeeId == employeeId, ct);

    public async Task<EmployeeMedicalProfileEntity> AddAsync(EmployeeMedicalProfileEntity entity, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        entity.CreatedAt = now;
        entity.UpdatedAt = now;
        db.EmployeeMedicalProfiles.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task UpdateAsync(EmployeeMedicalProfileEntity entity, CancellationToken ct = default)
    {
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        db.EmployeeMedicalProfiles.Update(entity);
        await db.SaveChangesAsync(ct);
    }
}

public sealed class EmployeeMedicalConditionRepository(ZelosHrDbContext db) : IEmployeeMedicalConditionRepository
{
    public Task<bool> EmployeeExistsAsync(Guid employeeId, string tenantId, string orgId, CancellationToken ct = default) =>
        EmployeeSubResourceRepositoryHelpers.EmployeeExistsAsync(db, employeeId, tenantId, orgId, ct);

    public async Task<IReadOnlyList<EmployeeMedicalConditionEntity>> ListByEmployeeAsync(
        Guid employeeId, string tenantId, string orgId, CancellationToken ct = default) =>
        await db.EmployeeMedicalConditions.AsNoTracking()
            .Where(x => x.EmployeeId == employeeId)
            .OrderBy(x => x.Condition)
            .ToListAsync(ct);

    public Task<EmployeeMedicalConditionEntity?> GetByIdAsync(
        Guid id, Guid employeeId, string tenantId, string orgId, CancellationToken ct = default) =>
        db.EmployeeMedicalConditions.FirstOrDefaultAsync(x => x.Id == id && x.EmployeeId == employeeId, ct);

    public async Task<EmployeeMedicalConditionEntity> AddAsync(EmployeeMedicalConditionEntity entity, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        entity.CreatedAt = now;
        entity.UpdatedAt = now;
        db.EmployeeMedicalConditions.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task UpdateAsync(EmployeeMedicalConditionEntity entity, CancellationToken ct = default)
    {
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        db.EmployeeMedicalConditions.Update(entity);
        await db.SaveChangesAsync(ct);
    }

    public Task<bool> DeleteAsync(Guid id, Guid employeeId, string tenantId, string orgId, CancellationToken ct = default) =>
        EmployeeSubResourceRepositoryHelpers.DeleteChildAsync(
            db, db.EmployeeMedicalConditions, id, employeeId, tenantId, orgId, ct);
}

public sealed class EmployeeAllergyRepository(ZelosHrDbContext db) : IEmployeeAllergyRepository
{
    public Task<bool> EmployeeExistsAsync(Guid employeeId, string tenantId, string orgId, CancellationToken ct = default) =>
        EmployeeSubResourceRepositoryHelpers.EmployeeExistsAsync(db, employeeId, tenantId, orgId, ct);

    public async Task<IReadOnlyList<EmployeeAllergyEntity>> ListByEmployeeAsync(
        Guid employeeId, string tenantId, string orgId, CancellationToken ct = default) =>
        await db.EmployeeAllergies.AsNoTracking()
            .Where(x => x.EmployeeId == employeeId)
            .OrderBy(x => x.Allergen)
            .ToListAsync(ct);

    public Task<EmployeeAllergyEntity?> GetByIdAsync(
        Guid id, Guid employeeId, string tenantId, string orgId, CancellationToken ct = default) =>
        db.EmployeeAllergies.FirstOrDefaultAsync(x => x.Id == id && x.EmployeeId == employeeId, ct);

    public async Task<EmployeeAllergyEntity> AddAsync(EmployeeAllergyEntity entity, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        entity.CreatedAt = now;
        entity.UpdatedAt = now;
        db.EmployeeAllergies.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task UpdateAsync(EmployeeAllergyEntity entity, CancellationToken ct = default)
    {
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        db.EmployeeAllergies.Update(entity);
        await db.SaveChangesAsync(ct);
    }

    public Task<bool> DeleteAsync(Guid id, Guid employeeId, string tenantId, string orgId, CancellationToken ct = default) =>
        EmployeeSubResourceRepositoryHelpers.DeleteChildAsync(
            db, db.EmployeeAllergies, id, employeeId, tenantId, orgId, ct);
}

public sealed class EmployeeMedicationRepository(ZelosHrDbContext db) : IEmployeeMedicationRepository
{
    public Task<bool> EmployeeExistsAsync(Guid employeeId, string tenantId, string orgId, CancellationToken ct = default) =>
        EmployeeSubResourceRepositoryHelpers.EmployeeExistsAsync(db, employeeId, tenantId, orgId, ct);

    public async Task<IReadOnlyList<EmployeeMedicationEntity>> ListByEmployeeAsync(
        Guid employeeId, string tenantId, string orgId, CancellationToken ct = default) =>
        await db.EmployeeMedications.AsNoTracking()
            .Where(x => x.EmployeeId == employeeId)
            .OrderBy(x => x.Name)
            .ToListAsync(ct);

    public Task<EmployeeMedicationEntity?> GetByIdAsync(
        Guid id, Guid employeeId, string tenantId, string orgId, CancellationToken ct = default) =>
        db.EmployeeMedications.FirstOrDefaultAsync(x => x.Id == id && x.EmployeeId == employeeId, ct);

    public async Task<EmployeeMedicationEntity> AddAsync(EmployeeMedicationEntity entity, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        entity.CreatedAt = now;
        entity.UpdatedAt = now;
        db.EmployeeMedications.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task UpdateAsync(EmployeeMedicationEntity entity, CancellationToken ct = default)
    {
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        db.EmployeeMedications.Update(entity);
        await db.SaveChangesAsync(ct);
    }

    public Task<bool> DeleteAsync(Guid id, Guid employeeId, string tenantId, string orgId, CancellationToken ct = default) =>
        EmployeeSubResourceRepositoryHelpers.DeleteChildAsync(
            db, db.EmployeeMedications, id, employeeId, tenantId, orgId, ct);
}

public sealed class EmployeeSkillRepository(ZelosHrDbContext db) : IEmployeeSkillRepository
{
    public Task<bool> EmployeeExistsAsync(Guid employeeId, string tenantId, string orgId, CancellationToken ct = default) =>
        EmployeeSubResourceRepositoryHelpers.EmployeeExistsAsync(db, employeeId, tenantId, orgId, ct);

    public async Task<IReadOnlyList<EmployeeSkillEntity>> ListByEmployeeAsync(
        Guid employeeId, string tenantId, string orgId, CancellationToken ct = default) =>
        await db.EmployeeSkills.AsNoTracking()
            .Where(x => x.EmployeeId == employeeId)
            .OrderBy(x => x.Name)
            .ToListAsync(ct);

    public Task<EmployeeSkillEntity?> GetByIdAsync(
        Guid id, Guid employeeId, string tenantId, string orgId, CancellationToken ct = default) =>
        db.EmployeeSkills.FirstOrDefaultAsync(x => x.Id == id && x.EmployeeId == employeeId, ct);

    public async Task<EmployeeSkillEntity> AddAsync(EmployeeSkillEntity entity, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        entity.CreatedAt = now;
        entity.UpdatedAt = now;
        db.EmployeeSkills.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task UpdateAsync(EmployeeSkillEntity entity, CancellationToken ct = default)
    {
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        db.EmployeeSkills.Update(entity);
        await db.SaveChangesAsync(ct);
    }

    public Task<bool> DeleteAsync(Guid id, Guid employeeId, string tenantId, string orgId, CancellationToken ct = default) =>
        EmployeeSubResourceRepositoryHelpers.DeleteChildAsync(
            db, db.EmployeeSkills, id, employeeId, tenantId, orgId, ct);
}

public sealed class EmployeeExperienceRepository(ZelosHrDbContext db) : IEmployeeExperienceRepository
{
    public Task<bool> EmployeeExistsAsync(Guid employeeId, string tenantId, string orgId, CancellationToken ct = default) =>
        EmployeeSubResourceRepositoryHelpers.EmployeeExistsAsync(db, employeeId, tenantId, orgId, ct);

    public async Task<IReadOnlyList<EmployeeExperienceEntity>> ListByEmployeeAsync(
        Guid employeeId, string tenantId, string orgId, CancellationToken ct = default) =>
        await db.EmployeeExperiences.AsNoTracking()
            .Where(x => x.EmployeeId == employeeId)
            .OrderByDescending(x => x.StartDate)
            .ToListAsync(ct);

    public Task<EmployeeExperienceEntity?> GetByIdAsync(
        Guid id, Guid employeeId, string tenantId, string orgId, CancellationToken ct = default) =>
        db.EmployeeExperiences.FirstOrDefaultAsync(x => x.Id == id && x.EmployeeId == employeeId, ct);

    public async Task<EmployeeExperienceEntity> AddAsync(EmployeeExperienceEntity entity, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        entity.CreatedAt = now;
        entity.UpdatedAt = now;
        db.EmployeeExperiences.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task UpdateAsync(EmployeeExperienceEntity entity, CancellationToken ct = default)
    {
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        db.EmployeeExperiences.Update(entity);
        await db.SaveChangesAsync(ct);
    }

    public Task<bool> DeleteAsync(Guid id, Guid employeeId, string tenantId, string orgId, CancellationToken ct = default) =>
        EmployeeSubResourceRepositoryHelpers.DeleteChildAsync(
            db, db.EmployeeExperiences, id, employeeId, tenantId, orgId, ct);
}

public sealed class EmployeeReferralRepository(ZelosHrDbContext db) : IEmployeeReferralRepository
{
    public Task<bool> EmployeeExistsAsync(Guid employeeId, string tenantId, string orgId, CancellationToken ct = default) =>
        EmployeeSubResourceRepositoryHelpers.EmployeeExistsAsync(db, employeeId, tenantId, orgId, ct);

    public async Task<IReadOnlyList<EmployeeReferralEntity>> ListByEmployeeAsync(
        Guid employeeId, string tenantId, string orgId, CancellationToken ct = default) =>
        await db.EmployeeReferrals.AsNoTracking()
            .Where(x => x.EmployeeId == employeeId)
            .OrderBy(x => x.Name)
            .ToListAsync(ct);

    public Task<EmployeeReferralEntity?> GetByIdAsync(
        Guid id, Guid employeeId, string tenantId, string orgId, CancellationToken ct = default) =>
        db.EmployeeReferrals.FirstOrDefaultAsync(x => x.Id == id && x.EmployeeId == employeeId, ct);

    public async Task<EmployeeReferralEntity> AddAsync(EmployeeReferralEntity entity, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        entity.CreatedAt = now;
        entity.UpdatedAt = now;
        db.EmployeeReferrals.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task UpdateAsync(EmployeeReferralEntity entity, CancellationToken ct = default)
    {
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        db.EmployeeReferrals.Update(entity);
        await db.SaveChangesAsync(ct);
    }

    public Task<bool> DeleteAsync(Guid id, Guid employeeId, string tenantId, string orgId, CancellationToken ct = default) =>
        EmployeeSubResourceRepositoryHelpers.DeleteChildAsync(
            db, db.EmployeeReferrals, id, employeeId, tenantId, orgId, ct);
}
