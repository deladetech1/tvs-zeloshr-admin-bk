using ZelosHR.Api.Entities.Branches;
using ZelosHR.Api.Entities.CustomFields;
using ZelosHR.Api.Entities.Departments;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Persistence;
using ZelosHR.Api.Persistence.Entities;
using ZelosHR.Api.Persistence.Repositories;
using ZelosHR.Api.Shared.Abstractions;
using ZelosHR.Api.Shared.Formatting;
using ZelosHR.Api.Shared.Pagination;

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
    private readonly IEmployeeWizardDocumentRepository _documents;
    private readonly ITenantContext _tenant;

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
        IEmployeeWizardDocumentRepository documents,
        ITenantContext tenant)
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
        _documents = documents;
        _tenant = tenant;
    }

    public async Task<Respons<EmployeeAggregateReadDto>> CreateAsync(
        CreateEmployeeAggregateRequest request, CancellationToken ct = default)
    {
        var validation = ValidateCreate(request);
        if (validation is not null)
            return Respons<EmployeeAggregateReadDto>.ValidationError(validation);

        var isFinalised = string.Equals(request.Status, "finalised", StringComparison.OrdinalIgnoreCase);
        if (!isFinalised && !string.Equals(request.Status, "draft", StringComparison.OrdinalIgnoreCase))
        {
            return Respons<EmployeeAggregateReadDto>.ValidationError(new Dictionary<string, string>
            {
                ["status"] = "Status must be 'draft' or 'finalised'.",
            });
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var draft = await _registration.CreateDraftAsync(
                request.Identity.FullName,
                existingUserId: null,
                ct);
            if (!draft.Success || draft.Data is null)
            {
                await transaction.RollbackAsync(ct);
                return Respons<EmployeeAggregateReadDto>.Fail(
                    draft.Error ?? draft.Detail ?? "Could not create employee.",
                    statusCode: draft.StatusCode);
            }

            var employeeId = draft.Data.Id;
            var wizard = EmployeeAggregateMapper.ToWizardRequest(request);

            var personal = await _registration.UpdatePersonalContactAsync(employeeId, wizard, ct);
            if (!personal.Success)
            {
                await transaction.RollbackAsync(ct);
                return MapError<EmployeeAggregateReadDto>(personal);
            }

            if (request.Employment is not null)
            {
                if (request.Employment.DepartmentId is { } deptId
                    && !await _departments.ExistsActiveScopedAsync(deptId, _tenant.TenantId, _tenant.OrgId, ct))
                {
                    await transaction.RollbackAsync(ct);
                    return Respons<EmployeeAggregateReadDto>.ValidationError(
                        new Dictionary<string, string> { ["employment.departmentId"] = "Department not found." });
                }

                if (request.Employment.BranchId is { } branchId
                    && !await _branches.ExistsActiveScopedAsync(branchId, _tenant.TenantId, _tenant.OrgId, ct))
                {
                    await transaction.RollbackAsync(ct);
                    return Respons<EmployeeAggregateReadDto>.ValidationError(
                        new Dictionary<string, string> { ["employment.branchId"] = "Branch not found." });
                }

                var employment = await _registration.UpdateEmploymentDetailsAsync(employeeId, wizard, ct);
                if (!employment.Success)
                {
                    await transaction.RollbackAsync(ct);
                    return MapError<EmployeeAggregateReadDto>(employment);
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
                    await transaction.RollbackAsync(ct);
                    return MapError<EmployeeAggregateReadDto>(added);
                }
            }

            foreach (var cert in request.Certifications)
            {
                var added = await _subResources.AddCertificationAsync(employeeId, cert, ct);
                if (!added.Success)
                {
                    await transaction.RollbackAsync(ct);
                    return MapError<EmployeeAggregateReadDto>(added);
                }
            }

            if (request.Documents is { Count: > 0 })
            {
                var docErrors = await ValidateDocumentReferencesAsync(employeeId, request.Documents, ct);
                if (docErrors is not null)
                {
                    await transaction.RollbackAsync(ct);
                    return Respons<EmployeeAggregateReadDto>.ValidationError(docErrors);
                }
            }

            if (isFinalised)
            {
                var finalised = await _registration.FinaliseAsync(employeeId, ct);
                if (!finalised.Success)
                {
                    await transaction.RollbackAsync(ct);
                    return MapError<EmployeeAggregateReadDto>(finalised);
                }
            }

            await transaction.CommitAsync(ct);
            return await GetAsync(employeeId, ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<Respons<EmployeeAggregateReadDto>> UpdateAsync(
        Guid employeeId, UpdateEmployeeAggregateRequest request, CancellationToken ct = default)
    {
        if (!HasAnyUpdate(request))
        {
            return Respons<EmployeeAggregateReadDto>.ValidationError(new Dictionary<string, string>
            {
                ["body"] = "Include at least one field to update (status, identity, employment, compensation, lifecycle_state, education, certifications, documents).",
            });
        }

        var validation = ValidateUpdate(request);
        if (validation is not null)
            return Respons<EmployeeAggregateReadDto>.ValidationError(validation);

        var shouldFinalise = string.Equals(request.Status, "finalised", StringComparison.OrdinalIgnoreCase);

        if (await _employees.GetByIdScopedAsync(employeeId, _tenant.TenantId, _tenant.OrgId, ct) is null)
            return Respons<EmployeeAggregateReadDto>.Fail("Employee not found.", statusCode: 404);

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
                    var entity = await _employees.GetByIdScopedForUpdateAsync(
                        employeeId, _tenant.TenantId, _tenant.OrgId, ct);
                    if (entity is null)
                    {
                        await transaction.RollbackAsync(ct);
                        return Respons<EmployeeAggregateReadDto>.Fail("Employee not found.", statusCode: 404);
                    }

                    EmployeeRegistrationService.ApplyEmploymentExtras(entity, request.Employment);
                    entity.UpdatedAt = DateTimeOffset.UtcNow;
                    await _employees.UpdateAsync(entity, ct);
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

            if (!string.IsNullOrWhiteSpace(request.LifecycleState))
            {
                var lifecycle = await _employeesService.UpdateLifecycleStateAsync(
                    employeeId, request.LifecycleState, _tenant.TenantId, _tenant.OrgId, ct);
                if (!lifecycle.Success)
                {
                    await transaction.RollbackAsync(ct);
                    return Respons<EmployeeAggregateReadDto>.Fail(
                        lifecycle.Error ?? lifecycle.Detail ?? "Lifecycle update failed.",
                        statusCode: lifecycle.StatusCode);
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

            if (request.Education is { Count: > 0 })
            {
                foreach (var edu in request.Education)
                {
                    var write = EmployeeAggregateMapper.ToEducationWrite(edu);
                    var result = edu.Id is { } id
                        ? await _subResources.UpdateEducationAsync(employeeId, id, write, ct)
                        : await _subResources.AddEducationAsync(employeeId, write, ct);
                    if (!result.Success)
                    {
                        await transaction.RollbackAsync(ct);
                        return MapError<EmployeeAggregateReadDto>(result);
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

            if (request.Certifications is { Count: > 0 })
            {
                foreach (var cert in request.Certifications)
                {
                    var write = EmployeeAggregateMapper.ToCertificationWrite(cert);
                    var result = cert.Id is { } id
                        ? await _subResources.UpdateCertificationAsync(employeeId, id, write, ct)
                        : await _subResources.AddCertificationAsync(employeeId, write, ct);
                    if (!result.Success)
                    {
                        await transaction.RollbackAsync(ct);
                        return MapError<EmployeeAggregateReadDto>(result);
                    }
                }
            }

            if (request.DeleteDocumentIds is { Count: > 0 })
            {
                foreach (var documentId in request.DeleteDocumentIds)
                {
                    var deleted = await _subResources.DeleteDocumentAsync(employeeId, documentId, ct);
                    if (!deleted.Success)
                    {
                        await transaction.RollbackAsync(ct);
                        return Respons<EmployeeAggregateReadDto>.Fail(
                            deleted.Error ?? deleted.Detail ?? "Could not delete document.",
                            statusCode: deleted.StatusCode);
                    }
                }
            }

            if (request.Documents is { Count: > 0 })
            {
                var docErrors = await ValidateDocumentReferencesAsync(employeeId, request.Documents, ct);
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
            return await GetAsync(employeeId, ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
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
        var uploadedDocuments = await _subResources.ListDocumentsAsync(id, category: null, ct);

        var fullName = EmployeeIdentityResolver.ResolveFullName(entity, cp);
        var workEmail = EmployeeIdentityResolver.ResolveWorkEmail(entity, cp);
        var profilePhoto = EmployeeIdentityResolver.ResolveProfilePhoto(entity, cp);

        var educationItems = education.Success && education.Data is not null ? education.Data : [];
        var certificationItems = certifications.Success && certifications.Data is not null ? certifications.Data : [];
        var documentIds = uploadedDocuments.Success && uploadedDocuments.Data is not null
            ? EmployeeAggregateMapper.ToDocumentIds(uploadedDocuments.Data)
            : Array.Empty<Guid>();

        var definitions = await _customFieldDefinitions.ListSchemaScopedAsync(
            _tenant.TenantId, _tenant.OrgId, CustomFieldEntityTypes.Employee, ct);
        var sections = EmployeeCustomFieldMapper.SplitFromFlat(
            EmployeeAggregateMapper.DeserializeCustomFields(entity.CustomFieldsData),
            definitions);

        var read = new EmployeeAggregateReadDto
        {
            Id = entity.Id,
            EmployeeCode = entity.EmployeeCode,
            Status = entity.IsDraft ? "draft" : entity.LifecycleStatus,
            IsDraft = entity.IsDraft,
            UserId = entity.UserId,
            ProfileUrl = profilePhoto,
            Identity = new EmployeeAggregateIdentityDto
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
                CustomFields = sections.Identity,
            },
            Employment = new EmployeeAggregateEmploymentReadDto
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
                CustomFields = sections.Employment,
            },
            Compensation = new EmployeeAggregateCompensationReadDto
            {
                GrossSalary = entity.GrossSalary,
                PayFrequency = entity.PayFrequency,
                Currency = entity.Currency,
                AnnualizedCost = entity.AnnualizedCost,
                CustomFields = sections.Compensation,
            },
            Education = educationItems
                .Select(e => e with { CustomFields = sections.Education })
                .ToList(),
            Certifications = certificationItems
                .Select(c => c with { CustomFields = sections.Certification })
                .ToList(),
            Documents = documentIds,
        };

        return Respons<EmployeeAggregateReadDto>.Ok(read);
    }

    public async Task<Respons<EmployeeListDto>> ListAsync(
        EmployeeListQuery query, CancellationToken ct = default)
    {
        var paging = PagedQuery.From(query.Page, query.Size);
        var (rows, total) = await _employees.ListScopedAsync(
            query, _tenant.TenantId, _tenant.OrgId, paging.Page, paging.Size, ct);

        var userIds = rows.Select(r => r.UserId).Where(id => !string.IsNullOrWhiteSpace(id)).Select(id => id!).Distinct();
        var platformUsers = await _cpUsers.GetByIdsAsync(userIds, _tenant.TenantId, ct);

        var items = rows.Select(e =>
        {
            platformUsers.TryGetValue(e.UserId ?? string.Empty, out var cp);
            var profileUrl = EmployeeIdentityResolver.ResolveProfilePhoto(e, cp);
            return new EmployeeListItemDto
            {
                EmployeeId = e.Id.ToString(),
                EmployeeCode = e.EmployeeCode,
                FullName = EmployeeIdentityResolver.ResolveFullName(e, cp),
                JobTitle = e.JobTitle,
                DepartmentName = e.Department?.Name,
                BranchName = e.Branch?.Name,
                WorkLocation = e.WorkLocation,
                LifecycleState = e.LifecycleState,
                EmploymentStatus = e.EmploymentStatus,
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

    private static Dictionary<string, string>? ValidateCreate(CreateEmployeeAggregateRequest request)
    {
        var errors = new Dictionary<string, string>();

        if (string.IsNullOrWhiteSpace(request.Identity.FullName))
            errors["identity.full_name"] = "Full name is required.";

        if (request.Education.Count > MaxEducation)
            errors["education"] = $"At most {MaxEducation} education records allowed.";

        if (request.Certifications.Count > MaxCertifications)
            errors["certifications"] = $"At most {MaxCertifications} certification records allowed.";

        return errors.Count == 0 ? null : errors;
    }

    private static bool HasAnyUpdate(UpdateEmployeeAggregateRequest request) =>
        !string.IsNullOrWhiteSpace(request.Status)
        || request.Identity is not null
        || request.Employment is not null
        || request.Compensation is not null
        || !string.IsNullOrWhiteSpace(request.LifecycleState)
        || request.Education is { Count: > 0 }
        || request.Certifications is { Count: > 0 }
        || request.DeleteEducationIds is { Count: > 0 }
        || request.DeleteCertificationIds is { Count: > 0 }
        || request.DeleteDocumentIds is { Count: > 0 }
        || request.Documents is { Count: > 0 }
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

    private async Task<Dictionary<string, string>?> ValidateDocumentReferencesAsync(
        Guid employeeId,
        IReadOnlyList<Guid> documentIds,
        CancellationToken ct)
    {
        var errors = new Dictionary<string, string>();

        for (var i = 0; i < documentIds.Count; i++)
        {
            var documentId = documentIds[i];
            if (documentId == Guid.Empty)
            {
                errors[$"documents[{i}]"] = "Document id is required.";
                continue;
            }

            var doc = await _documents.GetByIdAsync(
                documentId, employeeId, _tenant.TenantId, _tenant.OrgId, ct);
            if (doc is null)
                errors[$"documents[{i}]"] = "Document not found for this employee.";
        }

        return errors.Count == 0 ? null : errors;
    }

    private static Dictionary<string, string>? ValidateUpdate(UpdateEmployeeAggregateRequest request)
    {
        var errors = new Dictionary<string, string>();

        if (!string.IsNullOrWhiteSpace(request.Status)
            && !string.Equals(request.Status, "finalised", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(request.Status, "draft", StringComparison.OrdinalIgnoreCase))
        {
            errors["status"] = "Status must be 'draft' or 'finalised'.";
        }

        if (request.Education is { Count: > MaxEducation })
            errors["education"] = $"At most {MaxEducation} education records allowed per request.";

        if (request.Certifications is { Count: > MaxCertifications })
            errors["certifications"] = $"At most {MaxCertifications} certification records allowed per request.";

        return errors.Count == 0 ? null : errors;
    }

    private static Respons<T> MapError<T>(Respons<EmployeeRegistrationReadDto> source) =>
        Fail<T>(source.StatusCode, source.Error, source.Detail);

    private static Respons<T> MapError<T>(Respons<EmployeeEducationDto> source) =>
        Fail<T>(source.StatusCode, source.Error, source.Detail);

    private static Respons<T> MapError<T>(Respons<EmployeeCertificationDto> source) =>
        Fail<T>(source.StatusCode, source.Error, source.Detail);

    private static Respons<T> Fail<T>(int statusCode, string? error, string? detail) =>
        Respons<T>.Fail(error ?? detail ?? "Request failed.", statusCode: statusCode);

    private static DateOnly? ParseCpDob(string? dob) =>
        DateOnly.TryParse(dob, out var parsed) ? parsed : null;
}
