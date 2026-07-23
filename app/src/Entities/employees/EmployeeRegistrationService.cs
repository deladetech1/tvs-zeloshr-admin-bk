using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using ZelosHR.Api.Entities.EmployeeIdFormat;
using ZelosHR.Api.Entities.EmploymentTypes;
using ZelosHR.Api.Entities.Files;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Persistence;
using ZelosHR.Api.Persistence.Entities;
using ZelosHR.Api.Persistence.Repositories;
using ZelosHR.Api.Shared.Abstractions;
using ZelosHR.Api.Shared.Formatting;
using ZelosHR.Api.Shared.Infrastructure;
using ZelosHR.Api.Shared.Validation;

namespace ZelosHR.Api.Entities.Employees;

public sealed class EmployeeRegistrationService
{
    private readonly ZelosHrDbContext _db;
    private readonly IEmployeeRepository _employees;
    private readonly ICpUserRepository _cpUsers;
    private readonly ICpCurrencyRepository _currencies;
    private readonly IEmployeeDocumentBlobStorage _blobs;
    private readonly IHrDocumentPathRepository _documents;
    private readonly FileManagementStorage _storageConfig;
    private readonly HrDocumentPresignedUrlService _profileUrls;
    private readonly EmploymentTypesService _employmentTypes;
    private readonly EmployeeCodeGenerationService _codeGen;
    private readonly ITenantContext _tenant;
    private readonly ICurrentUserService _currentUser;

    public EmployeeRegistrationService(
        ZelosHrDbContext db,
        IEmployeeRepository employees,
        ICpUserRepository cpUsers,
        ICpCurrencyRepository currencies,
        IEmployeeDocumentBlobStorage blobs,
        IHrDocumentPathRepository documents,
        FileManagementStorage storageConfig,
        HrDocumentPresignedUrlService profileUrls,
        EmploymentTypesService employmentTypes,
        EmployeeCodeGenerationService codeGen,
        ITenantContext tenant,
        ICurrentUserService currentUser)
    {
        _db = db;
        _employees = employees;
        _cpUsers = cpUsers;
        _currencies = currencies;
        _blobs = blobs;
        _documents = documents;
        _storageConfig = storageConfig;
        _profileUrls = profileUrls;
        _employmentTypes = employmentTypes;
        _codeGen = codeGen;
        _tenant = tenant;
        _currentUser = currentUser;
    }

    public async Task<Respons<CpUserCheckResult>> CheckCpUserAsync(string email, CancellationToken ct = default)
    {
        var user = await _cpUsers.FindByEmailAsync(email, _tenant.TenantId, ct);
        if (user is null)
            return Respons<CpUserCheckResult>.Ok(new CpUserCheckResult(false, null, null, false));

        var linked = await _cpUsers.IsLinkedToEmployeeAsync(user.Id, _tenant.TenantId, ct);
        return Respons<CpUserCheckResult>.Ok(new CpUserCheckResult(true, user.Id, user.FullName, !linked));
    }

    public async Task<Respons<IReadOnlyList<CpUserDto>>> ImportSearchAsync(string query, CancellationToken ct = default)
    {
        var users = await _cpUsers.SearchAsync(query, _tenant.TenantId, limit: 20, ct);
        var filtered = new List<CpUserDto>();
        foreach (var u in users)
        {
            if (!await _cpUsers.IsLinkedToEmployeeAsync(u.Id, _tenant.TenantId, ct))
                filtered.Add(u);
        }

        return Respons<IReadOnlyList<CpUserDto>>.Ok(filtered);
    }

    public async Task<Respons<EmployeeRegistrationReadDto>> CreateDraftAsync(
        string fullName, string? existingUserId, string? employeeCode = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(fullName) && string.IsNullOrWhiteSpace(existingUserId))
            return Respons<EmployeeRegistrationReadDto>.ValidationError(
                new Dictionary<string, string> { ["fullName"] = "Full name is required." });

        string? userId = null;
        var draftDisplayName = string.Empty;

        if (!string.IsNullOrWhiteSpace(existingUserId))
        {
            var cp = await _cpUsers.GetByIdAsync(existingUserId.Trim(), _tenant.TenantId, ct);
            if (cp is null)
                return Respons<EmployeeRegistrationReadDto>.ValidationError(
                    new Dictionary<string, string> { ["existingUserId"] = "User not found in platform." });

            if (await _cpUsers.IsLinkedToEmployeeAsync(cp.Id, _tenant.TenantId, ct))
            {
                return Respons<EmployeeRegistrationReadDto>.Fail(
                    EmployeeErrorMessages.UserAlreadyLinkedToEmployee, statusCode: 409);
            }

            userId = cp.Id;
            draftDisplayName = cp.FullName?.Trim() ?? string.Empty;
        }
        else
        {
            draftDisplayName = fullName?.Trim() ?? string.Empty;
        }

