using Microsoft.EntityFrameworkCore;
using ZelosHR.Api.Entities.AuditLogs;
using ZelosHR.Api.Entities.Branches;
using ZelosHR.Api.Entities.CustomFields;
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

    private readonly ZelosHrDbContext _db;
    private readonly EmployeeRegistrationService _registration;
    private readonly EmployeeSubResourcesService _subResources;
    private readonly IEmployeeRepository _employees;
    private readonly ICpUserRepository _cpUsers;
    private readonly IDepartmentRepository _departments;
    private readonly IBranchRepository _branches;
    private readonly EmployeesService _employeesService;
    private readonly ICustomFieldDefinitionsRepository _customFieldDefinitions;
    private readonly IHrDocumentPathRepository _hrDocuments;
    private readonly ICpCurrencyRepository _currencies;
    private readonly HrDocumentPresignedUrlService _profileUrls;
    private readonly IAuditLogWriter _auditLogs;
    private readonly ITenantContext _tenant;
    private readonly ILogger<EmployeeAggregateService> _logger;

    public EmployeeAggregateService(
        ZelosHrDbContext db,
        EmployeeRegistrationService registration,
        EmployeeSubResourcesService subResources,
        IEmployeeRepository employees,
        ICpUserRepository cpUsers,
        IDepartmentRepository departments,
        IBranchRepository branches,
        EmployeesService employeesService,
        ICustomFieldDefinitionsRepository customFieldDefinitions,
        IHrDocumentPathRepository hrDocuments,
        ICpCurrencyRepository currencies,
        HrDocumentPresignedUrlService profileUrls,
        IAuditLogWriter auditLogs,
        ITenantContext tenant,
        ILogger<EmployeeAggregateService> logger)
    {
        _db = db;
        _registration = registration;
        _subResources = subResources;
        _employees = employees;
        _cpUsers = cpUsers;
        _departments = departments;
        _branches = branches;
        _employeesService = employeesService;
        _customFieldDefinitions = customFieldDefinitions;
        _hrDocuments = hrDocuments;
        _currencies = currencies;
        _profileUrls = profileUrls;
        _auditLogs = auditLogs;
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
            ?? !string.IsNullOrWhiteSpace(request.Identity.WorkEmail);

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var draft = await _registration.CreateDraftAsync(
                request.Identity.FullName,
                existingUserId: null,
                ct);
            if (!draft.Success || draft.Data is null)
            {
                await RollbackCreateTransactionAsync(transaction, ct);
                return Respons<EmployeeAggregateReadDto>.Fail(
                    draft.Error ?? draft.Detail ?? "Could not create employee.",
                    statusCode: draft.StatusCode);
            }

            var employeeId = draft.Data.Id;
            var wizard = EmployeeAggregateMapper.ToWizardRequest(request);

            var personal = await _registration.UpdatePersonalContactAsync(employeeId, wizard, ct);
            if (!personal.Success)
            {
                await RollbackCreateTransactionAsync(transaction, ct);
                return MapError<EmployeeAggregateReadDto>(personal);
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

            await transaction.CommitAsync(ct);
            await TryRecordEmployeeCreateAuditAsync(employeeId, isFinalised, ct);
            return await GetAsync(employeeId, ct);
        }
        catch (PlatformUserConflictException ex)
        {
            await RollbackCreateTransactionAsync(transaction, ct);
            return Respons<EmployeeAggregateReadDto>.ValidationError(
                new Dictionary<string, string> { [ex.FieldKey] = ex.Message });
        }
        catch (DbUpdateException ex) when (PostgresUniqueViolation.IsCpUserEmail(ex))
        {
            await RollbackCreateTransactionAsync(transaction, ct);
            return Respons<EmployeeAggregateReadDto>.ValidationError(
                new Dictionary<string, string>
                {
                    ["identity.work_email"] = EmployeeErrorMessages.WorkEmailAlreadyRegistered,
                });
        }
        catch (DbUpdateException ex) when (PostgresUniqueViolation.IsCpUserContact(ex))
        {
            await RollbackCreateTransactionAsync(transaction, ct);
            return Respons<EmployeeAggregateReadDto>.ValidationError(
                new Dictionary<string, string>
                {
                    ["identity.phone"] = EmployeeErrorMessages.PhoneAlreadyRegistered,
                });
        }
        catch (DbUpdateConcurrencyException ex)
        {
            await RollbackCreateTransactionAsync(transaction, ct);
            _logger.LogWarning(ex, "Employee create concurrency conflict for tenant {TenantId} org {OrgId}",
                _tenant.TenantId, _tenant.OrgId);
            return Respons<EmployeeAggregateReadDto>.Fail(
                "Could not save employee record. Please retry.", statusCode: 409);
        }
        catch (Exception ex)
        {
            await RollbackCreateTransactionAsync(transaction, ct);
            _logger.LogError(ex, "Employee create failed for tenant {TenantId} org {OrgId}", _tenant.TenantId, _tenant.OrgId);
            throw;
        }
    }

    private async Task RollbackCreateTransactionAsync(
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction,
        CancellationToken ct)
    {
        await transaction.RollbackAsync(ct);
        _db.ChangeTracker.Clear();
    }

    public async Task<Respons<EmployeeAggregateReadDto>> UpdateAsync(
        Guid employeeId, UpdateEmployeeAggregateRequest request, CancellationToken ct = default)
    {
        if (!HasAnyUpdate(request))
        {
            return Respons<EmployeeAggregateReadDto>.ValidationError(new Dictionary<string, string>
            {
                ["body"] = "Include at least one field to update (identity, employment, compensation, education, certifications, documents).",
            });
        }

        var validation = ValidateUpdate(request);
        if (validation is not null)
            return Respons<EmployeeAggregateReadDto>.ValidationError(validation);

        var entityBeforeUpdate = await _employees.GetByIdScopedAsync(employeeId, _tenant.TenantId, _tenant.OrgId, ct);
        if (entityBeforeUpdate is null)
            return Respons<EmployeeAggregateReadDto>.Fail("Employee not found.", statusCode: 404);

        var shouldFinalise = entityBeforeUpdate.IsDraft
            && !string.IsNullOrWhiteSpace(request.Identity?.WorkEmail);

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var wizard = EmployeeAggregateMapper.ToWizardRequest(request);

            if (request.Identity is not null)
            {
                var personal = await _registration.UpdatePersonalContactAsync(employeeId, wizard, ct);
                if (!personal.Success)
                {
                    await transaction.RollbackAsync(ct);
                    return MapError<EmployeeAggregateReadDto>(personal);
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

            await transaction.CommitAsync(ct);
            await TryRecordEmployeeUpdateAuditAsync(employeeId, request, ct);
            return await GetAsync(employeeId, ct);
        }
        catch (PlatformUserConflictException ex)
        {
            await transaction.RollbackAsync(ct);
            return Respons<EmployeeAggregateReadDto>.ValidationError(
                new Dictionary<string, string> { [ex.FieldKey] = ex.Message });
        }
        catch (DbUpdateException ex) when (PostgresUniqueViolation.IsCpUserEmail(ex))
        {
            await transaction.RollbackAsync(ct);
            return Respons<EmployeeAggregateReadDto>.ValidationError(
                new Dictionary<string, string>
                {
                    ["identity.work_email"] = EmployeeErrorMessages.WorkEmailAlreadyRegistered,
                });
        }
        catch (DbUpdateException ex) when (PostgresUniqueViolation.IsCpUserContact(ex))
        {
            await transaction.RollbackAsync(ct);
            return Respons<EmployeeAggregateReadDto>.ValidationError(
                new Dictionary<string, string>
                {
                    ["identity.phone"] = EmployeeErrorMessages.PhoneAlreadyRegistered,
                });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            _logger.LogError(ex, "Employee update failed for {EmployeeId}", employeeId);
            throw;
        }
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

        var fullName = EmployeeIdentityResolver.ResolveFullName(entity, cp);
        var workEmail = EmployeeIdentityResolver.ResolveWorkEmail(entity, cp);
        var storedProfileRef = EmployeeIdentityResolver.ResolveStoredProfileReference(entity, cp);
        var profileUrl = await _profileUrls.ResolveDocumentReadAsync(storedProfileRef, ct);

        var educationItems = education.Success && education.Data is { Count: > 0 } data ? data : null;
        var certificationItems = certifications.Success && certifications.Data is { Count: > 0 } certData
            ? certData
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

        var read = new EmployeeAggregateReadDto
        {
            Id = entity.Id,
            EmployeeCode = entity.EmployeeCode,
            UserId = entity.UserId,
            Identity = EmployeeAggregateReadMapper.BuildIdentity(
                fullName,
                entity,
                cp,
                workEmail,
                profileUrl,
                sections.Identity),
            Employment = EmployeeAggregateReadMapper.BuildEmployment(entity, sections.Employment),
            Compensation = EmployeeAggregateReadMapper.BuildCompensation(
                entity,
                sections.Compensation,
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
            Documents = EmployeeAggregateReadMapper.DocumentsOrNull(documentIds),
        };

        return Respons<EmployeeAggregateReadDto>.Ok(read);
    }

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

        var userIds = rows.Select(r => r.UserId).Where(id => !string.IsNullOrWhiteSpace(id)).Select(id => id!).Distinct();
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
        || request.DeleteEducationIds is { Count: > 0 }
        || request.DeleteCertificationIds is { Count: > 0 }
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

            var doc = await _hrDocuments.GetByIdAsync(documentId, _tenant.TenantId, ct);
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

    private static Dictionary<string, string>? ValidateUpdate(UpdateEmployeeAggregateRequest request)
    {
        var errors = new Dictionary<string, string>();

        if (request.Education is { Count: > MaxEducation })
            errors["education"] = $"At most {MaxEducation} education records allowed per request.";

        if (request.Certifications is { Count: > MaxCertifications })
            errors["certifications"] = $"At most {MaxCertifications} certification records allowed per request.";

        return errors.Count == 0 ? null : errors;
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
