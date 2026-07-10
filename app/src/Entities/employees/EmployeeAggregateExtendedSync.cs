using ZelosHR.Api.Entities.Shared;

namespace ZelosHR.Api.Entities.Employees;

/// <summary>Create-path helpers for extended profile array sections.</summary>
internal static class EmployeeAggregateExtendedSync
{
    internal static async Task<Respons<EmployeeAggregateReadDto>?> ApplyEmergencyCreateAsync(
        EmployeeExtendedProfileService extended,
        Guid employeeId,
        IReadOnlyList<EmployeeEmergencyContactUpsertDto> items,
        CancellationToken ct)
    {
        for (var i = 0; i < items.Count; i++)
        {
            var added = await extended.AddEmergencyAsync(
                employeeId, EmployeeExtendedProfileService.ToEmergencyWrite(items[i]), ct);
            if (!added.Success)
                return MapArrayError<EmployeeAggregateReadDto>(added, $"identity.emergency[{i}]");
        }

        return null;
    }

    internal static async Task<Respons<EmployeeAggregateReadDto>?> ApplyPaymentCreateAsync(
        EmployeeExtendedProfileService extended,
        Guid employeeId,
        IReadOnlyList<EmployeePaymentMethodUpsertDto> items,
        CancellationToken ct)
    {
        for (var i = 0; i < items.Count; i++)
        {
            var added = await extended.AddPaymentAsync(
                employeeId, EmployeeExtendedProfileService.ToPaymentWrite(items[i]), ct);
            if (!added.Success)
                return MapArrayError<EmployeeAggregateReadDto>(added, $"compensation.payment[{i}]");
        }

        return null;
    }

    internal static async Task<Respons<EmployeeAggregateReadDto>?> ApplyMedicalCreateAsync(
        EmployeeExtendedProfileService extended,
        Guid employeeId,
        EmployeeMedicalWriteDto medical,
        CancellationToken ct)
    {
        if (HasMedicalScalars(medical))
        {
            var profile = await extended.UpsertMedicalScalarsAsync(employeeId, medical, ct);
            if (!profile.Success)
                return MapArrayError<EmployeeAggregateReadDto>(profile, "medical");
        }

        if (medical.MedicalConditions is not null)
        {
            for (var i = 0; i < medical.MedicalConditions.Count; i++)
            {
                var added = await extended.AddMedicalConditionAsync(
                    employeeId, EmployeeExtendedProfileService.ToMedicalConditionWrite(medical.MedicalConditions[i]), ct);
                if (!added.Success)
                    return MapArrayError<EmployeeAggregateReadDto>(added, $"medical.medical_conditions[{i}]");
            }
        }

        if (medical.Allergies is not null)
        {
            for (var i = 0; i < medical.Allergies.Count; i++)
            {
                var added = await extended.AddAllergyAsync(
                    employeeId, EmployeeExtendedProfileService.ToAllergyWrite(medical.Allergies[i]), ct);
                if (!added.Success)
                    return MapArrayError<EmployeeAggregateReadDto>(added, $"medical.allergies[{i}]");
            }
        }

        if (medical.Medications is not null)
        {
            for (var i = 0; i < medical.Medications.Count; i++)
            {
                var added = await extended.AddMedicationAsync(
                    employeeId, EmployeeExtendedProfileService.ToMedicationWrite(medical.Medications[i]), ct);
                if (!added.Success)
                    return MapArrayError<EmployeeAggregateReadDto>(added, $"medical.medications[{i}]");
            }
        }

        return null;
    }

    internal static bool HasMedicalScalars(EmployeeMedicalWriteDto medical) =>
        medical.BloodGroup is not null
        || medical.HasMedicalCondition
        || medical.TakesRegularMedication
        || medical.DisabilityStatus is not null
        || medical.RequiresAccommodation
        || medical.AccommodationDetails is not null
        || medical.EmergencyMedicalNotes is not null;

    internal static async Task<Respons<EmployeeAggregateReadDto>?> ApplySkillsCreateAsync(
        EmployeeExtendedProfileService extended,
        Guid employeeId,
        IReadOnlyList<EmployeeSkillWriteDto> items,
        CancellationToken ct)
    {
        for (var i = 0; i < items.Count; i++)
        {
            var added = await extended.AddSkillAsync(employeeId, items[i], ct);
            if (!added.Success)
                return MapArrayError<EmployeeAggregateReadDto>(added, $"skills[{i}]");
        }

        return null;
    }

    internal static async Task<Respons<EmployeeAggregateReadDto>?> ApplyExperiencesCreateAsync(
        EmployeeExtendedProfileService extended,
        Guid employeeId,
        IReadOnlyList<EmployeeExperienceWriteDto> items,
        CancellationToken ct)
    {
        for (var i = 0; i < items.Count; i++)
        {
            var added = await extended.AddExperienceAsync(employeeId, items[i], ct);
            if (!added.Success)
                return MapArrayError<EmployeeAggregateReadDto>(added, $"experiences[{i}]");
        }

        return null;
    }

    internal static async Task<Respons<EmployeeAggregateReadDto>?> ApplyReferralsCreateAsync(
        EmployeeExtendedProfileService extended,
        Guid employeeId,
        IReadOnlyList<EmployeeReferralWriteDto> items,
        CancellationToken ct)
    {
        for (var i = 0; i < items.Count; i++)
        {
            var added = await extended.AddReferralAsync(employeeId, items[i], ct);
            if (!added.Success)
                return MapArrayError<EmployeeAggregateReadDto>(added, $"referrals[{i}]");
        }

        return null;
    }

    private static Respons<T> MapArrayError<T, TItem>(Respons<TItem> source, string prefix)
    {
        if (source.FieldErrors is { Count: > 0 })
        {
            var remapped = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var (key, message) in source.FieldErrors)
                remapped[$"{prefix}.{key}"] = message;
            return Respons<T>.ValidationError(remapped, source.Error ?? source.Detail);
        }

        return Respons<T>.Fail(source.Error ?? source.Detail ?? "Request failed.", statusCode: source.StatusCode);
    }
}