        var now = DateTimeOffset.UtcNow;
        var entity = NewDraftEntity(userId, draftDisplayName, now);

        var actorUserId = _currentUser.UserId?.ToString();
        var planned = await _codeGen.PlanAllocationAsync(
            _tenant.TenantId, _tenant.OrgId, employeeCode, actorUserId, ct);
        if (!planned.Success)
            return Respons<EmployeeRegistrationReadDto>.ValidationError(planned.Errors!);

        var plan = planned.Plan!;
        var maxAttempts = plan.UseRetry ? EmployeeIdFormatRules.MaxAttempts : 1;
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            entity.EmployeeCode = plan.UseRetry
                ? _codeGen.FormatCode(plan.Format, plan.StartSequence + attempt)
                : plan.InitialCode;
            var savepoint = $"draft_code_{attempt}";
            var transaction = _db.Database.CurrentTransaction;
            if (transaction is not null)
                await transaction.CreateSavepointAsync(savepoint, ct);

            try
            {
                await _employees.AddAsync(entity, ct);
                return Respons<EmployeeRegistrationReadDto>.Ok(await ToReadDtoAsync(entity, ct));
            }
            catch (DbUpdateException ex) when (PostgresUniqueViolation.IsEmployeeUserId(ex))
            {
                if (transaction is not null)
                    await transaction.RollbackToSavepointAsync(savepoint, ct);
                _db.Entry(entity).State = EntityState.Detached;
                return Respons<EmployeeRegistrationReadDto>.Fail(
                    EmployeeErrorMessages.UserAlreadyLinkedToEmployee, statusCode: 409);
            }
            catch (DbUpdateException ex) when (PostgresUniqueViolation.IsEmployeeCode(ex))
            {
                if (transaction is not null)
                    await transaction.RollbackToSavepointAsync(savepoint, ct);
                _db.Entry(entity).State = EntityState.Detached;
                entity = NewDraftEntity(userId, draftDisplayName, now);

                if (attempt == maxAttempts - 1)
                {
                    return Respons<EmployeeRegistrationReadDto>.Fail(
                        EmployeeErrorMessages.EmployeeCodeAllocationFailed, statusCode: 409);
                }
            }
        }

        return Respons<EmployeeRegistrationReadDto>.Fail(
            EmployeeErrorMessages.EmployeeCodeAllocationFailed, statusCode: 409);
    }

    public Task<Respons<EmployeeRegistrationReadDto>> ImportAsync(string userId, CancellationToken ct = default) =>
        CreateDraftAsync(string.Empty, userId, ct);

    public async Task<Respons<ImportEmployeesResult>> ImportManyAsync(
        IReadOnlyList<string> userIds,
        CancellationToken ct = default)
    {
        if (userIds is not { Count: > 0 })
        {
            return Respons<ImportEmployeesResult>.ValidationError(
                new Dictionary<string, string> { ["user_ids"] = "At least one user_id is required." });
        }

        var rows = new List<ImportEmployeeRowResult>();
        foreach (var userId in userIds.Where(id => !string.IsNullOrWhiteSpace(id)).Select(id => id.Trim()).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            ImportEmployeeRowResult rowResult;
            try
            {
                var imported = await ImportAsync(userId, ct);
                rowResult = new ImportEmployeeRowResult
                {
                    UserId = userId,
                    Success = imported.Success,
                    EmployeeId = imported.Data?.Id,
                    EmployeeCode = imported.Data?.EmployeeCode,
                    FullName = imported.Data?.FullName,
                    Error = imported.Success ? null : imported.Error ?? imported.Detail,
                };
            }
            catch (DbUpdateException ex)
            {
                _db.ChangeTracker.Clear();
                rowResult = new ImportEmployeeRowResult
                {
                    UserId = userId,
                    Success = false,
                    Error = PostgresUniqueViolation.IsEmployeeUserId(ex)
                        ? EmployeeErrorMessages.UserAlreadyLinkedToEmployee
                        : "Could not import user. Please retry.",
                };
            }

            rows.Add(rowResult);
        }

        if (rows.Count == 0)
        {
            return Respons<ImportEmployeesResult>.ValidationError(
                new Dictionary<string, string> { ["user_ids"] = "At least one non-empty user_id is required." });
        }

        var successCount = rows.Count(r => r.Success);
        var result = new ImportEmployeesResult
        {
            Items = rows,
            SuccessCount = successCount,
            FailureCount = rows.Count - successCount,
        };
        var failureMessages = rows.Where(r => !r.Success).Select(r => r.Error).ToList();
        return BatchResultResponse.FromRowCounts(
            result, successCount, result.FailureCount, "Import", failureMessages);
    }

    public async Task<Respons<EmployeeRegistrationReadDto>> UpdatePersonalContactAsync(
        Guid id, CreateEmployeeRequest dto, CancellationToken ct = default)
    {
        var entity = await LoadDraftAsync(id, ct);
        if (entity.Error is not null)
            return entity.Error;

        var identityErrors = EmployeeIdentityFieldValidator.ValidateRegistrationPatch(dto);
        if (identityErrors is not null)
            return Respons<EmployeeRegistrationReadDto>.ValidationError(identityErrors);

        var e = entity.Value!;
        ApplyHrPersonalFields(e, dto);

        var syncDto = dto;
        if (dto.ProfileUrl is not null)
        {
            CpUserDto? cp = null;
            if (!string.IsNullOrWhiteSpace(e.UserId))
                cp = await _cpUsers.GetByIdAsync(e.UserId, _tenant.TenantId, ct);

            var storedProfileRef = EmployeeIdentityResolver.ResolveStoredProfileReference(e, cp);
            var resolved = await _profileUrls.ResolveProfileUrlForWriteAsync(
                "identity.profile_url", dto.ProfileUrl, storedProfileRef, ct);
            if (resolved.Error is not null)
                return Respons<EmployeeRegistrationReadDto>.ValidationError(resolved.Error);

            if (resolved.ShouldApply)
                ApplyProfileUrlValue(e, resolved.Value);

            syncDto = resolved.ShouldApply
                ? dto with { ProfileUrl = resolved.Value }
                : dto with { ProfileUrl = null };
        }

        var syncError = await SyncPlatformIdentityAsync(e, syncDto, ct);
        if (syncError is not null)
            return syncError;

        if (string.IsNullOrWhiteSpace(e.UserId))
            StageIdentityOnEmployeeUntilEmail(e, dto);

        e.UpdatedAt = DateTimeOffset.UtcNow;
        e.UpdatedBy = _currentUser.UserId?.ToString();
        await _employees.UpdateAsync(e, ct);
        return Respons<EmployeeRegistrationReadDto>.Ok(await ToReadDtoAsync(e, ct));
    }

    public async Task<Respons<EmployeeRegistrationReadDto>> UpdateEmploymentDetailsAsync(
        Guid id, CreateEmployeeRequest dto, CancellationToken ct = default)
    {
        var entity = await LoadDraftAsync(id, ct);
        if (entity.Error is not null)
            return entity.Error;

        if (dto.ReportsToId == id)
            return Respons<EmployeeRegistrationReadDto>.ValidationError(
                new Dictionary<string, string> { ["reportsToId"] = "Employee cannot report to themselves." });

        ApplyEmployment(entity.Value!, dto);
        var typeError = await ApplyEmploymentTypeAsync(entity.Value!, dto, ct);
        if (typeError is not null)
            return typeError;

        entity.Value!.UpdatedAt = DateTimeOffset.UtcNow;
        await _employees.UpdateAsync(entity.Value, ct);
        return Respons<EmployeeRegistrationReadDto>.Ok(await ToReadDtoAsync(entity.Value, ct));
    }

    public async Task<Respons<EmployeeRegistrationReadDto>> UpdateCompensationAsync(
        Guid id, CreateEmployeeRequest dto, CancellationToken ct = default)
    {
        var entity = await LoadDraftAsync(id, ct);
        if (entity.Error is not null)
            return entity.Error;

        var currencyError = await ResolveCurrencyIdAsync(entity.Value!, dto, ct);
        if (currencyError is not null)
            return Respons<EmployeeRegistrationReadDto>.ValidationError(currencyError);

        ApplyCompensation(entity.Value!, dto);
        entity.Value!.UpdatedAt = DateTimeOffset.UtcNow;
        await _employees.UpdateAsync(entity.Value, ct);
        return Respons<EmployeeRegistrationReadDto>.Ok(await ToReadDtoAsync(entity.Value, ct));
    }

    public async Task<Respons<EmployeeRegistrationReadDto>> FinaliseAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await LoadDraftAsync(id, ct);
        if (entity.Error is not null)
            return entity.Error;

        var e = entity.Value!;
        var displayName = await ResolveDraftDisplayNameAsync(e, ct);
        if (string.IsNullOrWhiteSpace(displayName))
            return Respons<EmployeeRegistrationReadDto>.ValidationError(
                new Dictionary<string, string> { ["identity.full_name"] = "Full name is required." });

        var branchRule = WorkArrangementRules.ValidateBranchForArrangement(e.WorkArrangement, e.BranchId);
        if (branchRule is not null)
            return Respons<EmployeeRegistrationReadDto>.ValidationError(branchRule);

        var userLink = await ResolvePlatformUserForFinaliseAsync(e, displayName, ct);
        if (userLink.Error is not null)
            return userLink.Error;

        if (string.IsNullOrWhiteSpace(e.EmploymentStatus))
        {
            return Respons<EmployeeRegistrationReadDto>.ValidationError(
                new Dictionary<string, string>
                {
                    ["employment.employment_status"] =
                        "employment_status is required before finalising an employee.",
                });
        }

        e.UserId = userLink.UserId;
        ClearCpUserIdentityFromEmployee(e);
        e.IsDraft = false;
        EmployeeLifecycleSync.SyncFromEmploymentStatus(e, e.EmploymentStatus);
        e.UpdatedAt = DateTimeOffset.UtcNow;
        e.UpdatedBy = _currentUser.UserId?.ToString();
        await _employees.UpdateAsync(e, ct);
        return Respons<EmployeeRegistrationReadDto>.Ok(await ToReadDtoAsync(e, ct));
    }

    /// <summary>
    /// Writes identity to cp_users when work email is available; links employee via user_id + tenant_id.
    /// </summary>
    private async Task<Respons<EmployeeRegistrationReadDto>?> SyncPlatformIdentityAsync(
        EmployeeEntity e, CreateEmployeeRequest dto, CancellationToken ct)
    {
        var identity = CpUserIdentityMapper.FromEmployeeAndRequest(e, dto);
        var createdBy = _currentUser.UserId?.ToString();

        if (!string.IsNullOrWhiteSpace(e.UserId))
        {
            var syncExisting = await RunPlatformUserAsync(
                async innerCt =>
                {
                    await _cpUsers.UpdateIdentityAsync(e.UserId, _tenant.TenantId, identity, innerCt);
                }, ct);
            if (syncExisting is not null)
                return syncExisting;

            await _cpUsers.EnsureUserLocationAsync(
                e.UserId, _tenant.TenantId, _tenant.OrgId, _tenant.BusId, _tenant.LocId, ct);
            ClearCpUserIdentityFromEmployee(e);
            return null;
        }

        if (string.IsNullOrWhiteSpace(identity.Email))
            return null;

        if (string.IsNullOrWhiteSpace(identity.FullName))
            return Respons<EmployeeRegistrationReadDto>.ValidationError(
                new Dictionary<string, string> { ["fullName"] = "Full name is required." });

        var existing = await _cpUsers.FindByEmailAsync(identity.Email, _tenant.TenantId, ct);
        if (existing is not null)
        {
            if (await _cpUsers.IsLinkedToEmployeeAsync(existing.Id, _tenant.TenantId, ct))
            {
                return Respons<EmployeeRegistrationReadDto>.ValidationError(
                    new Dictionary<string, string>
                    {
                        ["identity.work_email"] = EmployeeErrorMessages.WorkEmailLinkedToAnotherEmployee,
                    });
            }

            e.UserId = existing.Id;
            var linkExisting = await RunPlatformUserAsync(async ct =>
            {
                await _cpUsers.UpdateIdentityAsync(existing.Id, _tenant.TenantId, identity, ct);
                await _cpUsers.EnsureHrMembershipAsync(existing.Id, _tenant.TenantId, createdBy, ct);
                await _cpUsers.EnsureUserLocationAsync(
                    existing.Id, _tenant.TenantId, _tenant.OrgId, _tenant.BusId, _tenant.LocId, ct);
            }, ct);
            if (linkExisting is not null)
                return linkExisting;
        }
        else
        {
            if (await _cpUsers.FindEmailOwnerAsync(identity.Email, ct) is not null)
            {
                return Respons<EmployeeRegistrationReadDto>.ValidationError(
                    new Dictionary<string, string>
                    {
                        ["identity.work_email"] = EmployeeErrorMessages.WorkEmailUsedByAnotherOrganisation,
                    });
            }

            var provisioned = await RunPlatformUserAsync(
                ct => _cpUsers.ProvisionEmployeeUserAsync(ToProvisionRequest(identity, createdBy), ct), ct);
            if (provisioned.Error is not null)
                return provisioned.Error;

            e.UserId = provisioned.Value!.Id;
        }

        ClearCpUserIdentityFromEmployee(e);
        return null;
    }

    private async Task<(string? UserId, Respons<EmployeeRegistrationReadDto>? Error)> ResolvePlatformUserForFinaliseAsync(
        EmployeeEntity employee, string displayName, CancellationToken ct)
    {
        var createdBy = _currentUser.UserId?.ToString();
        var identity = CpUserIdentityMapper.FromEmployeeAndRequest(employee);
        if (string.IsNullOrWhiteSpace(identity.FullName))
            identity = identity with { FullName = displayName };

        if (!string.IsNullOrWhiteSpace(employee.UserId))
        {
            var updateExisting = await RunPlatformUserAsync(async ct =>
            {
                await _cpUsers.UpdateIdentityAsync(employee.UserId, _tenant.TenantId, identity, ct);
                await _cpUsers.EnsureHrMembershipAsync(employee.UserId, _tenant.TenantId, createdBy, ct);
                await _cpUsers.EnsureUserLocationAsync(
                    employee.UserId, _tenant.TenantId, _tenant.OrgId, _tenant.BusId, _tenant.LocId, ct);
            }, ct);
            if (updateExisting is not null)
                return (null, updateExisting);

            return (employee.UserId, null);
        }

        var workEmail = identity.Email;
        if (string.IsNullOrWhiteSpace(workEmail))
        {
            return (null, Respons<EmployeeRegistrationReadDto>.ValidationError(
                new Dictionary<string, string>
                {
                    ["workEmail"] = "Work email is required to create or link a platform user.",
                }));
        }

        var existing = await _cpUsers.FindByEmailAsync(workEmail, _tenant.TenantId, ct);
        if (existing is not null)
        {
            if (await _cpUsers.IsLinkedToEmployeeAsync(existing.Id, _tenant.TenantId, ct))
            {
                return (null, Respons<EmployeeRegistrationReadDto>.ValidationError(
                    new Dictionary<string, string>
                    {
                        ["identity.work_email"] = EmployeeErrorMessages.WorkEmailLinkedToAnotherEmployee,
                    }));
            }

            var linkByEmail = await RunPlatformUserAsync(async ct =>
            {
                await _cpUsers.UpdateIdentityAsync(existing.Id, _tenant.TenantId, identity, ct);
                await _cpUsers.EnsureHrMembershipAsync(existing.Id, _tenant.TenantId, createdBy, ct);
                await _cpUsers.EnsureUserLocationAsync(
                    existing.Id, _tenant.TenantId, _tenant.OrgId, _tenant.BusId, _tenant.LocId, ct);
            }, ct);
            if (linkByEmail is not null)
                return (null, linkByEmail);

            return (existing.Id, null);
        }

        var provisioned = await RunPlatformUserAsync(
            ct => _cpUsers.ProvisionEmployeeUserAsync(ToProvisionRequest(identity, createdBy), ct), ct);
        if (provisioned.Error is not null)
            return (null, provisioned.Error);

        return (provisioned.Value!.Id, null);
    }

    private static async Task<Respons<EmployeeRegistrationReadDto>?> RunPlatformUserAsync(
        Func<CancellationToken, Task> action, CancellationToken ct)
    {
        try
        {
            await action(ct);
            return null;
        }
        catch (PlatformUserConflictException ex)
        {
            return ToPlatformUserValidationError(ex);
        }
    }

    private static async Task<(CpUserDto? Value, Respons<EmployeeRegistrationReadDto>? Error)> RunPlatformUserAsync(
        Func<CancellationToken, Task<CpUserDto>> action, CancellationToken ct)
    {
        try
        {
            return (await action(ct), null);
        }
        catch (PlatformUserConflictException ex)
        {
            return (null, ToPlatformUserValidationError(ex));
        }
    }

    private static Respons<EmployeeRegistrationReadDto> ToPlatformUserValidationError(
        PlatformUserConflictException ex) =>
        Respons<EmployeeRegistrationReadDto>.ValidationError(
            new Dictionary<string, string> { [ex.FieldKey] = ex.Message });

    private ProvisionCpUserRequest ToProvisionRequest(CpUserIdentityData identity, string? createdBy) =>
        new(
            _tenant.TenantId,
            _tenant.OrgId,
            _tenant.BusId,
            _tenant.LocId,
            identity.FullName,
            identity.Email,
            identity.Contact,
            identity.Gender,
            identity.Dob,
            identity.Address,
            identity.ProfilePic,
            createdBy);

    /// <summary>Identity lives in cp_users; remove duplicated columns from zhr_employees after link.</summary>
    private static void ClearCpUserIdentityFromEmployee(EmployeeEntity e)
    {
        e.FullName = string.Empty;
        e.FirstName = null;
        e.MiddleName = null;
        e.LastName = null;
        e.DateOfBirth = null;
        e.Gender = null;
        e.WorkEmail = null;
        e.Phone = null;
        e.PersonalPhone = null;
        e.ResidentialAddress = null;
        e.GhanaPostGps = null;
        e.State = null;
        e.ProfilePhotoUrl = null;
    }

    /// <summary>Temporary staging on zhr_employees until work email enables cp_users sync.</summary>
    private static void StageIdentityOnEmployeeUntilEmail(EmployeeEntity e, CreateEmployeeRequest dto)
    {
        if (!string.IsNullOrWhiteSpace(dto.FullName))
            e.FullName = dto.FullName.Trim();
        e.DateOfBirth = dto.DateOfBirth ?? e.DateOfBirth;
        e.Gender = dto.Gender ?? e.Gender;
        e.Phone = dto.Phone ?? e.Phone;
        e.WorkEmail = dto.WorkEmail ?? e.WorkEmail;
        e.ResidentialAddress = dto.ResidentialAddress ?? e.ResidentialAddress;
    }

    private static void ApplyHrPersonalFields(EmployeeEntity e, CreateEmployeeRequest dto)
    {
        e.Nationality = dto.Country ?? e.Nationality;
        e.NationalityIdType = dto.IdType ?? e.NationalityIdType;
        e.IdIssueDate = dto.IdIssueDate ?? e.IdIssueDate;
        e.IdExpiryDate = dto.IdExpiryDate ?? e.IdExpiryDate;
        e.IdNumber = dto.IdNumber ?? e.IdNumber;
        e.MaritalStatus = dto.MaritalStatus ?? e.MaritalStatus;
        e.NextOfKinName = dto.NextOfKinName ?? e.NextOfKinName;
        e.NextOfKinPhone = dto.NextOfKinPhone ?? e.NextOfKinPhone;
        e.RelationshipToNextOfKin = dto.RelationshipToNextOfKin ?? e.RelationshipToNextOfKin;
        e.PersonalEmail = dto.PersonalEmail ?? e.PersonalEmail;
        e.LinkedInUrl = dto.LinkedInUrl ?? e.LinkedInUrl;
    }

    private static void ApplyProfileUrlValue(EmployeeEntity e, string? profileUrl) =>
        e.ProfilePhotoUrl = string.IsNullOrWhiteSpace(profileUrl) ? null : profileUrl.Trim();

    private static void ApplyProfileUrl(EmployeeEntity e, CreateEmployeeRequest dto)
    {
        if (dto.ProfileUrl is null)
            return;

        ApplyProfileUrlValue(e, dto.ProfileUrl);
    }

    private async Task<string> ResolveDraftDisplayNameAsync(EmployeeEntity e, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(e.UserId))
        {
            var cp = await _cpUsers.GetByIdAsync(e.UserId, _tenant.TenantId, ct);
            if (!string.IsNullOrWhiteSpace(cp?.FullName))
                return cp.FullName;
        }

        if (!string.IsNullOrWhiteSpace(e.FullName))
            return e.FullName.Trim();

        return NameFormatting.ResolveFullName(e.FullName, e.FirstName, e.MiddleName, e.LastName);
    }

    public async Task<Respons<string>> UploadProfilePhotoAsync(
        Guid id, Stream photoStream, string fileName, string contentType, CancellationToken ct = default)
    {
        if (contentType is not "image/jpeg" and not "image/png")
            return Respons<string>.ValidationError(
                new Dictionary<string, string> { ["contentType"] = "Only image/jpeg and image/png are allowed." });

        var entity = await _employees.GetByIdScopedForUpdateAsync(id, _tenant.TenantId, _tenant.OrgId, ct);
        if (entity is null)
            return Respons<string>.Fail("Employee not found.", statusCode: 404);

        if (string.IsNullOrWhiteSpace(entity.UserId))
        {
            return Respons<string>.ValidationError(
                new Dictionary<string, string>
                {
                    ["workEmail"] = "Save personal contact with work email first so the platform user exists.",
                });
        }

        var ext = contentType == "image/png" ? "png" : "jpg";
        var blobPath = EmployeeBlobPathBuilder.BuildProfilePath(
            _tenant.TenantId, _tenant.OrgId, _tenant.BusId, id, ext);

        await using var stream = photoStream;
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, ct);

        try
        {
            await _blobs.UploadAsync(
                _storageConfig.ContainerName,
                blobPath,
                ms.ToArray(),
                contentType,
                ct);
        }
        catch (Exception ex)
        {
            return Respons<string>.Fail(BlobStorageErrors.Map(ex), statusCode: 502);
        }

        var documentId = Guid.NewGuid().ToString();
        await _documents.AddAsync(new HrDocumentPathEntity
        {
            Id = documentId,
            TenantId = _tenant.TenantId,
            OrgId = _tenant.OrgId,
            BusId = _tenant.BusId,
            DocumentPath = blobPath,
            FileName = fileName,
            Description = "Employee profile photo",
            CreatedBy = _currentUser.UserId?.ToString(),
            Cdatetime = DateTimeOffset.UtcNow,
        }, ct);

        await _cpUsers.UpdateProfilePicAsync(entity.UserId, _tenant.TenantId, documentId, ct);
        entity.ProfilePhotoUrl = documentId;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await _employees.UpdateAsync(entity, ct);
        return Respons<string>.Ok(documentId);
    }

    private async Task<(EmployeeEntity? Value, Respons<EmployeeRegistrationReadDto>? Error)> LoadDraftAsync(
        Guid id, CancellationToken ct)
    {
        var entity = await _employees.GetByIdScopedForUpdateAsync(id, _tenant.TenantId, _tenant.OrgId, ct);
        if (entity is null)
            return (null, Respons<EmployeeRegistrationReadDto>.Fail("Employee not found.", statusCode: 404));
        return (entity, null);
    }

    private static void ApplyEmployment(EmployeeEntity e, CreateEmployeeRequest dto)
    {
        e.JobTitle = dto.JobTitle ?? e.JobTitle;
        e.DepartmentId = dto.DepartmentId ?? e.DepartmentId;
        e.BranchId = dto.BranchId ?? e.BranchId;
        e.WorkArrangement = WorkArrangementRules.Normalize(dto.WorkArrangement) ?? e.WorkArrangement;
        e.WorkLocation = dto.WorkLocation ?? e.WorkLocation;
        e.PayGrade = dto.PayGrade ?? e.PayGrade;
        e.StartDate = dto.StartDate ?? e.StartDate;
        e.EmploymentStartDate = dto.StartDate ?? e.EmploymentStartDate;
        e.ProbationEndDate = dto.ProbationEndDate ?? e.ProbationEndDate;
        e.WorkingHours = dto.WorkingHours ?? e.WorkingHours;
        e.NoticePeriod = dto.NoticePeriod ?? e.NoticePeriod;
        e.ReportsToId = dto.ReportsToId ?? e.ReportsToId;
        e.ManagerId = dto.ReportsToId ?? e.ManagerId;
        e.DottedLineManagerId = dto.DottedLineManagerId ?? e.DottedLineManagerId;
    }

    private async Task<Respons<EmployeeRegistrationReadDto>?> ApplyEmploymentTypeAsync(
        EmployeeEntity e,
        CreateEmployeeRequest dto,
        CancellationToken ct)
    {
        if (dto.EmploymentTypeId is null && string.IsNullOrWhiteSpace(dto.EmploymentTypeName))
            return null;

        var (ok, error, type) = await _employmentTypes.ResolveForWriteAsync(
            dto.EmploymentTypeId, dto.EmploymentTypeName, _tenant.TenantId, _tenant.OrgId, ct);
        if (!ok)
        {
            return Respons<EmployeeRegistrationReadDto>.ValidationError(new Dictionary<string, string>
            {
                ["employment_type_id"] = error ?? "Invalid employment type.",
            });
        }

        if (type is null)
            return null;

        e.EmploymentTypeId = type.Id;
        e.EmploymentType = type.Name;
        return null;
    }

    internal static void ApplyEmploymentExtras(
        EmployeeEntity e, EmployeeAggregateEmploymentDto? employment)
    {
        if (employment is null)
            return;

        if (!string.IsNullOrWhiteSpace(employment.EmploymentStatus))
            EmployeeLifecycleSync.SyncFromEmploymentStatus(e, employment.EmploymentStatus);

        if (employment.ContractType is not null)
            e.ContractType = string.IsNullOrWhiteSpace(employment.ContractType)
                ? null
                : employment.ContractType.Trim();
    }

    private static void ApplyCompensation(EmployeeEntity e, CreateEmployeeRequest dto)
    {
        e.GrossSalary = dto.GrossSalary ?? e.GrossSalary;
        e.NetSalary = dto.NetSalary ?? e.NetSalary;
        e.PayFrequency = dto.PayFrequency ?? e.PayFrequency;
        e.SalaryEffectiveFrom = dto.SalaryEffectiveFrom ?? e.SalaryEffectiveFrom;
        e.SsnitNumber = dto.SsnitNumber ?? e.SsnitNumber;
        e.TinNumber = dto.TinNumber ?? e.TinNumber;
        e.Tier2PensionProvider = dto.Tier2PensionProvider ?? e.Tier2PensionProvider;
        e.Tier3PensionProvider = dto.Tier3PensionProvider ?? e.Tier3PensionProvider;
        e.PaymentMethod = dto.PaymentMethod ?? e.PaymentMethod;
        e.BankAccountNumber = dto.BankAccountNumber ?? e.BankAccountNumber;
        e.MobileMoneyNumber = dto.MobileMoneyNumber ?? e.MobileMoneyNumber;
        e.AnnualizedCost = CalculateAnnualizedCost(e.GrossSalary, e.PayFrequency);
    }

    internal static decimal? CalculateAnnualizedCost(decimal? gross, string? payFrequency) =>
        payFrequency?.Trim().ToLowerInvariant() switch
        {
            "monthly" => gross * 12,
            "bi-weekly" => gross * 26,
            _ => null,
        };

    internal static string? MaskSensitive(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var v = value.Trim();
        if (v.Length <= 4)
            return new string('X', v.Length);
        return $"{v[..2]}XXXXX{v[^2..]}";
    }

    private async Task<Dictionary<string, string>?> ResolveCurrencyIdAsync(
        EmployeeEntity entity, CreateEmployeeRequest dto, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(dto.CurrencyId))
        {
            var id = dto.CurrencyId.Trim();
            if (!await _currencies.ExistsAsync(id, _tenant.TenantId, ct))
            {
                return new Dictionary<string, string>
                {
                    ["compensation.currency_id"] = "Currency not found for this tenant.",
                };
            }

            entity.CurrencyId = id;
            return null;
        }

        if (entity.CurrencyId is not null || dto.GrossSalary is null)
            return null;

        return new Dictionary<string, string>
        {
            ["compensation.currency_id"] = "currency_id is required when gross_salary is set.",
        };
    }

    private EmployeeEntity NewDraftEntity(string? userId, string draftDisplayName, DateTimeOffset now) =>
        new()
        {
            Id = Guid.NewGuid(),
            TenantId = _tenant.TenantId,
            OrgId = _tenant.OrgId,
            UserId = userId,
            FullName = draftDisplayName,
            LifecycleState = EmployeeLifecycleStates.Draft,
            LifecycleStatus = "draft",
            IsDraft = true,
            EmploymentStatus = EmploymentStatusValues.Draft,
            ContractType = null,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = _currentUser.UserId?.ToString(),
        };

    private async Task<EmployeeRegistrationReadDto> ToReadDtoAsync(EmployeeEntity e, CancellationToken ct)
    {
        CpUserDto? cp = null;
        if (!string.IsNullOrWhiteSpace(e.UserId))
            cp = await _cpUsers.GetByIdAsync(e.UserId, _tenant.TenantId, ct);

        return new EmployeeRegistrationReadDto
        {
            Id = e.Id,
            EmployeeCode = e.EmployeeCode,
            FullName = EmployeeIdentityResolver.ResolveFullName(e, cp),
            UserId = e.UserId,
            IsDraft = e.IsDraft,
            LifecycleStatus = e.LifecycleStatus,
            JobTitle = e.JobTitle,
            DepartmentId = e.DepartmentId,
            WorkEmail = EmployeeIdentityResolver.ResolveWorkEmail(e, cp),
            ProfileUrl = await _profileUrls.ResolveDisplayUrlAsync(
                EmployeeIdentityResolver.ResolveStoredProfileReference(e, cp), ct),
            AnnualizedCost = e.AnnualizedCost,
            CurrencyId = e.CurrencyId,
            MaskedSsnitNumber = MaskSensitive(e.SsnitNumber),
            MaskedTinNumber = MaskSensitive(e.TinNumber),
        };
    }
}
