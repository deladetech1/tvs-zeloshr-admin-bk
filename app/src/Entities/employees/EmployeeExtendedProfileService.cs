using System.Globalization;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Persistence.Entities;
using ZelosHR.Api.Shared.Abstractions;

namespace ZelosHR.Api.Entities.Employees;

internal static class EmployeeWorkingHoursConverter
{
    internal static string? ToStorage(decimal? hours) =>
        hours?.ToString(CultureInfo.InvariantCulture);

    internal static decimal? FromStorage(string? value) =>
        decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
}

public sealed class EmployeeExtendedProfileService
{
    private readonly IEmployeeEmergencyContactRepository _emergency;
    private readonly IEmployeePaymentMethodRepository _payment;
    private readonly IEmployeeMedicalProfileRepository _medicalProfiles;
    private readonly IEmployeeMedicalConditionRepository _medicalConditions;
    private readonly IEmployeeAllergyRepository _allergies;
    private readonly IEmployeeMedicationRepository _medications;
    private readonly IEmployeeSkillRepository _skills;
    private readonly IEmployeeExperienceRepository _experiences;
    private readonly IEmployeeReferralRepository _referrals;
    private readonly ITenantContext _tenant;

    public EmployeeExtendedProfileService(
        IEmployeeEmergencyContactRepository emergency,
        IEmployeePaymentMethodRepository payment,
        IEmployeeMedicalProfileRepository medicalProfiles,
        IEmployeeMedicalConditionRepository medicalConditions,
        IEmployeeAllergyRepository allergies,
        IEmployeeMedicationRepository medications,
        IEmployeeSkillRepository skills,
        IEmployeeExperienceRepository experiences,
        IEmployeeReferralRepository referrals,
        ITenantContext tenant)
    {
        _emergency = emergency;
        _payment = payment;
        _medicalProfiles = medicalProfiles;
        _medicalConditions = medicalConditions;
        _allergies = allergies;
        _medications = medications;
        _skills = skills;
        _experiences = experiences;
        _referrals = referrals;
        _tenant = tenant;
    }

    // Emergency contacts

    public async Task<Respons<IReadOnlyList<EmployeeEmergencyContactDto>>> ListEmergencyAsync(
        Guid employeeId, CancellationToken ct = default)
    {
        if (!await _emergency.EmployeeExistsAsync(employeeId, _tenant.TenantId, _tenant.OrgId, ct))
            return FailList<EmployeeEmergencyContactDto>();

        var rows = await _emergency.ListByEmployeeAsync(employeeId, _tenant.TenantId, _tenant.OrgId, ct);
        return Respons<IReadOnlyList<EmployeeEmergencyContactDto>>.Ok(rows.Select(ToEmergencyDto).ToList());
    }

