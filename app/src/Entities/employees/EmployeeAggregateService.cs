using Microsoft.EntityFrameworkCore;
using ZelosHR.Api.Entities.AuditLogs;
using ZelosHR.Api.Entities.Branches;
using ZelosHR.Api.Entities.CustomFields;
using ZelosHR.Api.Entities.EmploymentTypes;
using ZelosHR.Api.Entities.Departments;
using ZelosHR.Api.Entities.Files;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Persistence;
using ZelosHR.Api.Persistence.Entities;
using ZelosHR.Api.Persistence.Repositories;
using ZelosHR.Api.Shared.Abstractions;
using ZelosHR.Api.Shared.Formatting;
using ZelosHR.Api.Shared.Infrastructure;
using ZelosHR.Api.Shared.Pagination;
using ZelosHR.Api.Shared.Validation;

namespace ZelosHR.Api.Entities.Employees;

public sealed class EmployeeAggregateService
{
    private const int MaxEducation = 20;
    private const int MaxCertifications = 50;
    private const int MaxIdentifications = 10;

    private readonly ZelosHrDbContext _db;
    private readonly EmployeeRegistrationService _registration;
    private readonly EmployeeSubResourcesService _subResources;
    private readonly EmployeeExtendedProfileService _extended;
    private readonly IEmployeeRepository _employees;
    private readonly ICpUserRepository _cpUsers;
    private readonly IDepartmentRepository _departments;
    private readonly IBranchRepository _branches;
    private readonly EmployeesService _employeesService;
    private readonly ICustomFieldDefinitionsRepository _customFieldDefinitions;
    private readonly IHrDocumentPathRepository _hrDocuments;
    private readonly ICpCurrencyRepository _currencies;
    private readonly HrDocumentPresignedUrlService _profileUrls;
    private readonly EmploymentTypesService _employmentTypes;
    private readonly IAuditLogWriter _auditLogs;
    private readonly EmployeeOnboardingInviteService _onboardingInvites;
    private readonly ITenantContext _tenant;
    private readonly ILogger<EmployeeAggregateService> _logger;

    public EmployeeAggregateService(
        ZelosHrDbContext db,
        EmployeeRegistrationService registration,
        EmployeeSubResourcesService subResources,
        EmployeeExtendedProfileService extended,
        IEmployeeRepository employees,
        ICpUserRepository cpUsers,
        IDepartmentRepository departments,
        IBranchRepository branches,
        EmployeesService employeesService,
        ICustomFieldDefinitionsRepository customFieldDefinitions,
        IHrDocumentPathRepository hrDocuments,
        ICpCurrencyRepository currencies,
        HrDocumentPresignedUrlService profileUrls,
        EmploymentTypesService employmentTypes,
        IAuditLogWriter auditLogs,
        EmployeeOnboardingInviteService onboardingInvites,
        ITenantContext tenant,
        ILogger<EmployeeAggregateService> logger)
    {
        _db = db;
        _registration = registration;
        _subResources = subResources;
        _extended = extended;
        _employees = employees;
        _cpUsers = cpUsers;
        _departments = departments;
        _branches = branches;
        _employeesService = employeesService;
        _customFieldDefinitions = customFieldDefinitions;
        _hrDocuments = hrDocuments;
        _currencies = currencies;
        _profileUrls = profileUrls;
        _employmentTypes = employmentTypes;
        _auditLogs = auditLogs;
        _onboardingInvites = onboardingInvites;
        _tenant = tenant;
        _logger = logger;
    }

    public async Task<Respons<EmployeeAggregateReadDto>> CreateAsync(
        CreateEmployeeAggregateRequest request,
        bool? finaliseOverride = null,
        CancellationToken ct = default)
    {
        var validation = EmployeeAggregateCreateValidator.Validate(request);
        if (validation is not null)
            return Respons<EmployeeAggregateReadDto>.ValidationError(validation);

        var isFinalised = finaliseOverride
            ?? (!request.IsDraft && !string.IsNullOrWhiteSpace(request.Identity.WorkEmail));

        var committed = false;
        Guid employeeId = default;

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var draft = await _registration.CreateDraftAsync(
                request.Identity.FullName,
                existingUserId: null,
                employeeCodeCustom: request.Identity.EmployeeCodeCustom,
                ct: ct);
            if (!draft.Success || draft.Data is null)
            {
                await RollbackCreateTransactionAsync(transaction, ct);
                return Respons<EmployeeAggregateReadDto>.Fail(
                    draft.Error ?? draft.Detail ?? "Could not create employee.",
                    statusCode: draft.StatusCode);
            }

            employeeId = draft.Data.Id;
            var wizard = EmployeeAggregateMapper.ToWizardRequest(request);

            var personal = await _registration.UpdatePersonalContactAsync(employeeId, wizard, ct);
            if (!personal.Success)
            {
                await RollbackCreateTransactionAsync(transaction, ct);
                return MapError<EmployeeAggregateReadDto>(personal);
            }

            if (request.Identity.Identifications is { Count: > 0 })
            {
                var identificationError = await ApplyIdentificationsCreateAsync(
                    employeeId, request.Identity.Identifications, ct);
                if (identificationError is not null)
                {
                    await RollbackCreateTransactionAsync(transaction, ct);
                    return identificationError;
                }
            }

            if (request.Employment is not null)
            {
                if (request.Employment.DepartmentId is { } deptId
                    && !await _departments.ExistsActiveScopedAsync(deptId, _tenant.TenantId, _tenant.OrgId, ct))
                {
                    await RollbackCreateTransactionAsync(transaction, ct);
                    return Respons<EmployeeAggregateReadDto>.ValidationError(
                        new Dictionary<string, string> { ["employment.department_id"] = "Department not found." });
                }

                if (request.Employment.BranchId is { } branchId
                    && !await _branches.ExistsActiveScopedAsync(branchId, _tenant.TenantId, _tenant.OrgId, ct))
                {
                    await RollbackCreateTransactionAsync(transaction, ct);
                    return Respons<EmployeeAggregateReadDto>.ValidationError(
                        new Dictionary<string, string> { ["employment.branch_id"] = "Branch not found." });
                }

                var employment = await _registration.UpdateEmploymentDetailsAsync(employeeId, wizard, ct);
                if (!employment.Success)
                {
                    await RollbackCreateTransactionAsync(transaction, ct);
                    return MapError<EmployeeAggregateReadDto>(employment);
                }

                var employmentExtrasError = await ApplyEmploymentExtrasIfNeededAsync(
                    employeeId, request.Employment, ct);
                if (employmentExtrasError is not null)
                {
                    await RollbackCreateTransactionAsync(transaction, ct);
                    return employmentExtrasError;
                }
            }

            if (request.Compensation is not null)
            {
                var compensation = await _registration.UpdateCompensationAsync(employeeId, wizard, ct);
                if (!compensation.Success)
                {
                    await RollbackCreateTransactionAsync(transaction, ct);
                    return MapError<EmployeeAggregateReadDto>(compensation);
                }
            }

            if (HasSectionCustomFields(request))
            {
                var entity = await _employees.GetByIdScopedForUpdateAsync(
                    employeeId, _tenant.TenantId, _tenant.OrgId, ct);
                if (entity is null)
                {
                    await RollbackCreateTransactionAsync(transaction, ct);
                    return Respons<EmployeeAggregateReadDto>.Fail("Employee not found.", statusCode: 404);
                }

                await ApplySectionCustomFieldsAsync(
                    entity,
                    new Dictionary<string, string?>(),
                    request.Identity.CustomFields,
                    request.Employment?.CustomFields,
                    request.Compensation?.CustomFields,
                    request.Education.Select(e => e.CustomFields),
                    request.Certifications.Select(c => c.CustomFields),
                    ct);
            }

            foreach (var edu in request.Education)
            {
                var added = await _subResources.AddEducationAsync(employeeId, edu, ct);
                if (!added.Success)
                {
                    await RollbackCreateTransactionAsync(transaction, ct);
                    return MapError<EmployeeAggregateReadDto>(added);
                }
            }

            foreach (var cert in request.Certifications)
            {
                var added = await _subResources.AddCertificationAsync(employeeId, cert, ct);
                if (!added.Success)
                {
                    await RollbackCreateTransactionAsync(transaction, ct);
                    return MapError<EmployeeAggregateReadDto>(added);
                }
            }

            if (request.Identity.Emergency is { Count: > 0 })
            {
                var emergencyError = await EmployeeAggregateExtendedSync.ApplyEmergencyCreateAsync(
                    _extended, employeeId, request.Identity.Emergency, ct);
                if (emergencyError is not null)
                {
                    await RollbackCreateTransactionAsync(transaction, ct);
                    return emergencyError;
                }
            }

            if (request.Compensation?.Payment is { Count: > 0 })
            {
                var paymentError = await EmployeeAggregateExtendedSync.ApplyPaymentCreateAsync(
                    _extended, employeeId, request.Compensation.Payment, ct);
                if (paymentError is not null)
                {
                    await RollbackCreateTransactionAsync(transaction, ct);
                    return paymentError;
                }
            }

            if (request.Medical is not null)
            {
                var medicalError = await EmployeeAggregateExtendedSync.ApplyMedicalCreateAsync(
                    _extended, employeeId, request.Medical, ct);
                if (medicalError is not null)
                {
                    await RollbackCreateTransactionAsync(transaction, ct);
                    return medicalError;
                }
            }

            if (request.Skills.Count > 0)
            {
                var skillsError = await EmployeeAggregateExtendedSync.ApplySkillsCreateAsync(
                    _extended, employeeId, request.Skills, ct);
                if (skillsError is not null)
                {
                    await RollbackCreateTransactionAsync(transaction, ct);
                    return skillsError;
                }
            }

            if (request.Experiences.Count > 0)
            {
                var experiencesError = await EmployeeAggregateExtendedSync.ApplyExperiencesCreateAsync(
                    _extended, employeeId, request.Experiences, ct);
                if (experiencesError is not null)
                {
                    await RollbackCreateTransactionAsync(transaction, ct);
                    return experiencesError;
                }
            }

            if (request.Referrals.Count > 0)
            {
                var referralsError = await EmployeeAggregateExtendedSync.ApplyReferralsCreateAsync(
                    _extended, employeeId, request.Referrals, ct);
                if (referralsError is not null)
                {
                    await RollbackCreateTransactionAsync(transaction, ct);
                    return referralsError;
                }
            }

            if (request.DocumentIds is { Count: > 0 })
            {
                var entity = await _employees.GetByIdScopedForUpdateAsync(
                    employeeId, _tenant.TenantId, _tenant.OrgId, ct);
                if (entity is null)
                {
                    await RollbackCreateTransactionAsync(transaction, ct);
                    return Respons<EmployeeAggregateReadDto>.Fail("Employee not found.", statusCode: 404);
                }

                var docErrors = await ValidateAndApplyDocumentIdsAsync(entity, request.DocumentIds, ct);
                if (docErrors is not null)
                {
                    await RollbackCreateTransactionAsync(transaction, ct);
                    return Respons<EmployeeAggregateReadDto>.ValidationError(docErrors);
                }
            }

            if (isFinalised)
            {
                var finalised = await _registration.FinaliseAsync(employeeId, ct);
                if (!finalised.Success)
                {
                    await RollbackCreateTransactionAsync(transaction, ct);
                    return MapError<EmployeeAggregateReadDto>(finalised);
                }
            }
            else if (request.IsDraft)
            {
                var draftError = await EnsureDraftFlagAsync(employeeId, ct);
                if (draftError is not null)
                {
                    await RollbackCreateTransactionAsync(transaction, ct);
                    return draftError;
                }
            }

            await transaction.CommitAsync(ct);
            committed = true;
        }
        catch (PlatformUserConflictException ex)
        {
            if (!committed)
                await RollbackCreateTransactionAsync(transaction, ct);
            return Respons<EmployeeAggregateReadDto>.ValidationError(
                new Dictionary<string, string> { [ex.FieldKey] = ex.Message });
        }
        catch (DbUpdateException ex) when (PostgresUniqueViolation.IsCpUserEmail(ex))
        {
            if (!committed)
                await RollbackCreateTransactionAsync(transaction, ct);
            return Respons<EmployeeAggregateReadDto>.ValidationError(
                new Dictionary<string, string>
                {
                    ["identity.work_email"] = EmployeeErrorMessages.WorkEmailAlreadyRegistered,
                });
        }
        catch (DbUpdateException ex) when (PostgresUniqueViolation.IsCpUserContact(ex))
        {
            if (!committed)
                await RollbackCreateTransactionAsync(transaction, ct);
            return Respons<EmployeeAggregateReadDto>.ValidationError(
                new Dictionary<string, string>
                {
                    ["identity.phone"] = EmployeeErrorMessages.PhoneAlreadyRegistered,
                });
        }
        catch (Exception ex) when (PostgresSchemaErrors.ReferencesIdentificationsStorage(ex))
        {
            if (!committed)
                await RollbackCreateTransactionAsync(transaction, ct);
            _logger.LogError(ex, "Employee identifications storage unavailable for tenant {TenantId} org {OrgId}",
                _tenant.TenantId, _tenant.OrgId);
            return IdentificationsStorageUnavailable();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            if (!committed)
                await RollbackCreateTransactionAsync(transaction, ct);
            _logger.LogWarning(ex, "Employee create concurrency conflict for tenant {TenantId} org {OrgId}",
                _tenant.TenantId, _tenant.OrgId);
            return Respons<EmployeeAggregateReadDto>.Fail(
                "Could not save employee record. Please retry.", statusCode: 409);
        }
        catch (Exception ex)
        {
            if (!committed)
                await RollbackCreateTransactionAsync(transaction, ct);
            _logger.LogError(ex, "Employee create failed for tenant {TenantId} org {OrgId}", _tenant.TenantId, _tenant.OrgId);
            throw;
        }

