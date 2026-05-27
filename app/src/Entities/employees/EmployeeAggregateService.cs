using ZelosHR.Api.Entities.Branches;
using ZelosHR.Api.Entities.Departments;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Persistence;
using ZelosHR.Api.Persistence.Entities;
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
                request.Import?.ExistingUserId,
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

            if (request.CustomFields is { Count: > 0 })
            {
                var entity = await _employees.GetByIdScopedForUpdateAsync(
                    employeeId, _tenant.TenantId, _tenant.OrgId, ct);
                if (entity is null)
                {
                    await transaction.RollbackAsync(ct);
                    return Respons<EmployeeAggregateReadDto>.Fail("Employee not found.", statusCode: 404);
                }

                entity.CustomFieldsData = EmployeeAggregateMapper.SerializeCustomFields(request.CustomFields);
                await _employees.UpdateAsync(entity, ct);
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
                ["body"] = "Include at least one section to update (identity, employment, compensation, lifecycle_state, education, certifications, custom_fields).",
            });
        }

        var validation = ValidateUpdate(request);
        if (validation is not null)
            return Respons<EmployeeAggregateReadDto>.ValidationError(validation);

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

            if (request.CustomFields is { Count: > 0 })
            {
                var entity = await _employees.GetByIdScopedForUpdateAsync(
                    employeeId, _tenant.TenantId, _tenant.OrgId, ct);
                if (entity is null)
                {
                    await transaction.RollbackAsync(ct);
                    return Respons<EmployeeAggregateReadDto>.Fail("Employee not found.", statusCode: 404);
                }

                var merged = EmployeeAggregateMapper.DeserializeCustomFields(entity.CustomFieldsData);
                foreach (var (key, value) in request.CustomFields)
                    merged[key] = value;
                entity.CustomFieldsData = EmployeeAggregateMapper.SerializeCustomFields(merged);
                entity.UpdatedAt = DateTimeOffset.UtcNow;
                await _employees.UpdateAsync(entity, ct);
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

        var fullName = EmployeeIdentityResolver.ResolveFullName(entity, cp);
        var workEmail = EmployeeIdentityResolver.ResolveWorkEmail(entity, cp);
        var profilePhoto = EmployeeIdentityResolver.ResolveProfilePhoto(entity, cp);

        var read = new EmployeeAggregateReadDto
        {
            Id = entity.Id,
            EmployeeCode = entity.EmployeeCode,
            Status = entity.IsDraft ? "draft" : entity.LifecycleStatus,
            IsDraft = entity.IsDraft,
            UserId = entity.UserId,
            ProfilePhotoUrl = profilePhoto,
            Identity = new EmployeeAggregateIdentityDto
            {
                FullName = fullName,
                DateOfBirth = entity.DateOfBirth ?? ParseCpDob(cp?.Dob),
                Gender = entity.Gender ?? cp?.Gender,
                Nationality = entity.Nationality,
                NationalityIdType = entity.NationalityIdType,
                IdNumber = entity.IdNumber,
                PersonalEmail = entity.PersonalEmail,
                WorkEmail = workEmail,
                Phone = entity.Phone ?? entity.PersonalPhone ?? cp?.Phone,
                LinkedInUrl = entity.LinkedInUrl,
                ResidentialAddress = entity.ResidentialAddress ?? cp?.Address,
                GpsAddress = entity.GhanaPostGps,
                State = entity.State,
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
            },
            Compensation = new EmployeeAggregateCompensationReadDto
            {
                GrossSalary = entity.GrossSalary,
                PayFrequency = entity.PayFrequency,
                SalaryEffectiveFrom = entity.SalaryEffectiveFrom,
                Currency = entity.Currency,
                SsnitNumber = entity.SsnitNumber,
                TinNumber = entity.TinNumber,
                Tier2PensionProvider = entity.Tier2PensionProvider,
                Tier3PensionProvider = entity.Tier3PensionProvider,
                PaymentMethod = entity.PaymentMethod,
                BankAccountNumber = entity.BankAccountNumber,
                MobileMoneyNumber = entity.MobileMoneyNumber,
                AnnualizedCost = entity.AnnualizedCost,
                MaskedSsnitNumber = EmployeeRegistrationService.MaskSensitive(entity.SsnitNumber),
                MaskedTinNumber = EmployeeRegistrationService.MaskSensitive(entity.TinNumber),
            },
            Education = education.Success && education.Data is not null ? education.Data : [],
            Certifications = certifications.Success && certifications.Data is not null ? certifications.Data : [],
            CustomFields = EmployeeAggregateMapper.DeserializeCustomFields(entity.CustomFieldsData),
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

        if (string.IsNullOrWhiteSpace(request.Identity.FullName)
            && string.IsNullOrWhiteSpace(request.Import?.ExistingUserId))
            errors["identity.fullName"] = "Full name is required unless importing an existing user.";

        if (request.Education.Count > MaxEducation)
            errors["education"] = $"At most {MaxEducation} education records allowed.";

        if (request.Certifications.Count > MaxCertifications)
            errors["certifications"] = $"At most {MaxCertifications} certification records allowed.";

        return errors.Count == 0 ? null : errors;
    }

    private static bool HasAnyUpdate(UpdateEmployeeAggregateRequest request) =>
        request.Identity is not null
        || request.Employment is not null
        || request.Compensation is not null
        || !string.IsNullOrWhiteSpace(request.LifecycleState)
        || request.Education is { Count: > 0 }
        || request.Certifications is { Count: > 0 }
        || request.CustomFields is { Count: > 0 }
        || request.DeleteEducationIds is { Count: > 0 }
        || request.DeleteCertificationIds is { Count: > 0 };

    private static Dictionary<string, string>? ValidateUpdate(UpdateEmployeeAggregateRequest request)
    {
        var errors = new Dictionary<string, string>();

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