    public async Task<Respons<EmployeeEmergencyContactDto>> AddEmergencyAsync(
        Guid employeeId, EmployeeEmergencyContactWriteDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.EmergencyContactName))
            return Validation<EmployeeEmergencyContactDto>("emergency_contact_name", "emergency_contact_name is required.");

        if (!await _emergency.EmployeeExistsAsync(employeeId, _tenant.TenantId, _tenant.OrgId, ct))
            return NotFound<EmployeeEmergencyContactDto>();

        var entity = await _emergency.AddAsync(new EmployeeEmergencyContactEntity
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            EmergencyContactName = dto.EmergencyContactName.Trim(),
            EmergencyContactPhone = dto.EmergencyContactPhone,
            Relationship = dto.Relationship,
        }, ct);

        return Respons<EmployeeEmergencyContactDto>.Ok(ToEmergencyDto(entity));
    }

    public async Task<Respons<EmployeeEmergencyContactDto>> UpdateEmergencyAsync(
        Guid employeeId, Guid rowId, EmployeeEmergencyContactWriteDto dto, CancellationToken ct = default)
    {
        var existing = await _emergency.GetByIdAsync(rowId, employeeId, _tenant.TenantId, _tenant.OrgId, ct);
        if (existing is null)
            return NotFound<EmployeeEmergencyContactDto>();

        existing.EmergencyContactName = dto.EmergencyContactName.Trim();
        existing.EmergencyContactPhone = dto.EmergencyContactPhone;
        existing.Relationship = dto.Relationship;
        await _emergency.UpdateAsync(existing, ct);
        return Respons<EmployeeEmergencyContactDto>.Ok(ToEmergencyDto(existing));
    }

    public async Task<Respons<object>> DeleteEmergencyAsync(
        Guid employeeId, Guid rowId, CancellationToken ct = default)
    {
        if (!await _emergency.DeleteAsync(rowId, employeeId, _tenant.TenantId, _tenant.OrgId, ct))
            return Respons<object>.Fail("Emergency contact not found.", statusCode: 404);
        return Respons<object>.Ok(new { id = rowId });
    }

    // Payment methods

    public async Task<Respons<IReadOnlyList<EmployeePaymentMethodDto>>> ListPaymentAsync(
        Guid employeeId, CancellationToken ct = default)
    {
        if (!await _payment.EmployeeExistsAsync(employeeId, _tenant.TenantId, _tenant.OrgId, ct))
            return FailList<EmployeePaymentMethodDto>();

        var rows = await _payment.ListByEmployeeAsync(employeeId, _tenant.TenantId, _tenant.OrgId, ct);
        return Respons<IReadOnlyList<EmployeePaymentMethodDto>>.Ok(rows.Select(ToPaymentDto).ToList());
    }

    public async Task<Respons<EmployeePaymentMethodDto>> AddPaymentAsync(
        Guid employeeId, EmployeePaymentMethodWriteDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.PaymentMode))
            return Validation<EmployeePaymentMethodDto>("payment_mode", "payment_mode is required.");

        if (!await _payment.EmployeeExistsAsync(employeeId, _tenant.TenantId, _tenant.OrgId, ct))
            return NotFound<EmployeePaymentMethodDto>();

        var entity = await _payment.AddAsync(new EmployeePaymentMethodEntity
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            PaymentMode = dto.PaymentMode.Trim(),
            BankName = dto.BankName,
            AccountName = dto.AccountName,
            AccountNumber = dto.AccountNumber,
            BranchName = dto.BranchName,
            IsPrimary = dto.IsPrimary,
        }, ct);

        return Respons<EmployeePaymentMethodDto>.Ok(ToPaymentDto(entity));
    }

    public async Task<Respons<EmployeePaymentMethodDto>> UpdatePaymentAsync(
        Guid employeeId, Guid rowId, EmployeePaymentMethodWriteDto dto, CancellationToken ct = default)
    {
        var existing = await _payment.GetByIdAsync(rowId, employeeId, _tenant.TenantId, _tenant.OrgId, ct);
        if (existing is null)
            return NotFound<EmployeePaymentMethodDto>();

        existing.PaymentMode = dto.PaymentMode.Trim();
        existing.BankName = dto.BankName;
        existing.AccountName = dto.AccountName;
        existing.AccountNumber = dto.AccountNumber;
        existing.BranchName = dto.BranchName;
        existing.IsPrimary = dto.IsPrimary;
        await _payment.UpdateAsync(existing, ct);
        return Respons<EmployeePaymentMethodDto>.Ok(ToPaymentDto(existing));
    }

    public async Task<Respons<object>> DeletePaymentAsync(
        Guid employeeId, Guid rowId, CancellationToken ct = default)
    {
        if (!await _payment.DeleteAsync(rowId, employeeId, _tenant.TenantId, _tenant.OrgId, ct))
            return Respons<object>.Fail("Payment method not found.", statusCode: 404);
        return Respons<object>.Ok(new { id = rowId });
    }

    // Medical

    public async Task<Respons<EmployeeMedicalReadDto>> GetMedicalAsync(
        Guid employeeId, CancellationToken ct = default)
    {
        if (!await _medicalProfiles.EmployeeExistsAsync(employeeId, _tenant.TenantId, _tenant.OrgId, ct))
            return Respons<EmployeeMedicalReadDto>.Fail("Employee not found.", statusCode: 404);

        var profile = await _medicalProfiles.GetByEmployeeAsync(employeeId, _tenant.TenantId, _tenant.OrgId, ct);
        var conditions = await _medicalConditions.ListByEmployeeAsync(employeeId, _tenant.TenantId, _tenant.OrgId, ct);
        var allergies = await _allergies.ListByEmployeeAsync(employeeId, _tenant.TenantId, _tenant.OrgId, ct);
        var medications = await _medications.ListByEmployeeAsync(employeeId, _tenant.TenantId, _tenant.OrgId, ct);

        if (profile is null && conditions.Count == 0 && allergies.Count == 0 && medications.Count == 0)
            return Respons<EmployeeMedicalReadDto>.Ok(new EmployeeMedicalReadDto());

        return Respons<EmployeeMedicalReadDto>.Ok(new EmployeeMedicalReadDto
        {
            Id = profile?.Id,
            BloodGroup = profile?.BloodGroup,
            HasMedicalCondition = profile?.HasMedicalCondition ?? false,
            TakesRegularMedication = profile?.TakesRegularMedication ?? false,
            DisabilityStatus = profile?.DisabilityStatus,
            RequiresAccommodation = profile?.RequiresAccommodation ?? false,
            AccommodationDetails = profile?.AccommodationDetails,
            EmergencyMedicalNotes = profile?.EmergencyMedicalNotes,
            CreatedAt = profile?.CreatedAt,
            UpdatedAt = profile?.UpdatedAt,
            MedicalConditions = conditions.Count > 0 ? conditions.Select(ToMedicalConditionDto).ToList() : null,
            Allergies = allergies.Count > 0 ? allergies.Select(ToAllergyDto).ToList() : null,
            Medications = medications.Count > 0 ? medications.Select(ToMedicationDto).ToList() : null,
        });
    }

    public async Task<Respons<EmployeeMedicalProfileDto>> UpsertMedicalScalarsAsync(
        Guid employeeId, EmployeeMedicalWriteDto dto, CancellationToken ct = default) =>
        await UpsertMedicalProfileAsync(employeeId, ToMedicalProfileWrite(dto), ct);

    internal static EmployeeMedicalProfileWriteDto ToMedicalProfileWrite(EmployeeMedicalWriteDto dto) => new()
    {
        BloodGroup = dto.BloodGroup,
        HasMedicalCondition = dto.HasMedicalCondition,
        TakesRegularMedication = dto.TakesRegularMedication,
        DisabilityStatus = dto.DisabilityStatus,
        RequiresAccommodation = dto.RequiresAccommodation,
        AccommodationDetails = dto.AccommodationDetails,
        EmergencyMedicalNotes = dto.EmergencyMedicalNotes,
    };

    public async Task<Respons<EmployeeMedicalProfileDto>> UpsertMedicalProfileAsync(
        Guid employeeId, EmployeeMedicalProfileWriteDto dto, CancellationToken ct = default)
    {
        if (!await _medicalProfiles.EmployeeExistsAsync(employeeId, _tenant.TenantId, _tenant.OrgId, ct))
            return NotFound<EmployeeMedicalProfileDto>();

        var existing = await _medicalProfiles.GetByEmployeeAsync(employeeId, _tenant.TenantId, _tenant.OrgId, ct);
        if (existing is null)
        {
            var created = await _medicalProfiles.AddAsync(new EmployeeMedicalProfileEntity
            {
                Id = Guid.NewGuid(),
                EmployeeId = employeeId,
                BloodGroup = dto.BloodGroup,
                HasMedicalCondition = dto.HasMedicalCondition,
                TakesRegularMedication = dto.TakesRegularMedication,
                DisabilityStatus = dto.DisabilityStatus,
                RequiresAccommodation = dto.RequiresAccommodation,
                AccommodationDetails = dto.AccommodationDetails,
                EmergencyMedicalNotes = dto.EmergencyMedicalNotes,
            }, ct);
            return Respons<EmployeeMedicalProfileDto>.Ok(ToMedicalProfileDto(created));
        }

        existing.BloodGroup = dto.BloodGroup;
        existing.HasMedicalCondition = dto.HasMedicalCondition;
        existing.TakesRegularMedication = dto.TakesRegularMedication;
        existing.DisabilityStatus = dto.DisabilityStatus;
        existing.RequiresAccommodation = dto.RequiresAccommodation;
        existing.AccommodationDetails = dto.AccommodationDetails;
        existing.EmergencyMedicalNotes = dto.EmergencyMedicalNotes;
        await _medicalProfiles.UpdateAsync(existing, ct);
        return Respons<EmployeeMedicalProfileDto>.Ok(ToMedicalProfileDto(existing));
    }

    public async Task<Respons<EmployeeMedicalConditionDto>> AddMedicalConditionAsync(
        Guid employeeId, EmployeeMedicalConditionWriteDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Condition))
            return Validation<EmployeeMedicalConditionDto>("condition", "condition is required.");

        if (!await _medicalConditions.EmployeeExistsAsync(employeeId, _tenant.TenantId, _tenant.OrgId, ct))
            return NotFound<EmployeeMedicalConditionDto>();

        var entity = await _medicalConditions.AddAsync(new EmployeeMedicalConditionEntity
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            Condition = dto.Condition.Trim(),
            Severity = dto.Severity,
            Notes = dto.Notes,
            DiagnosedDate = dto.DiagnosedDate,
        }, ct);

        return Respons<EmployeeMedicalConditionDto>.Ok(ToMedicalConditionDto(entity));
    }

    public async Task<Respons<EmployeeMedicalConditionDto>> UpdateMedicalConditionAsync(
        Guid employeeId, Guid rowId, EmployeeMedicalConditionWriteDto dto, CancellationToken ct = default)
    {
        var existing = await _medicalConditions.GetByIdAsync(rowId, employeeId, _tenant.TenantId, _tenant.OrgId, ct);
        if (existing is null)
            return NotFound<EmployeeMedicalConditionDto>();

        existing.Condition = dto.Condition.Trim();
        existing.Severity = dto.Severity;
        existing.Notes = dto.Notes;
        existing.DiagnosedDate = dto.DiagnosedDate;
        await _medicalConditions.UpdateAsync(existing, ct);
        return Respons<EmployeeMedicalConditionDto>.Ok(ToMedicalConditionDto(existing));
    }

    public async Task<Respons<object>> DeleteMedicalConditionAsync(
        Guid employeeId, Guid rowId, CancellationToken ct = default)
    {
        if (!await _medicalConditions.DeleteAsync(rowId, employeeId, _tenant.TenantId, _tenant.OrgId, ct))
            return Respons<object>.Fail("Medical condition not found.", statusCode: 404);
        return Respons<object>.Ok(new { id = rowId });
    }

    public async Task<Respons<EmployeeAllergyDto>> AddAllergyAsync(
        Guid employeeId, EmployeeAllergyWriteDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Allergen))
            return Validation<EmployeeAllergyDto>("allergen", "allergen is required.");

        if (!await _allergies.EmployeeExistsAsync(employeeId, _tenant.TenantId, _tenant.OrgId, ct))
            return NotFound<EmployeeAllergyDto>();

        var entity = await _allergies.AddAsync(new EmployeeAllergyEntity
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            Allergen = dto.Allergen.Trim(),
            Reaction = dto.Reaction,
            Severity = dto.Severity,
        }, ct);

        return Respons<EmployeeAllergyDto>.Ok(ToAllergyDto(entity));
    }

    public async Task<Respons<EmployeeAllergyDto>> UpdateAllergyAsync(
        Guid employeeId, Guid rowId, EmployeeAllergyWriteDto dto, CancellationToken ct = default)
    {
        var existing = await _allergies.GetByIdAsync(rowId, employeeId, _tenant.TenantId, _tenant.OrgId, ct);
        if (existing is null)
            return NotFound<EmployeeAllergyDto>();

        existing.Allergen = dto.Allergen.Trim();
        existing.Reaction = dto.Reaction;
        existing.Severity = dto.Severity;
        await _allergies.UpdateAsync(existing, ct);
        return Respons<EmployeeAllergyDto>.Ok(ToAllergyDto(existing));
    }

    public async Task<Respons<object>> DeleteAllergyAsync(
        Guid employeeId, Guid rowId, CancellationToken ct = default)
    {
        if (!await _allergies.DeleteAsync(rowId, employeeId, _tenant.TenantId, _tenant.OrgId, ct))
            return Respons<object>.Fail("Allergy not found.", statusCode: 404);
        return Respons<object>.Ok(new { id = rowId });
    }

    public async Task<Respons<EmployeeMedicationDto>> AddMedicationAsync(
        Guid employeeId, EmployeeMedicationWriteDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return Validation<EmployeeMedicationDto>("name", "name is required.");

        if (!await _medications.EmployeeExistsAsync(employeeId, _tenant.TenantId, _tenant.OrgId, ct))
            return NotFound<EmployeeMedicationDto>();

        var entity = await _medications.AddAsync(new EmployeeMedicationEntity
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            Name = dto.Name.Trim(),
            Dosage = dto.Dosage,
            Frequency = dto.Frequency,
            Notes = dto.Notes,
        }, ct);

        return Respons<EmployeeMedicationDto>.Ok(ToMedicationDto(entity));
    }

    public async Task<Respons<EmployeeMedicationDto>> UpdateMedicationAsync(
        Guid employeeId, Guid rowId, EmployeeMedicationWriteDto dto, CancellationToken ct = default)
    {
        var existing = await _medications.GetByIdAsync(rowId, employeeId, _tenant.TenantId, _tenant.OrgId, ct);
        if (existing is null)
            return NotFound<EmployeeMedicationDto>();

        existing.Name = dto.Name.Trim();
        existing.Dosage = dto.Dosage;
        existing.Frequency = dto.Frequency;
        existing.Notes = dto.Notes;
        await _medications.UpdateAsync(existing, ct);
        return Respons<EmployeeMedicationDto>.Ok(ToMedicationDto(existing));
    }

    public async Task<Respons<object>> DeleteMedicationAsync(
        Guid employeeId, Guid rowId, CancellationToken ct = default)
    {
        if (!await _medications.DeleteAsync(rowId, employeeId, _tenant.TenantId, _tenant.OrgId, ct))
            return Respons<object>.Fail("Medication not found.", statusCode: 404);
        return Respons<object>.Ok(new { id = rowId });
    }

    // Skills

    public async Task<Respons<IReadOnlyList<EmployeeSkillDto>>> ListSkillsAsync(
        Guid employeeId, CancellationToken ct = default)
    {
        if (!await _skills.EmployeeExistsAsync(employeeId, _tenant.TenantId, _tenant.OrgId, ct))
            return FailList<EmployeeSkillDto>();

        var rows = await _skills.ListByEmployeeAsync(employeeId, _tenant.TenantId, _tenant.OrgId, ct);
        return Respons<IReadOnlyList<EmployeeSkillDto>>.Ok(rows.Select(ToSkillDto).ToList());
    }

    public async Task<Respons<EmployeeSkillDto>> AddSkillAsync(
        Guid employeeId, EmployeeSkillWriteDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return Validation<EmployeeSkillDto>("name", "name is required.");

        if (!await _skills.EmployeeExistsAsync(employeeId, _tenant.TenantId, _tenant.OrgId, ct))
            return NotFound<EmployeeSkillDto>();

        var entity = await _skills.AddAsync(new EmployeeSkillEntity
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            Name = dto.Name.Trim(),
            Proficiency = dto.Proficiency,
            YearsOfExperience = dto.YearsOfExperience,
        }, ct);

        return Respons<EmployeeSkillDto>.Ok(ToSkillDto(entity));
    }

    public async Task<Respons<EmployeeSkillDto>> UpdateSkillAsync(
        Guid employeeId, Guid rowId, EmployeeSkillWriteDto dto, CancellationToken ct = default)
    {
        var existing = await _skills.GetByIdAsync(rowId, employeeId, _tenant.TenantId, _tenant.OrgId, ct);
        if (existing is null)
            return NotFound<EmployeeSkillDto>();

        existing.Name = dto.Name.Trim();
        existing.Proficiency = dto.Proficiency;
        existing.YearsOfExperience = dto.YearsOfExperience;
        await _skills.UpdateAsync(existing, ct);
        return Respons<EmployeeSkillDto>.Ok(ToSkillDto(existing));
    }

    public async Task<Respons<object>> DeleteSkillAsync(
        Guid employeeId, Guid rowId, CancellationToken ct = default)
    {
        if (!await _skills.DeleteAsync(rowId, employeeId, _tenant.TenantId, _tenant.OrgId, ct))
            return Respons<object>.Fail("Skill not found.", statusCode: 404);
        return Respons<object>.Ok(new { id = rowId });
    }

    // Experiences

    public async Task<Respons<IReadOnlyList<EmployeeExperienceDto>>> ListExperiencesAsync(
        Guid employeeId, CancellationToken ct = default)
    {
        if (!await _experiences.EmployeeExistsAsync(employeeId, _tenant.TenantId, _tenant.OrgId, ct))
            return FailList<EmployeeExperienceDto>();

        var rows = await _experiences.ListByEmployeeAsync(employeeId, _tenant.TenantId, _tenant.OrgId, ct);
        return Respons<IReadOnlyList<EmployeeExperienceDto>>.Ok(rows.Select(ToExperienceDto).ToList());
    }

    public async Task<Respons<EmployeeExperienceDto>> AddExperienceAsync(
        Guid employeeId, EmployeeExperienceWriteDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Company))
            return Validation<EmployeeExperienceDto>("company", "company is required.");

        if (!await _experiences.EmployeeExistsAsync(employeeId, _tenant.TenantId, _tenant.OrgId, ct))
            return NotFound<EmployeeExperienceDto>();

        var entity = await _experiences.AddAsync(new EmployeeExperienceEntity
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            Company = dto.Company.Trim(),
            JobTitle = dto.JobTitle,
            EmploymentType = dto.EmploymentType,
            Location = dto.Location,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            IsCurrent = dto.IsCurrent,
            Description = dto.Description,
        }, ct);

        return Respons<EmployeeExperienceDto>.Ok(ToExperienceDto(entity));
    }

    public async Task<Respons<EmployeeExperienceDto>> UpdateExperienceAsync(
        Guid employeeId, Guid rowId, EmployeeExperienceWriteDto dto, CancellationToken ct = default)
    {
        var existing = await _experiences.GetByIdAsync(rowId, employeeId, _tenant.TenantId, _tenant.OrgId, ct);
        if (existing is null)
            return NotFound<EmployeeExperienceDto>();

        existing.Company = dto.Company.Trim();
        existing.JobTitle = dto.JobTitle;
        existing.EmploymentType = dto.EmploymentType;
        existing.Location = dto.Location;
        existing.StartDate = dto.StartDate;
        existing.EndDate = dto.EndDate;
        existing.IsCurrent = dto.IsCurrent;
        existing.Description = dto.Description;
        await _experiences.UpdateAsync(existing, ct);
        return Respons<EmployeeExperienceDto>.Ok(ToExperienceDto(existing));
    }

    public async Task<Respons<object>> DeleteExperienceAsync(
        Guid employeeId, Guid rowId, CancellationToken ct = default)
    {
        if (!await _experiences.DeleteAsync(rowId, employeeId, _tenant.TenantId, _tenant.OrgId, ct))
            return Respons<object>.Fail("Experience not found.", statusCode: 404);
        return Respons<object>.Ok(new { id = rowId });
    }

    // Referrals

    public async Task<Respons<IReadOnlyList<EmployeeReferralDto>>> ListReferralsAsync(
        Guid employeeId, CancellationToken ct = default)
    {
        if (!await _referrals.EmployeeExistsAsync(employeeId, _tenant.TenantId, _tenant.OrgId, ct))
            return FailList<EmployeeReferralDto>();

        var rows = await _referrals.ListByEmployeeAsync(employeeId, _tenant.TenantId, _tenant.OrgId, ct);
        return Respons<IReadOnlyList<EmployeeReferralDto>>.Ok(rows.Select(ToReferralDto).ToList());
    }

    public async Task<Respons<EmployeeReferralDto>> AddReferralAsync(
        Guid employeeId, EmployeeReferralWriteDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return Validation<EmployeeReferralDto>("name", "name is required.");

        if (!await _referrals.EmployeeExistsAsync(employeeId, _tenant.TenantId, _tenant.OrgId, ct))
            return NotFound<EmployeeReferralDto>();

        var entity = await _referrals.AddAsync(new EmployeeReferralEntity
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            Name = dto.Name.Trim(),
            JobTitle = dto.JobTitle,
            Company = dto.Company,
            Relationship = dto.Relationship,
            Email = dto.Email,
            Phone = dto.Phone,
        }, ct);

        return Respons<EmployeeReferralDto>.Ok(ToReferralDto(entity));
    }

    public async Task<Respons<EmployeeReferralDto>> UpdateReferralAsync(
        Guid employeeId, Guid rowId, EmployeeReferralWriteDto dto, CancellationToken ct = default)
    {
        var existing = await _referrals.GetByIdAsync(rowId, employeeId, _tenant.TenantId, _tenant.OrgId, ct);
        if (existing is null)
            return NotFound<EmployeeReferralDto>();

        existing.Name = dto.Name.Trim();
        existing.JobTitle = dto.JobTitle;
        existing.Company = dto.Company;
        existing.Relationship = dto.Relationship;
        existing.Email = dto.Email;
        existing.Phone = dto.Phone;
        await _referrals.UpdateAsync(existing, ct);
        return Respons<EmployeeReferralDto>.Ok(ToReferralDto(existing));
    }

    public async Task<Respons<object>> DeleteReferralAsync(
        Guid employeeId, Guid rowId, CancellationToken ct = default)
    {
        if (!await _referrals.DeleteAsync(rowId, employeeId, _tenant.TenantId, _tenant.OrgId, ct))
            return Respons<object>.Fail("Referral not found.", statusCode: 404);
        return Respons<object>.Ok(new { id = rowId });
    }

    internal static EmployeeEmergencyContactWriteDto ToEmergencyWrite(EmployeeEmergencyContactUpsertDto dto) =>
        new(dto.EmergencyContactName, dto.EmergencyContactPhone, dto.Relationship);

    internal static EmployeePaymentMethodWriteDto ToPaymentWrite(EmployeePaymentMethodUpsertDto dto) =>
        new(dto.PaymentMode, dto.BankName, dto.AccountName, dto.AccountNumber, dto.BranchName, dto.IsPrimary);

    internal static EmployeeMedicalConditionWriteDto ToMedicalConditionWrite(EmployeeMedicalConditionUpsertDto dto) =>
        new(dto.Condition, dto.Severity, dto.Notes, dto.DiagnosedDate);

    internal static EmployeeAllergyWriteDto ToAllergyWrite(EmployeeAllergyUpsertDto dto) =>
        new(dto.Allergen, dto.Reaction, dto.Severity);

    internal static EmployeeMedicationWriteDto ToMedicationWrite(EmployeeMedicationUpsertDto dto) =>
        new(dto.Name, dto.Dosage, dto.Frequency, dto.Notes);

    internal static EmployeeSkillWriteDto ToSkillWrite(EmployeeSkillUpsertDto dto) =>
        new(dto.Name, dto.Proficiency, dto.YearsOfExperience);

    internal static EmployeeExperienceWriteDto ToExperienceWrite(EmployeeExperienceUpsertDto dto) =>
        new(dto.Company, dto.JobTitle, dto.EmploymentType, dto.Location, dto.StartDate, dto.EndDate, dto.IsCurrent, dto.Description);

    internal static EmployeeReferralWriteDto ToReferralWrite(EmployeeReferralUpsertDto dto) =>
        new(dto.Name, dto.JobTitle, dto.Company, dto.Relationship, dto.Email, dto.Phone);

    private static EmployeeEmergencyContactDto ToEmergencyDto(EmployeeEmergencyContactEntity e) => new(
        e.Id, e.EmergencyContactName, e.EmergencyContactPhone, e.Relationship, e.CreatedAt, e.UpdatedAt);

    private static EmployeePaymentMethodDto ToPaymentDto(EmployeePaymentMethodEntity e) => new(
        e.Id, e.PaymentMode, e.BankName, e.AccountName, e.AccountNumber, e.BranchName, e.IsPrimary, e.CreatedAt, e.UpdatedAt);

    private static EmployeeMedicalProfileDto ToMedicalProfileDto(EmployeeMedicalProfileEntity e) => new()
    {
        Id = e.Id,
        BloodGroup = e.BloodGroup,
        HasMedicalCondition = e.HasMedicalCondition,
        TakesRegularMedication = e.TakesRegularMedication,
        DisabilityStatus = e.DisabilityStatus,
        RequiresAccommodation = e.RequiresAccommodation,
        AccommodationDetails = e.AccommodationDetails,
        EmergencyMedicalNotes = e.EmergencyMedicalNotes,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt,
    };

    private static EmployeeMedicalConditionDto ToMedicalConditionDto(EmployeeMedicalConditionEntity e) => new(
        e.Id, e.Condition, e.Severity, e.Notes, e.DiagnosedDate, e.CreatedAt, e.UpdatedAt);

    private static EmployeeAllergyDto ToAllergyDto(EmployeeAllergyEntity e) => new(
        e.Id, e.Allergen, e.Reaction, e.Severity, e.CreatedAt, e.UpdatedAt);

    private static EmployeeMedicationDto ToMedicationDto(EmployeeMedicationEntity e) => new(
        e.Id, e.Name, e.Dosage, e.Frequency, e.Notes, e.CreatedAt, e.UpdatedAt);

    private static EmployeeSkillDto ToSkillDto(EmployeeSkillEntity e) => new(
        e.Id, e.Name, e.Proficiency, e.YearsOfExperience, e.CreatedAt, e.UpdatedAt);

    private static EmployeeExperienceDto ToExperienceDto(EmployeeExperienceEntity e) => new(
        e.Id, e.Company, e.JobTitle, e.EmploymentType, e.Location, e.StartDate, e.EndDate, e.IsCurrent, e.Description, e.CreatedAt, e.UpdatedAt);

    private static EmployeeReferralDto ToReferralDto(EmployeeReferralEntity e) => new(
        e.Id, e.Name, e.JobTitle, e.Company, e.Relationship, e.Email, e.Phone, e.CreatedAt, e.UpdatedAt);

    private static Respons<IReadOnlyList<T>> FailList<T>() =>
        Respons<IReadOnlyList<T>>.Fail("Employee not found.", statusCode: 404);

    private static Respons<T> NotFound<T>() =>
        Respons<T>.Fail("Employee not found.", statusCode: 404);

    private static Respons<T> Validation<T>(string key, string message) =>
        Respons<T>.ValidationError(new Dictionary<string, string> { [key] = message });
}
