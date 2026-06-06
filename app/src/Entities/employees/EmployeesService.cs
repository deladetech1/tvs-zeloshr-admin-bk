using Microsoft.EntityFrameworkCore;
using Npgsql;
using ZelosHR.Api.Entities.Branches;
using ZelosHR.Api.Entities.Departments;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Persistence.Entities;
using ZelosHR.Api.Shared.Abstractions;
using ZelosHR.Api.Shared.Formatting;
using ZelosHR.Api.Shared.Infrastructure;

namespace ZelosHR.Api.Entities.Employees;

public partial class EmployeesService : IEmployeesService, IEmployeeLookup
{
    internal const string DuplicateGhanaCardMessage =
        "This Ghana Card number is already registered to another employee";

    private readonly ILogger<EmployeesService> _logger;
    private readonly IEmployeeRepository _employees;
    private readonly ICpUserRepository _cpUsers;
    private readonly IDepartmentRepository _departments;
    private readonly IBranchRepository _branches;
    private readonly ITenantContext _tenant;

    public EmployeesService(
        ILogger<EmployeesService> logger,
        IEmployeeRepository employees,
        ICpUserRepository cpUsers,
        IDepartmentRepository departments,
        IBranchRepository branches,
        ITenantContext tenant)
    {
        _logger = logger;
        _employees = employees;
        _cpUsers = cpUsers;
        _departments = departments;
        _branches = branches;
        _tenant = tenant;
    }

    public async Task<Respons<CreateEmployeeServiceReadDto>> CreateEmployeeAsync(
        CreateEmployeeServiceWriteDto data,
        string tenantId,
        string orgId,
        CancellationToken ct = default)
    {
        var fieldErrors = ValidateCreate(data);
        if (fieldErrors.Count > 0)
            return Respons<CreateEmployeeServiceReadDto>.ValidationError(fieldErrors);

        var normalizedGhanaCard = NormalizeGhanaCard(data.GhanaCardNumber);
        if (await _employees.ExistsByGhanaCardAsync(normalizedGhanaCard, tenantId, ct: ct))
            return Respons<CreateEmployeeServiceReadDto>.Fail(DuplicateGhanaCardMessage, statusCode: 409);

        var entity = data.ToEntity(tenantId, orgId, employeeCode: string.Empty);

        const int maxAttempts = 3;
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            var seq = await _employees.GetNextEmployeeSequenceAsync(tenantId, orgId, ct);
            var employeeCode = $"ZEL-{seq:D4}";
            entity.EmployeeCode = employeeCode;

            try
            {
                await _employees.AddAsync(entity, ct);
                _logger.LogInformation(
                    "Created employee {EmployeeId} code={EmployeeCode} tenant={TenantId}",
                    entity.Id,
                    employeeCode,
                    tenantId);

                return Respons<CreateEmployeeServiceReadDto>.Ok(new CreateEmployeeServiceReadDto
                {
                    Id = entity.Id,
                    EmployeeCode = employeeCode,
                    FirstName = entity.FirstName,
                    MiddleName = entity.MiddleName,
                    LastName = entity.LastName,
                    LifecycleState = entity.LifecycleState,
                });
            }
            catch (DbUpdateException ex) when (PostgresUniqueViolation.IsGhanaCard(ex))
            {
                _logger.LogWarning(ex, "Duplicate Ghana Card on create");
                return Respons<CreateEmployeeServiceReadDto>.Fail(DuplicateGhanaCardMessage, statusCode: 409);
            }
            catch (DbUpdateException ex) when (PostgresUniqueViolation.IsEmployeeCode(ex))
            {
                if (attempt == maxAttempts - 1)
                {
                    _logger.LogWarning(ex, "Duplicate employee code on create after {Attempts} attempts", maxAttempts);
                    return Respons<CreateEmployeeServiceReadDto>.Fail(
                        "Could not allocate a unique employee code. Please retry.", statusCode: 409);
                }
            }
        }