        await TryRecordEmployeeCreateAuditAsync(employeeId, isFinalised, ct);
        if (isFinalised)
            await _onboardingInvites.TrySendAfterFinaliseAsync(employeeId, ct);

        return await GetAsync(employeeId, ct);
    }

    private async Task RollbackCreateTransactionAsync(
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction,
        CancellationToken ct)
    {
        if (_db.Database.CurrentTransaction is null)
            return;

        try
        {
            await transaction.RollbackAsync(ct);
        }
        catch (InvalidOperationException)
        {
            // Transaction already committed or disposed.
        }

        _db.ChangeTracker.Clear();
    }

    public async Task<Respons<EmployeeAggregateReadDto>> UpdateAsync(
        Guid employeeId, UpdateEmployeeAggregateRequest request, CancellationToken ct = default)
    {
        if (!HasAnyUpdate(request))
        {
            return Respons<EmployeeAggregateReadDto>.ValidationError(new Dictionary<string, string>
            {
                ["body"] = "Include at least one field to update (identity, employment, compensation, education, certifications, identifications, documents).",
            });
        }

        var validation = ValidateUpdate(request);
        if (validation is not null)
            return Respons<EmployeeAggregateReadDto>.ValidationError(validation);

        var entityBeforeUpdate = await _employees.GetByIdScopedAsync(employeeId, _tenant.TenantId, _tenant.OrgId, ct);
        if (entityBeforeUpdate is null)
            return Respons<EmployeeAggregateReadDto>.Fail("Employee not found.", statusCode: 404);

        var shouldFinalise = entityBeforeUpdate.IsDraft
            && !request.IsDraft
            && !string.IsNullOrWhiteSpace(request.Identity?.WorkEmail);

        var committed = false;

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var wizard = EmployeeAggregateMapper.ToWizardRequest(request);

            if (request.Identity is not null && HasIdentityPersonalContactUpdate(request.Identity))
            {
                var personal = await _registration.UpdatePersonalContactAsync(employeeId, wizard, ct);
                if (!personal.Success)
                {
                    await transaction.RollbackAsync(ct);
                    return MapError<EmployeeAggregateReadDto>(personal);
                }
            }

            if (request.DeleteIdentificationIds is { Count: > 0 })
            {
                foreach (var identificationId in request.DeleteIdentificationIds)
                {
                    var deleted = await _subResources.DeleteIdentificationAsync(employeeId, identificationId, ct);
                    if (!deleted.Success)
                    {
                        await transaction.RollbackAsync(ct);
                        return Respons<EmployeeAggregateReadDto>.Fail(
                            deleted.Error ?? deleted.Detail ?? "Could not delete identification.",
                            statusCode: deleted.StatusCode);
                    }
                }
            }

            if (request.Identity?.Identifications is not null)
            {
                var identificationDupErrors = request.Identity.Identifications.Count > 0
                    ? EmployeeSubResourceUpsertRules.ValidateDuplicateIds(request.Identity.Identifications)
                    : null;
                if (identificationDupErrors is not null)
                {
                    await transaction.RollbackAsync(ct);
                    return Respons<EmployeeAggregateReadDto>.ValidationError(identificationDupErrors);
                }

                var identificationTypeDupErrors = request.Identity.Identifications.Count > 0
                    ? EmployeeSubResourceUpsertRules.ValidateDuplicateIdCardTypeIds(request.Identity.Identifications)
                    : null;
                if (identificationTypeDupErrors is not null)
                {
                    await transaction.RollbackAsync(ct);
                    return Respons<EmployeeAggregateReadDto>.ValidationError(identificationTypeDupErrors);
                }

                var existingIdentifications = await _subResources.ListIdentificationsAsync(employeeId, ct);
                var existingIdentificationRows = existingIdentifications.Success && existingIdentifications.Data is not null
                    ? existingIdentifications.Data
                    : Array.Empty<EmployeeIdentificationDto>();
                var existingIdentificationIds = existingIdentificationRows.Select(r => r.Id).ToHashSet();
                var preservedIdentificationIds = new HashSet<Guid>();

                for (var i = 0; i < request.Identity.Identifications.Count; i++)
                {
                    var identification = request.Identity.Identifications[i];
                    var write = EmployeeAggregateMapper.ToIdentificationWrite(identification);
                    var result = EmployeeSubResourceUpsertRules.ShouldUpdateExisting(
                            identification.Id, existingIdentificationIds)
                        ? await _subResources.UpdateIdentificationAsync(
                            employeeId, identification.Id!.Value, write, ct)
                        : await _subResources.AddIdentificationAsync(employeeId, write, ct);
                    if (!result.Success)
                    {
                        await transaction.RollbackAsync(ct);
                        return MapIdentificationError<EmployeeAggregateReadDto>(result, i);
                    }

                    if (result.Data is not null)
                        preservedIdentificationIds.Add(result.Data.Id);
                }

                if (request.SyncIdentifications)
                {
                    foreach (var row in existingIdentificationRows)
                    {
                        if (preservedIdentificationIds.Contains(row.Id))
                            continue;

                        var deleted = await _subResources.DeleteIdentificationAsync(employeeId, row.Id, ct);
                        if (!deleted.Success)
                        {
                            await transaction.RollbackAsync(ct);
                            return Respons<EmployeeAggregateReadDto>.Fail(
                                deleted.Error ?? deleted.Detail ?? "Could not remove identification during sync.",
                                statusCode: deleted.StatusCode);
                        }
                    }
                }
            }

            if (request.Employment is not null)
            {
                if (request.Employment.DepartmentId is { } deptId
                    && !await _departments.ExistsActiveScopedAsync(deptId, _tenant.TenantId, _tenant.OrgId, ct))
                {
                    await transaction.RollbackAsync(ct);
                    return Respons<EmployeeAggregateReadDto>.ValidationError(
                        new Dictionary<string, string> { ["employment.department_id"] = "Department not found." });
                }

                if (request.Employment.BranchId is { } branchId
                    && !await _branches.ExistsActiveScopedAsync(branchId, _tenant.TenantId, _tenant.OrgId, ct))
                {
                    await transaction.RollbackAsync(ct);
                    return Respons<EmployeeAggregateReadDto>.ValidationError(
                        new Dictionary<string, string> { ["employment.branch_id"] = "Branch not found." });
                }

                var employment = await _registration.UpdateEmploymentDetailsAsync(employeeId, wizard, ct);
                if (!employment.Success)
                {
                    await transaction.RollbackAsync(ct);
                    return MapError<EmployeeAggregateReadDto>(employment);
                }

                if (request.Employment.EmploymentStatus is not null || request.Employment.ContractType is not null)
                {
                    var employmentExtrasError = await ApplyEmploymentExtrasIfNeededAsync(
                        employeeId, request.Employment, ct);
                    if (employmentExtrasError is not null)
                    {
                        await transaction.RollbackAsync(ct);
                        return employmentExtrasError;
                    }
                }
            }

            if (request.Compensation is not null)
            {
                var compensation = await _registration.UpdateCompensationAsync(employeeId, wizard, ct);
                if (!compensation.Success)
                {
                    await transaction.RollbackAsync(ct);
                    return MapError<EmployeeAggregateReadDto>(compensation);
                }
            }

            if (HasSectionCustomFields(request))
            {
                var entity = await _employees.GetByIdScopedForUpdateAsync(
                    employeeId, _tenant.TenantId, _tenant.OrgId, ct);
                if (entity is null)
                {
                    await transaction.RollbackAsync(ct);
                    return Respons<EmployeeAggregateReadDto>.Fail("Employee not found.", statusCode: 404);
                }

                await ApplySectionCustomFieldsAsync(
                    entity,
                    EmployeeAggregateMapper.DeserializeCustomFields(entity.CustomFieldsData),
                    request.Identity?.CustomFields,
                    request.Employment?.CustomFields,
                    request.Compensation?.CustomFields,
                    request.Education?.Select(e => e.CustomFields) ?? [],
                    request.Certifications?.Select(c => c.CustomFields) ?? [],
                    ct);
            }

            if (request.DeleteEducationIds is { Count: > 0 })
            {
                foreach (var educationId in request.DeleteEducationIds)
                {
                    var deleted = await _subResources.DeleteEducationAsync(employeeId, educationId, ct);
                    if (!deleted.Success)
                    {
                        await transaction.RollbackAsync(ct);
                        return Respons<EmployeeAggregateReadDto>.Fail(
                            deleted.Error ?? deleted.Detail ?? "Could not delete education record.",
                            statusCode: deleted.StatusCode);
                    }
                }
            }

            if (request.Education is not null)
            {
                var educationDupErrors = request.Education.Count > 0
                    ? EmployeeSubResourceUpsertRules.ValidateDuplicateIds(request.Education)
                    : null;
                if (educationDupErrors is not null)
                {
                    await transaction.RollbackAsync(ct);
                    return Respons<EmployeeAggregateReadDto>.ValidationError(educationDupErrors);
                }

                var existingEducation = await _subResources.ListEducationAsync(employeeId, ct);
                var existingEducationRows = existingEducation.Success && existingEducation.Data is not null
                    ? existingEducation.Data
                    : Array.Empty<EmployeeEducationDto>();
                var existingEducationIds = existingEducationRows.Select(r => r.Id).ToHashSet();
                var preservedEducationIds = new HashSet<Guid>();

                for (var i = 0; i < request.Education.Count; i++)
                {
                    var edu = request.Education[i];
                    var write = EmployeeAggregateMapper.ToEducationWrite(edu);
                    var result = EmployeeSubResourceUpsertRules.ShouldUpdateExisting(edu.Id, existingEducationIds)
                        ? await _subResources.UpdateEducationAsync(employeeId, edu.Id!.Value, write, ct)
                        : await _subResources.AddEducationAsync(employeeId, write, ct);
                    if (!result.Success)
                    {
                        await transaction.RollbackAsync(ct);
                        return MapEducationError<EmployeeAggregateReadDto>(result, i);
                    }

                    if (result.Data is not null)
                        preservedEducationIds.Add(result.Data.Id);
                }

                if (request.SyncEducation)
                {
                    foreach (var row in existingEducationRows)
                    {
                        if (preservedEducationIds.Contains(row.Id))
                            continue;

                        var deleted = await _subResources.DeleteEducationAsync(employeeId, row.Id, ct);
                        if (!deleted.Success)
                        {
                            await transaction.RollbackAsync(ct);
                            return Respons<EmployeeAggregateReadDto>.Fail(
                                deleted.Error ?? deleted.Detail ?? "Could not remove education record during sync.",
                                statusCode: deleted.StatusCode);
                        }
                    }
                }
            }

            if (request.DeleteCertificationIds is { Count: > 0 })
            {
                foreach (var certificationId in request.DeleteCertificationIds)
                {
                    var deleted = await _subResources.DeleteCertificationAsync(employeeId, certificationId, ct);
                    if (!deleted.Success)
                    {
                        await transaction.RollbackAsync(ct);
                        return Respons<EmployeeAggregateReadDto>.Fail(
                            deleted.Error ?? deleted.Detail ?? "Could not delete certification.",
                            statusCode: deleted.StatusCode);
                    }
                }
            }

            if (request.Certifications is not null)
            {
                var certificationDupErrors = request.Certifications.Count > 0
                    ? EmployeeSubResourceUpsertRules.ValidateDuplicateIds(request.Certifications)
                    : null;
                if (certificationDupErrors is not null)
                {
                    await transaction.RollbackAsync(ct);
                    return Respons<EmployeeAggregateReadDto>.ValidationError(certificationDupErrors);
                }

                var existingCertifications = await _subResources.ListCertificationsAsync(employeeId, ct);
                var existingCertificationRows = existingCertifications.Success && existingCertifications.Data is not null
                    ? existingCertifications.Data
                    : Array.Empty<EmployeeCertificationDto>();
                var existingCertificationIds = existingCertificationRows.Select(r => r.Id).ToHashSet();
                var preservedCertificationIds = new HashSet<Guid>();

                for (var i = 0; i < request.Certifications.Count; i++)
                {
                    var cert = request.Certifications[i];
                    var write = EmployeeAggregateMapper.ToCertificationWrite(cert);
                    var result = EmployeeSubResourceUpsertRules.ShouldUpdateExisting(cert.Id, existingCertificationIds)
                        ? await _subResources.UpdateCertificationAsync(employeeId, cert.Id!.Value, write, ct)
                        : await _subResources.AddCertificationAsync(employeeId, write, ct);
                    if (!result.Success)
                    {
                        await transaction.RollbackAsync(ct);
                        return MapCertificationError<EmployeeAggregateReadDto>(result, i);
                    }

                    if (result.Data is not null)
                        preservedCertificationIds.Add(result.Data.Id);
                }

                if (request.SyncCertifications)
                {
                    foreach (var row in existingCertificationRows)
                    {
                        if (preservedCertificationIds.Contains(row.Id))
                            continue;

                        var deleted = await _subResources.DeleteCertificationAsync(employeeId, row.Id, ct);
                        if (!deleted.Success)
                        {
                            await transaction.RollbackAsync(ct);
                            return Respons<EmployeeAggregateReadDto>.Fail(
                                deleted.Error ?? deleted.Detail ?? "Could not remove certification during sync.",
                                statusCode: deleted.StatusCode);
                        }
                    }
                }
            }

            var emergencyUpdateError = await SyncEmergencySectionAsync(employeeId, request, ct);
            if (emergencyUpdateError is not null)
            {
                await transaction.RollbackAsync(ct);
                return emergencyUpdateError;
            }

            var paymentUpdateError = await SyncPaymentSectionAsync(employeeId, request, ct);
            if (paymentUpdateError is not null)
            {
                await transaction.RollbackAsync(ct);
                return paymentUpdateError;
            }

            var medicalUpdateError = await SyncMedicalSectionAsync(employeeId, request, ct);
            if (medicalUpdateError is not null)
            {
                await transaction.RollbackAsync(ct);
                return medicalUpdateError;
            }

            var skillsUpdateError = await SyncSkillsSectionAsync(employeeId, request, ct);
            if (skillsUpdateError is not null)
            {
                await transaction.RollbackAsync(ct);
                return skillsUpdateError;
            }

            var experiencesUpdateError = await SyncExperiencesSectionAsync(employeeId, request, ct);
            if (experiencesUpdateError is not null)
            {
                await transaction.RollbackAsync(ct);
                return experiencesUpdateError;
            }

            var referralsUpdateError = await SyncReferralsSectionAsync(employeeId, request, ct);
            if (referralsUpdateError is not null)
            {
                await transaction.RollbackAsync(ct);
                return referralsUpdateError;
            }

            if (request.DeleteDocumentIds is { Count: > 0 })
            {
                var entity = await _employees.GetByIdScopedForUpdateAsync(
                    employeeId, _tenant.TenantId, _tenant.OrgId, ct);
                if (entity is null)
                {
                    await transaction.RollbackAsync(ct);
                    return Respons<EmployeeAggregateReadDto>.Fail("Employee not found.", statusCode: 404);
                }

                RemoveDocumentIds(entity, request.DeleteDocumentIds);
                entity.UpdatedAt = DateTimeOffset.UtcNow;
                await _employees.UpdateAsync(entity, ct);
            }

            if (request.DocumentIds is { Count: > 0 })
            {
                var entity = await _employees.GetByIdScopedForUpdateAsync(
                    employeeId, _tenant.TenantId, _tenant.OrgId, ct);
                if (entity is null)
                {
                    await transaction.RollbackAsync(ct);
                    return Respons<EmployeeAggregateReadDto>.Fail("Employee not found.", statusCode: 404);
                }

                var docErrors = await ValidateAndApplyDocumentIdsAsync(entity, request.DocumentIds, ct);
                if (docErrors is not null)
                {
                    await transaction.RollbackAsync(ct);
                    return Respons<EmployeeAggregateReadDto>.ValidationError(docErrors);
                }
            }

            if (shouldFinalise)
            {
                var finalised = await _registration.FinaliseAsync(employeeId, ct);
                if (!finalised.Success)
                {
                    await transaction.RollbackAsync(ct);
                    return MapError<EmployeeAggregateReadDto>(finalised);
                }
            }
            else if (request.IsDraft)
            {
                var draftError = await EnsureDraftFlagAsync(employeeId, ct);
                if (draftError is not null)
                {
                    await transaction.RollbackAsync(ct);
                    return draftError;
                }
            }

            await transaction.CommitAsync(ct);
            committed = true;
        }
        catch (PlatformUserConflictException ex)
        {
            if (!committed)
                await transaction.RollbackAsync(ct);
            return Respons<EmployeeAggregateReadDto>.ValidationError(
                new Dictionary<string, string> { [ex.FieldKey] = ex.Message });
        }
        catch (DbUpdateException ex) when (PostgresUniqueViolation.IsCpUserEmail(ex))
        {
            if (!committed)
                await transaction.RollbackAsync(ct);
            return Respons<EmployeeAggregateReadDto>.ValidationError(
                new Dictionary<string, string>
                {
                    ["identity.work_email"] = EmployeeErrorMessages.WorkEmailAlreadyRegistered,
                });
        }
        catch (DbUpdateException ex) when (PostgresUniqueViolation.IsCpUserContact(ex))
        {
            if (!committed)
                await transaction.RollbackAsync(ct);
            return Respons<EmployeeAggregateReadDto>.ValidationError(
                new Dictionary<string, string>
                {
                    ["identity.phone"] = EmployeeErrorMessages.PhoneAlreadyRegistered,
                });
        }
        catch (Exception ex) when (PostgresSchemaErrors.ReferencesIdentificationsStorage(ex))
        {
            if (!committed)
                await transaction.RollbackAsync(ct);
            _logger.LogError(ex, "Employee identifications storage unavailable during update for {EmployeeId}", employeeId);
            return IdentificationsStorageUnavailable();
        }
        catch (Exception ex)
        {
            if (!committed)
                await transaction.RollbackAsync(ct);
            _logger.LogError(ex, "Employee update failed for {EmployeeId}", employeeId);
            throw;
        }

        await TryRecordEmployeeUpdateAuditAsync(employeeId, request, ct);
        if (shouldFinalise)
            await _onboardingInvites.TrySendAfterFinaliseAsync(employeeId, ct);

        return await GetAsync(employeeId, ct);
    }

    public async Task<Respons<EmployeeAggregateReadDto>> GetAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _employees.GetByIdScopedAsync(id, _tenant.TenantId, _tenant.OrgId, ct);
        if (entity is null)
            return Respons<EmployeeAggregateReadDto>.Fail("Employee not found.", statusCode: 404);

        CpUserDto? cp = null;
        if (!string.IsNullOrWhiteSpace(entity.UserId))
            cp = await _cpUsers.GetByIdAsync(entity.UserId, _tenant.TenantId, ct);

        var education = await _subResources.ListEducationAsync(id, ct);
        var certifications = await _subResources.ListCertificationsAsync(id, ct);
        var identifications = await ListIdentificationsForReadAsync(id, ct);
        if (identifications.ErrorResponse is { } identificationError)
            return identificationError;

        var emergency = await _extended.ListEmergencyAsync(id, ct);
        var payment = await _extended.ListPaymentAsync(id, ct);
        var medical = await _extended.GetMedicalAsync(id, ct);
        var skills = await _extended.ListSkillsAsync(id, ct);
        var experiences = await _extended.ListExperiencesAsync(id, ct);
        var referrals = await _extended.ListReferralsAsync(id, ct);

        var fullName = EmployeeIdentityResolver.ResolveFullName(entity, cp);
        var workEmail = EmployeeIdentityResolver.ResolveWorkEmail(entity, cp);
        var storedProfileRef = EmployeeIdentityResolver.ResolveStoredProfileReference(entity, cp);
        var profileUrl = await _profileUrls.ResolveDocumentReadAsync(storedProfileRef, ct);

        var educationItems = education.Success && education.Data is { Count: > 0 } data ? data : null;
        var certificationItems = certifications.Success && certifications.Data is { Count: > 0 } certData
            ? certData
            : null;
        var identificationItems = identifications.Data is { Count: > 0 } idData
            ? idData
            : null;
        var documentIds = entity.DocumentIds.Count > 0
            ? (await _profileUrls.ResolveDocumentsAsync(entity.DocumentIds, ct))
                .Select(HrDocumentPresignedUrlService.ToEmbeddedDocument)
                .ToList()
            : null;

        CpCurrencyDto? currency = null;
        if (!string.IsNullOrWhiteSpace(entity.CurrencyId))
            currency = await _currencies.GetByIdAsync(entity.CurrencyId, _tenant.TenantId, ct);

        var definitions = await _customFieldDefinitions.ListSchemaScopedAsync(
            _tenant.TenantId, _tenant.OrgId, CustomFieldEntityTypes.Employee, ct);
        var sections = EmployeeCustomFieldMapper.SplitFromFlat(
            EmployeeAggregateMapper.DeserializeCustomFields(entity.CustomFieldsData),
            definitions);

        var auditUsers = await _cpUsers.GetByIdsAsync(
            ResourceAuditMapper.CollectUserIds(new[] { entity.CreatedBy, entity.UpdatedBy }),
            _tenant.TenantId,
            ct);

        var reportsTo = await ResolveReportsToDisplayAsync(entity, ct);
        var secondaryReportsTo = await ResolveSecondaryReportsToDisplayAsync(entity, ct);
        var employmentType = await ResolveEmploymentTypeDisplayAsync(entity, ct);
        var roleFlags = await _employees.ResolveRoleFlagsBatchAsync(
            [id], _tenant.TenantId, _tenant.OrgId, ct);

        var read = new EmployeeAggregateReadDto
        {
            Id = entity.Id,
            EmployeeCode = entity.EmployeeCode,
            EmployeeCodeSystem = entity.EmployeeCodeSystem,
            EmployeeCodeCustom = entity.EmployeeCodeCustom,
            UserId = entity.UserId,
            Identity = EmployeeAggregateReadMapper.BuildIdentity(
                fullName,
                entity,
                cp,
                workEmail,
                profileUrl,
                identificationItems,
                emergency.Success && emergency.Data is { Count: > 0 } emergencyData ? emergencyData : null,
                sections.Identity),
            Employment = EmployeeAggregateReadMapper.BuildEmployment(
                entity,
                sections.Employment,
                reportsTo,
                secondaryReportsTo,
                employmentType,
                roleFlags.IsLineManager(entity.Id),
                roleFlags.IsHeadOfDepartment(entity.Id)),
            Compensation = EmployeeAggregateReadMapper.BuildCompensation(
                entity,
                sections.Compensation,
                payment.Success && payment.Data is { Count: > 0 } paymentData ? paymentData : null,
                currency?.Code,
                currency?.Name,
                currency?.Symbol),
            Education = educationItems?
                .Select(e => e with
                {
                    CustomFields = EmployeeAggregateReadMapper.CustomFieldsOrNull(sections.Education),
                })
                .ToList(),
            Certifications = certificationItems?
                .Select(c => c with
                {
                    CustomFields = EmployeeAggregateReadMapper.CustomFieldsOrNull(sections.Certification),
                })
                .ToList(),
            Medical = medical.Success
                ? EmployeeAggregateReadMapper.BuildMedical(medical.Data)
                : null,
            Skills = skills.Success && skills.Data is { Count: > 0 } skillData ? skillData : null,
            Experiences = experiences.Success && experiences.Data is { Count: > 0 } experienceData ? experienceData : null,
            Referrals = referrals.Success && referrals.Data is { Count: > 0 } referralData ? referralData : null,
            Documents = EmployeeAggregateReadMapper.DocumentsOrNull(documentIds),
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            CreatedById = entity.CreatedBy,
            UpdatedById = entity.UpdatedBy,
            CreatedBy = ResourceAuditMapper.ResolveDisplayName(entity.CreatedBy, auditUsers),
            UpdatedBy = ResourceAuditMapper.ResolveDisplayName(entity.UpdatedBy, auditUsers),
        };

        return Respons<EmployeeAggregateReadDto>.Ok(read);
    }

    private async Task<IdentificationReadResult> ListIdentificationsForReadAsync(Guid employeeId, CancellationToken ct)
    {
        try
        {
            var result = await _subResources.ListIdentificationsAsync(employeeId, ct);
            if (!result.Success)
            {
                return new IdentificationReadResult(
                    null,
                    MapNestedError<EmployeeAggregateReadDto>(
                        result.StatusCode, result.Error, result.Detail, result.FieldErrors));
            }

            return new IdentificationReadResult(result.Data, null);
        }
        catch (Exception ex) when (PostgresSchemaErrors.ReferencesIdentificationsStorage(ex))
        {
            _logger.LogWarning(ex,
                "Employee identifications storage unavailable for employee {EmployeeId}; returning empty list",
                employeeId);
            return new IdentificationReadResult(Array.Empty<EmployeeIdentificationDto>(), null);
        }
    }

    private sealed record IdentificationReadResult(
        IReadOnlyList<EmployeeIdentificationDto>? Data,
        Respons<EmployeeAggregateReadDto>? ErrorResponse);

    public async Task<Respons<EmployeeListDto>> ListAsync(
        EmployeeListQuery query, CancellationToken ct = default)
    {
        if (query.StartDate is not null && query.EndDate is not null && query.StartDate > query.EndDate)
        {
            return Respons<EmployeeListDto>.ValidationError(new Dictionary<string, string>
            {
                ["start_date"] = "start_date must be on or before end_date.",
            });
        }

        var paging = PagedQuery.From(query.Page, query.Size);
        var (rows, total) = await _employees.ListScopedAsync(
            query, _tenant.TenantId, _tenant.OrgId, paging.Page, paging.Size, ct);

        var roleFlags = await _employees.ResolveRoleFlagsBatchAsync(
            rows.Select(r => r.Id).ToList(),
            _tenant.TenantId,
            _tenant.OrgId,
            ct);

        var userIds = rows
            .SelectMany(r => new[] { r.UserId, r.CreatedBy, r.UpdatedBy })
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!)
            .Distinct();
        var platformUsers = await _cpUsers.GetByIdsAsync(userIds, _tenant.TenantId, ct);

        var storedProfileRefs = rows
            .Select(e =>
            {
                platformUsers.TryGetValue(e.UserId ?? string.Empty, out var cp);
                return EmployeeIdentityResolver.ResolveStoredProfileReference(e, cp);
            })
            .ToList();
        var profileUrlMap = await _profileUrls.ResolveDocumentReadsAsync(storedProfileRefs, ct);

        var items = rows.Select(e =>
        {
            platformUsers.TryGetValue(e.UserId ?? string.Empty, out var cp);
            var storedProfileRef = EmployeeIdentityResolver.ResolveStoredProfileReference(e, cp);
            var profileUrl = storedProfileRef is null
                ? null
                : profileUrlMap.GetValueOrDefault(storedProfileRef.Trim());
            return new EmployeeListItemDto
            {
                EmployeeId = e.Id.ToString(),
                EmployeeCode = e.EmployeeCode,
                FullName = EmployeeIdentityResolver.ResolveFullName(e, cp),
                JobTitle = e.JobTitle,
                DepartmentName = e.Department?.Name,
                BranchName = e.Branch?.Name,
                WorkLocation = e.WorkLocation,
                EmploymentStatus = e.EmploymentStatus,
                Engagement = EmployeeStatusFilter.ResolveEngagement(e),
                WorkStates = EmployeeStatusFilter.ResolveWorkStates(e, DateOnly.FromDateTime(DateTime.UtcNow)),
                EmploymentType = e.EmploymentType,
                ProfileUrl = profileUrl,
                CreatedAt = e.CreatedAt,
                UpdatedAt = e.UpdatedAt,
                CreatedById = e.CreatedBy,
                UpdatedById = e.UpdatedBy,
                CreatedBy = ResourceAuditMapper.ResolveDisplayName(e.CreatedBy, platformUsers),
                UpdatedBy = ResourceAuditMapper.ResolveDisplayName(e.UpdatedBy, platformUsers),
                IsLineManager = roleFlags.IsLineManager(e.Id),
                IsHeadOfDepartment = roleFlags.IsHeadOfDepartment(e.Id),
            };
        }).ToList();

        return Respons<EmployeeListDto>.Ok(
            new EmployeeListDto { Items = items },
            pagination: new PaginationMeta
            {
                Page = paging.Page,
                Size = paging.Size,
                Total = total,
                HasNext = paging.Offset + items.Count < total,
            });
    }

    private static bool HasAnyUpdate(UpdateEmployeeAggregateRequest request) =>
        request.Identity is not null
        || request.Employment is not null
        || request.Compensation is not null
        || request.Education is not null
        || request.Certifications is not null
        || request.Medical is not null
        || request.Skills is not null
        || request.Experiences is not null
        || request.Referrals is not null
        || request.DeleteEducationIds is { Count: > 0 }
        || request.DeleteCertificationIds is { Count: > 0 }
        || request.DeleteIdentificationIds is { Count: > 0 }
        || request.DeleteEmergencyIds is { Count: > 0 }
        || request.DeletePaymentIds is { Count: > 0 }
        || request.DeleteMedicalConditionIds is { Count: > 0 }
        || request.DeleteAllergyIds is { Count: > 0 }
        || request.DeleteMedicationIds is { Count: > 0 }
        || request.DeleteSkillIds is { Count: > 0 }
        || request.DeleteExperienceIds is { Count: > 0 }
        || request.DeleteReferralIds is { Count: > 0 }
        || request.SyncIdentifications
        || request.SyncEmergency
        || request.SyncPayment
        || request.SyncMedicalConditions
        || request.SyncAllergies
        || request.SyncMedications
        || request.SyncSkills
        || request.SyncExperiences
        || request.SyncReferrals
        || request.Identity?.Identifications is not null
        || request.Identity?.Emergency is not null
        || request.Compensation?.Payment is not null
        || request.DeleteDocumentIds is { Count: > 0 }
        || request.DocumentIds is { Count: > 0 }
        || HasSectionCustomFields(request);

    private static bool HasSectionCustomFields(CreateEmployeeAggregateRequest request) =>
        HasCustomFields(request.Identity.CustomFields)
        || HasCustomFields(request.Employment?.CustomFields)
        || HasCustomFields(request.Compensation?.CustomFields)
        || request.Education.Any(e => HasCustomFields(e.CustomFields))
        || request.Certifications.Any(c => HasCustomFields(c.CustomFields));

    private static bool HasSectionCustomFields(UpdateEmployeeAggregateRequest request) =>
        HasCustomFields(request.Identity?.CustomFields)
        || HasCustomFields(request.Employment?.CustomFields)
        || HasCustomFields(request.Compensation?.CustomFields)
        || request.Education?.Any(e => HasCustomFields(e.CustomFields)) == true
        || request.Certifications?.Any(c => HasCustomFields(c.CustomFields)) == true;

    private static bool HasCustomFields(Dictionary<string, string?>? fields) => fields is { Count: > 0 };

    private static bool HasIdentityPersonalContactUpdate(EmployeeAggregateIdentityDto identity) =>
        !string.IsNullOrWhiteSpace(identity.FullName)
        || identity.DateOfBirth is not null
        || !string.IsNullOrWhiteSpace(identity.Gender)
        || !string.IsNullOrWhiteSpace(identity.Country)
        || !string.IsNullOrWhiteSpace(identity.MaritalStatus)
        || !string.IsNullOrWhiteSpace(identity.NextOfKinName)
        || !string.IsNullOrWhiteSpace(identity.NextOfKinPhone)
        || !string.IsNullOrWhiteSpace(identity.RelationshipToNextOfKin)
        || identity.Emergency is not null
        || !string.IsNullOrWhiteSpace(identity.PersonalEmail)
        || !string.IsNullOrWhiteSpace(identity.WorkEmail)
        || !string.IsNullOrWhiteSpace(identity.Phone)
        || !string.IsNullOrWhiteSpace(identity.LinkedInUrl)
        || !string.IsNullOrWhiteSpace(identity.ResidentialAddress)
        || identity.ProfileUrl is not null
        || HasCustomFields(identity.CustomFields);

    private async Task ApplySectionCustomFieldsAsync(
        EmployeeEntity entity,
        Dictionary<string, string?> currentFlat,
        Dictionary<string, string?>? identityFields,
        Dictionary<string, string?>? employmentFields,
        Dictionary<string, string?>? compensationFields,
        IEnumerable<Dictionary<string, string?>?> educationFieldSets,
        IEnumerable<Dictionary<string, string?>?> certificationFieldSets,
        CancellationToken ct)
    {
        var definitions = await _customFieldDefinitions.ListSchemaScopedAsync(
            _tenant.TenantId, _tenant.OrgId, CustomFieldEntityTypes.Employee, ct);
        var merged = EmployeeCustomFieldMapper.MergeIntoFlat(
            currentFlat,
            definitions,
            identityFields,
            employmentFields,
            compensationFields,
            educationFieldSets,
            certificationFieldSets);

        entity.CustomFieldsData = EmployeeAggregateMapper.SerializeCustomFields(merged);
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await _employees.UpdateAsync(entity, ct);
    }

    private async Task<Dictionary<string, string>?> ValidateAndApplyDocumentIdsAsync(
        EmployeeEntity entity,
        IReadOnlyList<string> documentIds,
        CancellationToken ct)
    {
        var errors = new Dictionary<string, string>();

        for (var i = 0; i < documentIds.Count; i++)
        {
            var documentId = documentIds[i]?.Trim();
            if (string.IsNullOrWhiteSpace(documentId))
            {
                errors[$"document_ids[{i}]"] = "Document id is required.";
                continue;
            }

            var doc = await _hrDocuments.GetByIdAsync(documentId, _tenant.TenantId, _tenant.OrgId, ct);
            if (doc is null)
                errors[$"document_ids[{i}]"] = "Document not found in file registry.";
        }

        if (errors.Count > 0)
            return errors;

        var merged = entity.DocumentIds.ToList();
        foreach (var id in documentIds)
        {
            var trimmed = id.Trim();
            if (!merged.Contains(trimmed, StringComparer.Ordinal))
                merged.Add(trimmed);
        }

        entity.DocumentIds = merged;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await _employees.UpdateAsync(entity, ct);
        return null;
    }

    private static void RemoveDocumentIds(EmployeeEntity entity, IReadOnlyList<string> deleteDocumentIds)
    {
        if (entity.DocumentIds.Count == 0)
            return;

        var remove = new HashSet<string>(
            deleteDocumentIds.Where(id => !string.IsNullOrWhiteSpace(id)).Select(id => id.Trim()),
            StringComparer.Ordinal);
        entity.DocumentIds = entity.DocumentIds.Where(id => !remove.Contains(id)).ToList();
    }

    private async Task<EmploymentTypeDisplay?> ResolveEmploymentTypeDisplayAsync(
        EmployeeEntity entity,
        CancellationToken ct)
    {
        if (entity.EmploymentTypeRef is not null)
        {
            return new EmploymentTypeDisplay(
                entity.EmploymentTypeRef.Id.ToString(),
                entity.EmploymentTypeRef.Name,
                entity.EmploymentTypeRef.Description,
                entity.EmploymentTypeRef.IsSystemDefault
                    ? EmploymentTypeKind.Default
                    : EmploymentTypeKind.Custom);
        }

        var resolved = await _employmentTypes.ResolveRefAsync(
            entity.EmploymentTypeId,
            entity.EmploymentType,
            _tenant.TenantId,
            _tenant.OrgId,
            ct);

        if (resolved is null || string.IsNullOrWhiteSpace(resolved.Id))
            return null;

        return new EmploymentTypeDisplay(
            resolved.Id,
            resolved.Name,
            resolved.Description,
            resolved.Type);
    }

    private async Task<ReportsToDisplay?> ResolveSecondaryReportsToDisplayAsync(
        EmployeeEntity entity,
        CancellationToken ct)
    {
        if (!entity.DottedLineManagerId.HasValue)
            return null;

        return await ResolveManagerDisplayAsync(entity.DottedLineManagerId.Value, entity.DottedLineManager, ct);
    }

    private async Task<ReportsToDisplay?> ResolveReportsToDisplayAsync(
        EmployeeEntity entity,
        CancellationToken ct)
    {
        if (!entity.ReportsToId.HasValue)
            return null;

        var reportsTo = entity.ReportsTo
            ?? (entity.ManagerId == entity.ReportsToId ? entity.Manager : null);

        return await ResolveManagerDisplayAsync(entity.ReportsToId.Value, reportsTo, ct);
    }

    private async Task<ReportsToDisplay?> ResolveManagerDisplayAsync(
        Guid managerId,
        EmployeeEntity? manager,
        CancellationToken ct)
    {
        manager ??= await _employees.GetByIdScopedAsync(managerId, _tenant.TenantId, _tenant.OrgId, ct);
        if (manager is null)
            return null;

        CpUserDto? managerCp = null;
        if (!string.IsNullOrWhiteSpace(manager.UserId))
            managerCp = await _cpUsers.GetByIdAsync(manager.UserId, _tenant.TenantId, ct);

        var photoRef = EmployeeIdentityResolver.ResolveStoredProfileReference(manager, managerCp);
        var photoUrl = await _profileUrls.ResolveDocumentReadAsync(photoRef, ct);

        return new ReportsToDisplay(
            managerId,
            EmployeeIdentityResolver.ResolveFullName(manager, managerCp),
            manager.JobTitle,
            photoUrl);
    }

    private async Task<Respons<EmployeeAggregateReadDto>?> SyncEmergencySectionAsync(
        Guid employeeId,
        UpdateEmployeeAggregateRequest request,
        CancellationToken ct)
    {
        if (request.DeleteEmergencyIds is { Count: > 0 })
        {
            foreach (var rowId in request.DeleteEmergencyIds)
            {
                var deleted = await _extended.DeleteEmergencyAsync(employeeId, rowId, ct);
                if (!deleted.Success)
                    return FailSync<EmployeeAggregateReadDto>(deleted, "Could not delete emergency contact.");
            }
        }

        if (request.Identity?.Emergency is null)
            return null;

        return await SyncSimpleArrayAsync(
            request.Identity.Emergency,
            request.SyncEmergency,
            () => _extended.ListEmergencyAsync(employeeId, ct),
            i => i.Id,
            r => r.Id,
            (write) => _extended.AddEmergencyAsync(employeeId, write, ct),
            (id, write) => _extended.UpdateEmergencyAsync(employeeId, id, write, ct),
            id => _extended.DeleteEmergencyAsync(employeeId, id, ct),
            EmployeeExtendedProfileService.ToEmergencyWrite,
            "identity.emergency",
            ct);
    }

    private async Task<Respons<EmployeeAggregateReadDto>?> SyncPaymentSectionAsync(
        Guid employeeId,
        UpdateEmployeeAggregateRequest request,
        CancellationToken ct)
    {
        if (request.DeletePaymentIds is { Count: > 0 })
        {
            foreach (var rowId in request.DeletePaymentIds)
            {
                var deleted = await _extended.DeletePaymentAsync(employeeId, rowId, ct);
                if (!deleted.Success)
                    return FailSync<EmployeeAggregateReadDto>(deleted, "Could not delete payment method.");
            }
        }

        if (request.Compensation?.Payment is null)
            return null;

        return await SyncSimpleArrayAsync(
            request.Compensation.Payment,
            request.SyncPayment,
            () => _extended.ListPaymentAsync(employeeId, ct),
            i => i.Id,
            r => r.Id,
            (write) => _extended.AddPaymentAsync(employeeId, write, ct),
            (id, write) => _extended.UpdatePaymentAsync(employeeId, id, write, ct),
            id => _extended.DeletePaymentAsync(employeeId, id, ct),
            EmployeeExtendedProfileService.ToPaymentWrite,
            "compensation.payment",
            ct);
    }

    private async Task<Respons<EmployeeAggregateReadDto>?> SyncMedicalSectionAsync(
        Guid employeeId,
        UpdateEmployeeAggregateRequest request,
        CancellationToken ct)
    {
        if (request.Medical is not null && EmployeeAggregateExtendedSync.HasMedicalScalars(request.Medical))
        {
            var profile = await _extended.UpsertMedicalScalarsAsync(employeeId, request.Medical, ct);
            if (!profile.Success)
                return MapNestedError<EmployeeAggregateReadDto>(profile.StatusCode, profile.Error, profile.Detail, profile.FieldErrors);
        }

        if (request.DeleteMedicalConditionIds is { Count: > 0 })
        {
            foreach (var rowId in request.DeleteMedicalConditionIds)
            {
                var deleted = await _extended.DeleteMedicalConditionAsync(employeeId, rowId, ct);
                if (!deleted.Success)
                    return FailSync<EmployeeAggregateReadDto>(deleted, "Could not delete medical condition.");
            }
        }

        if (request.Medical?.MedicalConditions is not null)
        {
            var error = await SyncSimpleArrayAsync(
                request.Medical.MedicalConditions,
                request.SyncMedicalConditions,
                () => ListMedicalConditionsAsync(employeeId, ct),
                i => i.Id,
                r => r.Id,
                write => _extended.AddMedicalConditionAsync(employeeId, write, ct),
                (id, write) => _extended.UpdateMedicalConditionAsync(employeeId, id, write, ct),
                id => _extended.DeleteMedicalConditionAsync(employeeId, id, ct),
                EmployeeExtendedProfileService.ToMedicalConditionWrite,
                "medical.medical_conditions",
                ct);
            if (error is not null)
                return error;
        }

        if (request.DeleteAllergyIds is { Count: > 0 })
        {
            foreach (var rowId in request.DeleteAllergyIds)
            {
                var deleted = await _extended.DeleteAllergyAsync(employeeId, rowId, ct);
                if (!deleted.Success)
                    return FailSync<EmployeeAggregateReadDto>(deleted, "Could not delete allergy.");
            }
        }

        if (request.Medical?.Allergies is not null)
        {
            var error = await SyncSimpleArrayAsync(
                request.Medical.Allergies,
                request.SyncAllergies,
                () => ListMedicalAllergiesAsync(employeeId, ct),
                i => i.Id,
                r => r.Id,
                write => _extended.AddAllergyAsync(employeeId, write, ct),
                (id, write) => _extended.UpdateAllergyAsync(employeeId, id, write, ct),
                id => _extended.DeleteAllergyAsync(employeeId, id, ct),
                EmployeeExtendedProfileService.ToAllergyWrite,
                "medical.allergies",
                ct);
            if (error is not null)
                return error;
        }

        if (request.DeleteMedicationIds is { Count: > 0 })
        {
            foreach (var rowId in request.DeleteMedicationIds)
            {
                var deleted = await _extended.DeleteMedicationAsync(employeeId, rowId, ct);
                if (!deleted.Success)
                    return FailSync<EmployeeAggregateReadDto>(deleted, "Could not delete medication.");
            }
        }

        if (request.Medical?.Medications is not null)
        {
            var error = await SyncSimpleArrayAsync(
                request.Medical.Medications,
                request.SyncMedications,
                () => ListMedicalMedicationsAsync(employeeId, ct),
                i => i.Id,
                r => r.Id,
                write => _extended.AddMedicationAsync(employeeId, write, ct),
                (id, write) => _extended.UpdateMedicationAsync(employeeId, id, write, ct),
                id => _extended.DeleteMedicationAsync(employeeId, id, ct),
                EmployeeExtendedProfileService.ToMedicationWrite,
                "medical.medications",
                ct);
            if (error is not null)
                return error;
        }

        return null;
    }

    private async Task<Respons<IReadOnlyList<EmployeeMedicalConditionDto>>> ListMedicalConditionsAsync(
        Guid employeeId, CancellationToken ct)
    {
        var medical = await _extended.GetMedicalAsync(employeeId, ct);
        if (!medical.Success)
            return Respons<IReadOnlyList<EmployeeMedicalConditionDto>>.Fail(medical.Error ?? medical.Detail ?? "Could not load medical.", statusCode: medical.StatusCode);
        return Respons<IReadOnlyList<EmployeeMedicalConditionDto>>.Ok(medical.Data?.MedicalConditions ?? Array.Empty<EmployeeMedicalConditionDto>());
    }

    private async Task<Respons<IReadOnlyList<EmployeeAllergyDto>>> ListMedicalAllergiesAsync(
        Guid employeeId, CancellationToken ct)
    {
        var medical = await _extended.GetMedicalAsync(employeeId, ct);
        if (!medical.Success)
            return Respons<IReadOnlyList<EmployeeAllergyDto>>.Fail(medical.Error ?? medical.Detail ?? "Could not load medical.", statusCode: medical.StatusCode);
        return Respons<IReadOnlyList<EmployeeAllergyDto>>.Ok(medical.Data?.Allergies ?? Array.Empty<EmployeeAllergyDto>());
    }

    private async Task<Respons<IReadOnlyList<EmployeeMedicationDto>>> ListMedicalMedicationsAsync(
        Guid employeeId, CancellationToken ct)
    {
        var medical = await _extended.GetMedicalAsync(employeeId, ct);
        if (!medical.Success)
            return Respons<IReadOnlyList<EmployeeMedicationDto>>.Fail(medical.Error ?? medical.Detail ?? "Could not load medical.", statusCode: medical.StatusCode);
        return Respons<IReadOnlyList<EmployeeMedicationDto>>.Ok(medical.Data?.Medications ?? Array.Empty<EmployeeMedicationDto>());
    }

    private async Task<Respons<EmployeeAggregateReadDto>?> SyncSkillsSectionAsync(
        Guid employeeId,
        UpdateEmployeeAggregateRequest request,
        CancellationToken ct)
    {
        if (request.DeleteSkillIds is { Count: > 0 })
        {
            foreach (var rowId in request.DeleteSkillIds)
            {
                var deleted = await _extended.DeleteSkillAsync(employeeId, rowId, ct);
                if (!deleted.Success)
                    return FailSync<EmployeeAggregateReadDto>(deleted, "Could not delete skill.");
            }
        }

        if (request.Skills is null)
            return null;

        return await SyncSimpleArrayAsync(
            request.Skills,
            request.SyncSkills,
            () => _extended.ListSkillsAsync(employeeId, ct),
            i => i.Id,
            r => r.Id,
            write => _extended.AddSkillAsync(employeeId, write, ct),
            (id, write) => _extended.UpdateSkillAsync(employeeId, id, write, ct),
            id => _extended.DeleteSkillAsync(employeeId, id, ct),
            EmployeeExtendedProfileService.ToSkillWrite,
            "skills",
            ct);
    }

    private async Task<Respons<EmployeeAggregateReadDto>?> SyncExperiencesSectionAsync(
        Guid employeeId,
        UpdateEmployeeAggregateRequest request,
        CancellationToken ct)
    {
        if (request.DeleteExperienceIds is { Count: > 0 })
        {
            foreach (var rowId in request.DeleteExperienceIds)
            {
                var deleted = await _extended.DeleteExperienceAsync(employeeId, rowId, ct);
                if (!deleted.Success)
                    return FailSync<EmployeeAggregateReadDto>(deleted, "Could not delete experience.");
            }
        }

        if (request.Experiences is null)
            return null;

        return await SyncSimpleArrayAsync(
            request.Experiences,
            request.SyncExperiences,
            () => _extended.ListExperiencesAsync(employeeId, ct),
            i => i.Id,
            r => r.Id,
            write => _extended.AddExperienceAsync(employeeId, write, ct),
            (id, write) => _extended.UpdateExperienceAsync(employeeId, id, write, ct),
            id => _extended.DeleteExperienceAsync(employeeId, id, ct),
            EmployeeExtendedProfileService.ToExperienceWrite,
            "experiences",
            ct);
    }

    private async Task<Respons<EmployeeAggregateReadDto>?> SyncReferralsSectionAsync(
        Guid employeeId,
        UpdateEmployeeAggregateRequest request,
        CancellationToken ct)
    {
        if (request.DeleteReferralIds is { Count: > 0 })
        {
            foreach (var rowId in request.DeleteReferralIds)
            {
                var deleted = await _extended.DeleteReferralAsync(employeeId, rowId, ct);
                if (!deleted.Success)
                    return FailSync<EmployeeAggregateReadDto>(deleted, "Could not delete referral.");
            }
        }

        if (request.Referrals is null)
            return null;

        return await SyncSimpleArrayAsync(
            request.Referrals,
            request.SyncReferrals,
            () => _extended.ListReferralsAsync(employeeId, ct),
            i => i.Id,
            r => r.Id,
            write => _extended.AddReferralAsync(employeeId, write, ct),
            (id, write) => _extended.UpdateReferralAsync(employeeId, id, write, ct),
            id => _extended.DeleteReferralAsync(employeeId, id, ct),
            EmployeeExtendedProfileService.ToReferralWrite,
            "referrals",
            ct);
    }

    private async Task<Respons<EmployeeAggregateReadDto>?> SyncSimpleArrayAsync<TUpsert, TDto, TWrite>(
        IReadOnlyList<TUpsert> items,
        bool sync,
        Func<Task<Respons<IReadOnlyList<TDto>>>> listAsync,
        Func<TUpsert, Guid?> getId,
        Func<TDto, Guid> getDtoId,
        Func<TWrite, Task<Respons<TDto>>> addAsync,
        Func<Guid, TWrite, Task<Respons<TDto>>> updateAsync,
        Func<Guid, Task<Respons<object>>> deleteAsync,
        Func<TUpsert, TWrite> toWrite,
        string fieldPrefix,
        CancellationToken ct)
    {
        var dupErrors = items.Count > 0
            ? EmployeeSubResourceUpsertRules.ValidateDuplicateIds(items, fieldPrefix, getId)
            : null;
        if (dupErrors is not null)
            return Respons<EmployeeAggregateReadDto>.ValidationError(dupErrors);

        var existing = await listAsync();
        var existingRows = existing.Success && existing.Data is not null
            ? existing.Data
            : Array.Empty<TDto>();
        var existingIds = existingRows.Select(getDtoId).ToHashSet();
        var preservedIds = new HashSet<Guid>();

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var write = toWrite(item);
            var id = getId(item);
            var result = EmployeeSubResourceUpsertRules.ShouldUpdateExisting(id, existingIds)
                ? await updateAsync(id!.Value, write)
                : await addAsync(write);
            if (!result.Success)
                return MapIndexedRowError<EmployeeAggregateReadDto, TDto>(result, $"{fieldPrefix}[{i}]");

            preservedIds.Add(getDtoId(result.Data!));
        }

        if (sync)
        {
            foreach (var row in existingRows)
            {
                var rowId = getDtoId(row);
                if (preservedIds.Contains(rowId))
                    continue;

                var deleted = await deleteAsync(rowId);
                if (!deleted.Success)
                    return FailSync<EmployeeAggregateReadDto>(deleted, $"Could not remove {fieldPrefix} row during sync.");
            }
        }

        return null;
    }

    private static Respons<T> FailSync<T>(Respons<object> source, string fallback) =>
        Respons<T>.Fail(source.Error ?? source.Detail ?? fallback, statusCode: source.StatusCode);

    private static Respons<T> MapIndexedRowError<T, TItem>(Respons<TItem> source, string prefix)
    {
        if (source.StatusCode == 404)
        {
            return Respons<T>.ValidationError(
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    [$"{prefix}.id"] = "Row not found for this employee.",
                },
                source.Error ?? source.Detail);
        }

        if (source.FieldErrors is { Count: > 0 })
        {
            var remapped = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var (key, message) in source.FieldErrors)
                remapped[$"{prefix}.{key}"] = message;
            return Respons<T>.ValidationError(remapped, source.Error ?? source.Detail);
        }

        return Respons<T>.Fail(source.Error ?? source.Detail ?? "Request failed.", statusCode: source.StatusCode);
    }

    private static Dictionary<string, string>? ValidateUpdate(UpdateEmployeeAggregateRequest request)
    {
        var errors = new Dictionary<string, string>(StringComparer.Ordinal);

        var identityErrors = EmployeeIdentityFieldValidator.ValidateForUpdate(request.Identity);
        if (identityErrors is not null)
        {
            foreach (var (key, value) in identityErrors)
                errors[key] = value;
        }

        if (request.Education is { Count: > MaxEducation })
            errors["education"] = $"At most {MaxEducation} education records allowed per request.";

        if (request.Certifications is { Count: > MaxCertifications })
            errors["certifications"] = $"At most {MaxCertifications} certification records allowed per request.";

        var identificationCount = request.Identity?.Identifications?.Count ?? 0;
        if (identificationCount > MaxIdentifications)
            errors["identity.identifications"] = $"At most {MaxIdentifications} identification records allowed per request.";

        return errors.Count == 0 ? null : errors;
    }

    private async Task<Respons<EmployeeAggregateReadDto>?> ApplyIdentificationsCreateAsync(
        Guid employeeId,
        IReadOnlyList<EmployeeIdentificationUpsertDto> identifications,
        CancellationToken ct)
    {
        var duplicateIdErrors = EmployeeSubResourceUpsertRules.ValidateDuplicateIds(identifications);
        if (duplicateIdErrors is not null)
            return Respons<EmployeeAggregateReadDto>.ValidationError(duplicateIdErrors);

        var duplicateTypeErrors = EmployeeSubResourceUpsertRules.ValidateDuplicateIdCardTypeIds(identifications);
        if (duplicateTypeErrors is not null)
            return Respons<EmployeeAggregateReadDto>.ValidationError(duplicateTypeErrors);

        for (var i = 0; i < identifications.Count; i++)
        {
            var write = EmployeeAggregateMapper.ToIdentificationWrite(identifications[i]);
            var added = await _subResources.AddIdentificationAsync(employeeId, write, ct);
            if (!added.Success)
                return MapIdentificationError<EmployeeAggregateReadDto>(added, i);
        }

        return null;
    }

    private static Respons<T> MapIdentificationError<T>(Respons<EmployeeIdentificationDto> source, int index)
    {
        if (source.StatusCode == 404)
        {
            return Respons<T>.ValidationError(
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    [$"identity.identifications[{index}].id"] = "Identification not found for this employee.",
                },
                source.Error ?? source.Detail);
        }

        if (source.FieldErrors is { Count: > 0 })
        {
            var remapped = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var (key, message) in source.FieldErrors)
                remapped[$"identity.identifications[{index}].{key}"] = message;

            return Respons<T>.ValidationError(remapped, source.Error ?? source.Detail);
        }

        return MapNestedError<T>(source.StatusCode, source.Error, source.Detail, source.FieldErrors);
    }

    private async Task<Respons<EmployeeAggregateReadDto>?> EnsureDraftFlagAsync(
        Guid employeeId,
        CancellationToken ct)
    {
        var entity = await _employees.GetByIdScopedForUpdateAsync(
            employeeId, _tenant.TenantId, _tenant.OrgId, ct);
        if (entity is null)
            return Respons<EmployeeAggregateReadDto>.Fail("Employee not found.", statusCode: 404);

        entity.IsDraft = true;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await _employees.UpdateAsync(entity, ct);
        return null;
    }

    private async Task<Respons<EmployeeAggregateReadDto>?> ApplyEmploymentExtrasIfNeededAsync(
        Guid employeeId,
        EmployeeAggregateEmploymentDto employment,
        CancellationToken ct)
    {
        if (employment.EmploymentStatus is null && employment.ContractType is null)
            return null;

        var entity = await _employees.GetByIdScopedForUpdateAsync(
            employeeId, _tenant.TenantId, _tenant.OrgId, ct);
        if (entity is null)
            return Respons<EmployeeAggregateReadDto>.Fail("Employee not found.", statusCode: 404);

        EmployeeRegistrationService.ApplyEmploymentExtras(entity, employment);
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await _employees.UpdateAsync(entity, ct);
        return null;
    }

    private static Respons<T> MapError<T>(Respons<EmployeeRegistrationReadDto> source)
    {
        if (source.FieldErrors is { Count: > 0 })
            return Respons<T>.ValidationError(
                new Dictionary<string, string>(source.FieldErrors),
                source.Error ?? source.Detail);

        return Fail<T>(source.StatusCode, source.Error, source.Detail);
    }

    private static Respons<T> MapEducationError<T>(Respons<EmployeeEducationDto> source, int index)
    {
        if (source.StatusCode == 404)
        {
            return Respons<T>.ValidationError(
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    [$"education[{index}].id"] = "Education record not found for this employee.",
                },
                source.Error ?? source.Detail);
        }

        if (source.FieldErrors is { Count: > 0 })
            return Respons<T>.ValidationError(
                new Dictionary<string, string>(source.FieldErrors),
                source.Error ?? source.Detail);

        return MapNestedError<T>(source.StatusCode, source.Error, source.Detail, source.FieldErrors);
    }

    private static Respons<T> MapCertificationError<T>(Respons<EmployeeCertificationDto> source, int index)
    {
        if (source.StatusCode == 404)
        {
            return Respons<T>.ValidationError(
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    [$"certifications[{index}].id"] = "Certification not found for this employee.",
                },
                source.Error ?? source.Detail);
        }

        return MapError<T>(source);
    }

    private static Respons<T> MapError<T>(Respons<EmployeeEducationDto> source) =>
        MapNestedError<T>(source.StatusCode, source.Error, source.Detail, source.FieldErrors);

    private static Respons<T> MapError<T>(Respons<EmployeeCertificationDto> source) =>
        MapNestedError<T>(source.StatusCode, source.Error, source.Detail, source.FieldErrors);

    private static Respons<T> MapNestedError<T>(
        int statusCode,
        string? error,
        string? detail,
        Dictionary<string, string>? fieldErrors)
    {
        if (fieldErrors is { Count: > 0 })
            return Respons<T>.ValidationError(new Dictionary<string, string>(fieldErrors), error ?? detail);

        return Fail<T>(statusCode, error, detail);
    }

    private static Respons<T> Fail<T>(int statusCode, string? error, string? detail) =>
        Respons<T>.Fail(error ?? detail ?? "Request failed.", statusCode: statusCode);

    private static Respons<EmployeeAggregateReadDto> IdentificationsStorageUnavailable() =>
        Respons<EmployeeAggregateReadDto>.Fail(
            "Employee identifications storage is not deployed on this database.",
            statusCode: 503);

    private async Task TryRecordEmployeeCreateAuditAsync(
        Guid employeeId,
        bool isFinalised,
        CancellationToken ct)
    {
        try
        {
            await RecordEmployeeCreateAuditAsync(employeeId, isFinalised, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Audit log append failed after employee create {EmployeeId}; employee was saved.",
                employeeId);
        }
    }

    private async Task TryRecordEmployeeUpdateAuditAsync(
        Guid employeeId,
        UpdateEmployeeAggregateRequest request,
        CancellationToken ct)
    {
        try
        {
            await RecordEmployeeUpdateAuditAsync(employeeId, request, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Audit log append failed after employee update {EmployeeId}; employee was saved.",
                employeeId);
        }
    }

    private async Task RecordEmployeeCreateAuditAsync(
        Guid employeeId,
        bool isFinalised,
        CancellationToken ct)
    {
        var (code, fullName) = await ResolveEmployeeAuditIdentityAsync(employeeId, ct);
        var auditEvent = AuditLogEmployeeEvents.WithEmployee(
            AuditLogEmployeeEvents.ForCreate(isFinalised),
            employeeId,
            code,
            fullName);
        await _auditLogs.RecordAsync(_tenant.TenantId, _tenant.OrgId, auditEvent, ct);
    }

    private async Task RecordEmployeeUpdateAuditAsync(
        Guid employeeId,
        UpdateEmployeeAggregateRequest request,
        CancellationToken ct)
    {
        var (code, fullName) = await ResolveEmployeeAuditIdentityAsync(employeeId, ct);
        var auditEvent = AuditLogEmployeeEvents.ForUpdate(request, employeeId, code, fullName);
        await _auditLogs.RecordAsync(_tenant.TenantId, _tenant.OrgId, auditEvent, ct);
    }

    private async Task<(string Code, string FullName)> ResolveEmployeeAuditIdentityAsync(
        Guid employeeId,
        CancellationToken ct)
    {
        var entity = await _employees.GetByIdScopedAsync(employeeId, _tenant.TenantId, _tenant.OrgId, ct);
        if (entity is null)
            return (string.Empty, "Unknown employee");

        CpUserDto? cp = null;
        if (!string.IsNullOrWhiteSpace(entity.UserId))
            cp = await _cpUsers.GetByIdAsync(entity.UserId, _tenant.TenantId, ct);

        var fullName = EmployeeIdentityResolver.ResolveFullName(entity, cp);
        return (entity.EmployeeCode ?? string.Empty, fullName);
    }
}
