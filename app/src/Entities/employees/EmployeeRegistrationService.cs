using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Persistence.Entities;
using ZelosHR.Api.Shared.Abstractions;
using ZelosHR.Api.Shared.Formatting;

namespace ZelosHR.Api.Entities.Employees;

public sealed class EmployeeRegistrationService
{
    private readonly IEmployeeRepository _employees;
    private readonly ICpUserRepository _cpUsers;
    private readonly IFileStorageService _files;
    private readonly ITenantContext _tenant;
    private readonly ICurrentUserService _currentUser;

    public EmployeeRegistrationService(
        IEmployeeRepository employees,
        ICpUserRepository cpUsers,
        IFileStorageService files,
        ITenantContext tenant,
        ICurrentUserService currentUser)
    {
        _employees = employees;
        _cpUsers = cpUsers;
        _files = files;
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
        string fullName, string? existingUserId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(fullName) && string.IsNullOrWhiteSpace(existingUserId))
            return Respons<EmployeeRegistrationReadDto>.ValidationError(
                new Dictionary<string, string> { ["fullName"] = "Full name is required." });

        string? userId = null;
        var resolvedName = fullName?.Trim() ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(existingUserId))
        {
            var cp = await _cpUsers.GetByIdAsync(existingUserId.Trim(), _tenant.TenantId, ct);
            if (cp is null)
                return Respons<EmployeeRegistrationReadDto>.ValidationError(
                    new Dictionary<string, string> { ["existingUserId"] = "User not found in platform." });

            if (await _cpUsers.IsLinkedToEmployeeAsync(cp.Id, _tenant.TenantId, ct))
                return Respons<EmployeeRegistrationReadDto>.Fail("User is already linked to an employee.", statusCode: 409);

            userId = cp.Id;
            resolvedName = cp.FullName;
        }

        var seq = await _employees.GetNextEmployeeSequenceAsync(_tenant.TenantId, _tenant.OrgId, ct);
        var now = DateTimeOffset.UtcNow;
        var entity = new EmployeeEntity
        {
            Id = Guid.NewGuid(),
            EmployeeCode = $"ZEL-{seq:D4}",
            TenantId = _tenant.TenantId,
            OrgId = _tenant.OrgId,
            UserId = userId,
            FullName = resolvedName,
            LifecycleState = "Draft",
            LifecycleStatus = "draft",
            IsDraft = true,
            EmploymentStatus = "Draft",
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = _currentUser.UserId?.ToString(),
        };

        await _employees.AddAsync(entity, ct);
        return Respons<EmployeeRegistrationReadDto>.Ok(ToReadDto(entity));
    }

    public Task<Respons<EmployeeRegistrationReadDto>> ImportAsync(string userId, CancellationToken ct = default) =>
        CreateDraftAsync(string.Empty, userId, ct);

    public async Task<Respons<EmployeeRegistrationReadDto>> UpdatePersonalContactAsync(
        Guid id, CreateEmployeeRequest dto, CancellationToken ct = default)
    {
        var entity = await LoadDraftAsync(id, ct);
        if (entity.Error is not null)
            return entity.Error;

        ApplyPersonalContact(entity.Value!, dto);
        entity.Value!.UpdatedAt = DateTimeOffset.UtcNow;
        entity.Value.UpdatedBy = _currentUser.UserId?.ToString();
        await _employees.UpdateAsync(entity.Value, ct);
        return Respons<EmployeeRegistrationReadDto>.Ok(ToReadDto(entity.Value));
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
        entity.Value!.UpdatedAt = DateTimeOffset.UtcNow;
        await _employees.UpdateAsync(entity.Value, ct);
        return Respons<EmployeeRegistrationReadDto>.Ok(ToReadDto(entity.Value));
    }

    public async Task<Respons<EmployeeRegistrationReadDto>> UpdateCompensationAsync(
        Guid id, CreateEmployeeRequest dto, CancellationToken ct = default)
    {
        var entity = await LoadDraftAsync(id, ct);
        if (entity.Error is not null)
            return entity.Error;

        ApplyCompensation(entity.Value!, dto);
        entity.Value!.UpdatedAt = DateTimeOffset.UtcNow;
        await _employees.UpdateAsync(entity.Value, ct);
        return Respons<EmployeeRegistrationReadDto>.Ok(ToReadDto(entity.Value));
    }

    public async Task<Respons<EmployeeRegistrationReadDto>> FinaliseAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await LoadDraftAsync(id, ct);
        if (entity.Error is not null)
            return entity.Error;

        var e = entity.Value!;
        if (string.IsNullOrWhiteSpace(e.FullName))
            return Respons<EmployeeRegistrationReadDto>.ValidationError(
                new Dictionary<string, string> { ["fullName"] = "Full name is required." });
        if (string.IsNullOrWhiteSpace(e.JobTitle))
            return Respons<EmployeeRegistrationReadDto>.ValidationError(
                new Dictionary<string, string> { ["jobTitle"] = "Job title is required." });
        if (e.DepartmentId is null)
            return Respons<EmployeeRegistrationReadDto>.ValidationError(
                new Dictionary<string, string> { ["departmentId"] = "Department is required." });

        // User linking on finalise: Trovesuite.Package user creation is wired when platform API is available.
        e.IsDraft = false;
        e.LifecycleStatus = "pre_hire";
        e.LifecycleState = "Pre-hire";
        e.EmploymentStatus = "Pre-hire";
        e.UpdatedAt = DateTimeOffset.UtcNow;
        await _employees.UpdateAsync(e, ct);
        return Respons<EmployeeRegistrationReadDto>.Ok(ToReadDto(e));
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

        var url = await _files.UploadAsync(
            photoStream, fileName, contentType, "profile-photos", _tenant.TenantId, id, ct);
        entity.ProfilePhotoUrl = url;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await _employees.UpdateAsync(entity, ct);
        return Respons<string>.Ok(url);
    }

    private async Task<(EmployeeEntity? Value, Respons<EmployeeRegistrationReadDto>? Error)> LoadDraftAsync(
        Guid id, CancellationToken ct)
    {
        var entity = await _employees.GetByIdScopedForUpdateAsync(id, _tenant.TenantId, _tenant.OrgId, ct);
        if (entity is null)
            return (null, Respons<EmployeeRegistrationReadDto>.Fail("Employee not found.", statusCode: 404));
        return (entity, null);
    }

    private static void ApplyPersonalContact(EmployeeEntity e, CreateEmployeeRequest dto)
    {
        if (!string.IsNullOrWhiteSpace(dto.FullName))
            e.FullName = dto.FullName.Trim();
        e.DateOfBirth = dto.DateOfBirth ?? e.DateOfBirth;
        e.Gender = dto.Gender ?? e.Gender;
        e.Nationality = dto.Nationality ?? e.Nationality;
        e.NationalityIdType = dto.NationalityIdType ?? e.NationalityIdType;
        e.IdNumber = dto.IdNumber ?? e.IdNumber;
        e.PersonalEmail = dto.PersonalEmail ?? e.PersonalEmail;
        e.WorkEmail = dto.WorkEmail ?? e.WorkEmail;
        e.Phone = dto.Phone ?? e.Phone;
        e.PersonalPhone = dto.Phone ?? e.PersonalPhone;
        e.LinkedInUrl = dto.LinkedInUrl ?? e.LinkedInUrl;
        e.GhanaPostGps = dto.GpsAddress ?? e.GhanaPostGps;
        e.State = dto.State ?? e.State;
        e.ResidentialAddress = dto.ResidentialAddress ?? e.ResidentialAddress;
    }

    private static void ApplyEmployment(EmployeeEntity e, CreateEmployeeRequest dto)
    {
        e.JobTitle = dto.JobTitle ?? e.JobTitle;
        e.DepartmentId = dto.DepartmentId ?? e.DepartmentId;
        e.EmploymentType = dto.EmploymentType ?? e.EmploymentType;
        e.WorkArrangement = dto.WorkArrangement ?? e.WorkArrangement;
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

    private static void ApplyCompensation(EmployeeEntity e, CreateEmployeeRequest dto)
    {
        e.GrossSalary = dto.GrossSalary ?? e.GrossSalary;
        e.PayFrequency = dto.PayFrequency ?? e.PayFrequency;
        e.SalaryEffectiveFrom = dto.SalaryEffectiveFrom ?? e.SalaryEffectiveFrom;
        e.Currency = dto.Currency ?? e.Currency ?? "GHS";
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

    private static EmployeeRegistrationReadDto ToReadDto(EmployeeEntity e) => new()
    {
        Id = e.Id,
        EmployeeCode = e.EmployeeCode,
        FullName = NameFormatting.ResolveFullName(e.FullName, e.FirstName, e.MiddleName, e.LastName),
        UserId = e.UserId,
        IsDraft = e.IsDraft,
        LifecycleStatus = e.LifecycleStatus,
        JobTitle = e.JobTitle,
        DepartmentId = e.DepartmentId,
        WorkEmail = e.WorkEmail,
        ProfilePhotoUrl = e.ProfilePhotoUrl,
        AnnualizedCost = e.AnnualizedCost,
        Currency = e.Currency,
        MaskedSsnitNumber = MaskSensitive(e.SsnitNumber),
        MaskedTinNumber = MaskSensitive(e.TinNumber),
    };
}