        return Respons<CreateEmployeeServiceReadDto>.Fail(
            "Could not allocate a unique employee code. Please retry.", statusCode: 409);
    }

    public async Task<Respons<GetEmployeesServiceReadDto>> ListEmployeesAsync(
        string tenantId,
        string orgId,
        CancellationToken ct = default)
    {
        var (rows, _) = await _employees.GetPagedScopedAsync(tenantId, orgId, page: 1, pageSize: 10_000, ct);
        var userIds = rows.Select(r => r.UserId).Where(id => !string.IsNullOrWhiteSpace(id)).Select(id => id!).Distinct();
        var platformUsers = await _cpUsers.GetByIdsAsync(userIds, tenantId, ct);
        var items = rows.Select(r =>
        {
            platformUsers.TryGetValue(r.UserId ?? string.Empty, out var cp);
            return new EmployeeListItemServiceReadDto
            {
                Id = r.Id,
                EmployeeCode = r.EmployeeCode,
                FullName = EmployeeIdentityResolver.ResolveFullName(r, cp),
                LifecycleState = r.LifecycleState,
            };
        }).ToList();

        return Respons<GetEmployeesServiceReadDto>.Ok(new GetEmployeesServiceReadDto { Items = items });
    }

    public async Task<Respons<EmployeeDetailDto>> GetByIdAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default)
    {
        var entity = await _employees.GetByIdScopedAsync(id, tenantId, orgId, ct);
        if (entity is null)
            return Respons<EmployeeDetailDto>.NotFound("Employee not found.");

        return Respons<EmployeeDetailDto>.Ok(await MapDetailAsync(entity, tenantId, ct));
    }

    public async Task<Respons<EmployeeDetailDto>> UpdateProfileAsync(
        Guid id,
        UpdateEmployeeProfileDto data,
        string tenantId,
        string orgId,
        CancellationToken ct = default)
    {
        var fieldErrors = ValidateProfilePatch(data);
        if (fieldErrors.Count > 0)
            return Respons<EmployeeDetailDto>.ValidationError(fieldErrors);

        var entity = await _employees.GetByIdScopedForUpdateAsync(id, tenantId, orgId, ct);
        if (entity is null)
            return Respons<EmployeeDetailDto>.NotFound("Employee not found.");

        if (!string.IsNullOrWhiteSpace(data.GhanaCardNumber))
        {
            var normalized = NormalizeGhanaCard(data.GhanaCardNumber);
            if (await _employees.ExistsByGhanaCardAsync(normalized, tenantId, id, ct))
                return Respons<EmployeeDetailDto>.Fail(DuplicateGhanaCardMessage, statusCode: 409);
            entity.GhanaCardNumber = normalized;
        }

        var changed = data.GhanaCardNumber is not null;
        if (!string.IsNullOrWhiteSpace(data.FirstName))
        {
            entity.FirstName = data.FirstName.Trim();
            changed = true;
        }
        if (data.MiddleName is not null)
        {
            entity.MiddleName = string.IsNullOrWhiteSpace(data.MiddleName) ? null : data.MiddleName.Trim();
            changed = true;
        }
        if (!string.IsNullOrWhiteSpace(data.LastName))
        {
            entity.LastName = data.LastName.Trim();
            changed = true;
        }
        if (data.DateOfBirth.HasValue)
        {
            entity.DateOfBirth = data.DateOfBirth.Value;
            changed = true;
        }
        if (!string.IsNullOrWhiteSpace(data.Gender))
        {
            entity.Gender = data.Gender.Trim();
            changed = true;
        }
        if (!string.IsNullOrWhiteSpace(data.Nationality))
        {
            entity.Nationality = data.Nationality.Trim();
            changed = true;
        }
        if (!string.IsNullOrWhiteSpace(data.PersonalEmail))
        {
            entity.PersonalEmail = data.PersonalEmail.Trim().ToLowerInvariant();
            changed = true;
        }
        if (!string.IsNullOrWhiteSpace(data.PersonalPhone))
        {
            entity.PersonalPhone = data.PersonalPhone.Trim();
            changed = true;
        }
        if (!string.IsNullOrWhiteSpace(data.ResidentialAddress))
        {
            entity.ResidentialAddress = data.ResidentialAddress.Trim();
            changed = true;
        }
        if (!string.IsNullOrWhiteSpace(data.GhanaPostGps))
        {
            entity.GhanaPostGps = data.GhanaPostGps.Trim();
            changed = true;
        }

        if (!changed)
            return Respons<EmployeeDetailDto>.EmptyUpdateRequest();

        await _employees.UpdateAsync(entity, ct);
        return await GetByIdAsync(id, tenantId, orgId, ct);
    }

    public async Task<Respons<EmployeeDetailDto>> UpdateEmploymentAsync(
        Guid id,
        UpdateEmployeeEmploymentDto data,
        string tenantId,
        string orgId,
        CancellationToken ct = default)
    {
        var entity = await _employees.GetByIdScopedForUpdateAsync(id, tenantId, orgId, ct);
        if (entity is null)
            return Respons<EmployeeDetailDto>.NotFound("Employee not found.");

        if (data.DepartmentId.HasValue)
        {
            if (!await _departments.ExistsActiveScopedAsync(data.DepartmentId.Value, tenantId, orgId, ct))
                return Respons<EmployeeDetailDto>.ValidationError(
                    new Dictionary<string, string> { ["departmentId"] = "Department not found." });
        }

        if (data.BranchId.HasValue)
        {
            if (!await _branches.ExistsActiveScopedAsync(data.BranchId.Value, tenantId, orgId, ct))
                return Respons<EmployeeDetailDto>.ValidationError(
                    new Dictionary<string, string> { ["branchId"] = "Branch not found." });
        }

        if (data.ManagerId.HasValue)
        {
            if (data.ManagerId == id)
                return Respons<EmployeeDetailDto>.ValidationError(
                    new Dictionary<string, string> { ["managerId"] = "Employee cannot be their own manager." });
            if (!await _employees.ExistsActiveScopedAsync(data.ManagerId.Value, tenantId, orgId, ct))
                return Respons<EmployeeDetailDto>.ValidationError(
                    new Dictionary<string, string> { ["managerId"] = "Manager not found." });
        }

        var changed = false;
        if (data.JobTitle is not null)
        {
            entity.JobTitle = string.IsNullOrWhiteSpace(data.JobTitle) ? null : data.JobTitle.Trim();
            changed = true;
        }
        if (data.DepartmentId.HasValue)
        {
            entity.DepartmentId = data.DepartmentId;
            changed = true;
        }
        if (data.BranchId.HasValue)
        {
            entity.BranchId = data.BranchId;
            changed = true;
        }
        if (data.ManagerId.HasValue)
        {
            entity.ManagerId = data.ManagerId;
            changed = true;
        }
        if (data.EmploymentType is not null)
        {
            entity.EmploymentType = data.EmploymentType.Trim();
            changed = true;
        }
        if (!string.IsNullOrWhiteSpace(data.EmploymentStatus))
        {
            entity.EmploymentStatus = data.EmploymentStatus.Trim();
            changed = true;
        }
        if (data.ContractType is not null)
        {
            entity.ContractType = data.ContractType.Trim();
            changed = true;
        }
        if (data.ProbationEndDate is not null)
        {
            entity.ProbationEndDate = data.ProbationEndDate;
            changed = true;
        }
        if (data.EmploymentStartDate is not null)
        {
            entity.EmploymentStartDate = data.EmploymentStartDate;
            changed = true;
        }

        if (!changed)
            return Respons<EmployeeDetailDto>.EmptyUpdateRequest();

        await _employees.UpdateAsync(entity, ct);
        return await GetByIdAsync(id, tenantId, orgId, ct);
    }

    public async Task<Respons<EmployeeDetailDto>> UpdateLifecycleStateAsync(
        Guid id,
        string lifecycleState,
        string tenantId,
        string orgId,
        CancellationToken ct = default)
    {
        if (!IsAllowedLifecycleState(lifecycleState))
            return Respons<EmployeeDetailDto>.ValidationError(
                new Dictionary<string, string> { ["lifecycleState"] = "Invalid lifecycle state." });

        var entity = await _employees.GetByIdScopedForUpdateAsync(id, tenantId, orgId, ct);
        if (entity is null)
            return Respons<EmployeeDetailDto>.NotFound("Employee not found.");

        entity.LifecycleState = lifecycleState.Trim();
        await _employees.UpdateAsync(entity, ct);
        return await GetByIdAsync(id, tenantId, orgId, ct);
    }

    public async Task<Respons<object>> SoftDeleteAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default)
    {
        var deleted = await _employees.SoftDeleteScopedAsync(id, tenantId, orgId, ct);
        if (!deleted)
            return Respons<object>.NotFound("Employee not found.");

        return Respons<object>.Ok(new { employeeId = id.ToString() }, "Employee removed.");
    }

    public async Task<EmployeeDisplayInfo?> ResolveEmployeeDisplayAsync(
        Guid employeeId, string tenantId, string orgId, CancellationToken ct)
    {
        var entity = await _employees.GetByIdScopedAsync(employeeId, tenantId, orgId, ct);
        if (entity is null)
            return null;

        CpUserDto? cp = null;
        if (!string.IsNullOrWhiteSpace(entity.UserId))
            cp = await _cpUsers.GetByIdAsync(entity.UserId, tenantId, ct);

        return new EmployeeDisplayInfo(
            EmployeeIdentityResolver.ResolveFullName(entity, cp),
            entity.EmployeeCode);
    }

    private async Task<EmployeeDetailDto> MapDetailAsync(EmployeeEntity row, string tenantId, CancellationToken ct)
    {
        CpUserDto? cp = null;
        if (!string.IsNullOrWhiteSpace(row.UserId))
            cp = await _cpUsers.GetByIdAsync(row.UserId, tenantId, ct);

        CpUserDto? managerCp = null;
        if (row.Manager is not null && !string.IsNullOrWhiteSpace(row.Manager.UserId))
            managerCp = await _cpUsers.GetByIdAsync(row.Manager.UserId, tenantId, ct);

        var (first, last) = EmployeeIdentityResolver.ResolveNameParts(row, cp);

        return new EmployeeDetailDto
        {
            EmployeeId = row.Id.ToString(),
            EmployeeCode = row.EmployeeCode,
            UserId = row.UserId,
            FirstName = first,
            MiddleName = row.MiddleName,
            LastName = last,
            FullName = EmployeeIdentityResolver.ResolveFullName(row, cp),
            WorkEmail = EmployeeIdentityResolver.ResolveWorkEmail(row, cp),
            DateOfBirth = row.DateOfBirth ?? DateOnly.FromDateTime(DateTime.UtcNow),
            Gender = row.Gender ?? string.Empty,
            Nationality = row.Nationality ?? string.Empty,
            GhanaCardNumber = row.GhanaCardNumber ?? string.Empty,
            PersonalEmail = row.PersonalEmail ?? string.Empty,
            PersonalPhone = EmployeeIdentityResolver.ResolvePhone(row, cp) ?? row.PersonalPhone ?? string.Empty,
            ResidentialAddress = row.ResidentialAddress ?? string.Empty,
            GhanaPostGps = row.GhanaPostGps ?? string.Empty,
            LifecycleState = row.LifecycleState,
            JobTitle = row.JobTitle,
            DepartmentId = row.DepartmentId?.ToString(),
            DepartmentName = row.Department?.Name,
            BranchId = row.BranchId?.ToString(),
            BranchName = row.Branch?.Name,
            ManagerId = row.ManagerId?.ToString(),
            ManagerName = row.Manager is null
                ? null
                : EmployeeIdentityResolver.ResolveFullName(row.Manager, managerCp),
            EmploymentType = row.EmploymentType,
            EmploymentStatus = row.EmploymentStatus,
            ContractType = row.ContractType,
            ProbationEndDate = row.ProbationEndDate,
            EmploymentStartDate = row.EmploymentStartDate,
            CreatedAt = row.CreatedAt,
            UpdatedAt = row.UpdatedAt,
        };
    }

    private static bool IsAllowedLifecycleState(string state) =>
        state is EmployeeLifecycleStates.PreHire
            or EmployeeLifecycleStates.Active
            or EmployeeLifecycleStates.OnLeave
            or EmployeeLifecycleStates.Suspended
            or EmployeeLifecycleStates.Resigned
            or EmployeeLifecycleStates.Terminated;

    private static Dictionary<string, string> ValidateProfilePatch(UpdateEmployeeProfileDto data)
    {
        var errors = new Dictionary<string, string>();
        if (data.DateOfBirth.HasValue && data.DateOfBirth > DateOnly.FromDateTime(DateTime.UtcNow))
            errors["dateOfBirth"] = "Date of birth cannot be in the future.";
        if (!string.IsNullOrWhiteSpace(data.GhanaCardNumber) && !IsValidGhanaCardFormat(data.GhanaCardNumber))
            errors["ghanaCardNumber"] = "Ghana Card number format is invalid.";
        if (!string.IsNullOrWhiteSpace(data.PersonalEmail) && !data.PersonalEmail.Contains('@'))
            errors["personalEmail"] = "Personal email format is invalid.";
        return errors;
    }

    private static Dictionary<string, string> ValidateCreate(CreateEmployeeServiceWriteDto data)
    {
        var errors = new Dictionary<string, string>();

        if (string.IsNullOrWhiteSpace(data.FirstName))
            errors["firstName"] = "First name is required.";
        if (string.IsNullOrWhiteSpace(data.LastName))
            errors["lastName"] = "Last name is required.";
        if (data.DateOfBirth == default)
            errors["dateOfBirth"] = "Date of birth is required.";
        else if (data.DateOfBirth > DateOnly.FromDateTime(DateTime.UtcNow))
            errors["dateOfBirth"] = "Date of birth cannot be in the future.";
        if (string.IsNullOrWhiteSpace(data.Gender))
            errors["gender"] = "Gender is required.";
        if (string.IsNullOrWhiteSpace(data.Nationality))
            errors["nationality"] = "Nationality is required.";
        if (string.IsNullOrWhiteSpace(data.GhanaCardNumber))
            errors["ghanaCardNumber"] = "Ghana Card number is required.";
        else if (!IsValidGhanaCardFormat(data.GhanaCardNumber))
            errors["ghanaCardNumber"] = "Ghana Card number format is invalid.";
        if (string.IsNullOrWhiteSpace(data.PersonalEmail))
            errors["personalEmail"] = "Personal email is required.";
        else if (!data.PersonalEmail.Contains('@'))
            errors["personalEmail"] = "Personal email format is invalid.";
        if (string.IsNullOrWhiteSpace(data.PersonalPhone))
            errors["personalPhone"] = "Personal phone is required.";
        if (string.IsNullOrWhiteSpace(data.ResidentialAddress))
            errors["residentialAddress"] = "Residential address is required.";
        if (string.IsNullOrWhiteSpace(data.GhanaPostGps))
            errors["ghanaPostGps"] = "Ghana Post GPS address is required.";

        return errors;
    }

    private static bool IsValidGhanaCardFormat(string value)
    {
        var normalized = NormalizeGhanaCard(value);
        return normalized.Length >= 5 && normalized.Length <= 50;
    }

    private static string NormalizeGhanaCard(string value) =>
        EmployeeMappingExtensions.NormalizeGhanaCard(value);
}
