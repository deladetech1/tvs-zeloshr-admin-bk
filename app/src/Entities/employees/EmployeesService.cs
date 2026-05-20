using Dapper;
using Microsoft.Extensions.Options;
using Npgsql;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Formatting;

namespace ZelosHR.Api.Entities.Employees;

public class EmployeesService
{
    private const string DuplicateGhanaCardMessage =
        "This Ghana Card number is already registered to another employee";

    private readonly IDatabaseManager _database;
    private readonly AppSettings _settings;
    private readonly ILogger<EmployeesService> _logger;

    public EmployeesService(
        IDatabaseManager database,
        IOptions<AppSettings> settings,
        ILogger<EmployeesService> logger)
    {
        _database = database;
        _settings = settings.Value;
        _logger = logger;
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

        try
        {
            await using var connection = await _database.GetConnectionAsync(ct);
            await using var transaction = await connection.BeginTransactionAsync(ct);

            var exists = await connection.ExecuteScalarAsync<bool>(
                $"""
                SELECT EXISTS(
                    SELECT 1 FROM {_settings.EmployeesTable}
                    WHERE tenant_id = @TenantId AND ghana_card_number = @GhanaCardNumber
                )
                """,
                new { TenantId = tenantId, GhanaCardNumber = normalizedGhanaCard },
                transaction);

            if (exists)
            {
                return Respons<CreateEmployeeServiceReadDto>.Fail(
                    DuplicateGhanaCardMessage,
                    statusCode: 409);
            }

            var employeeCode = await GenerateEmployeeCodeAsync(connection, transaction, tenantId, orgId, ct);

            var id = await connection.QuerySingleAsync<Guid>(
                $"""
                INSERT INTO {_settings.EmployeesTable} (
                    employee_code, tenant_id, org_id,
                    first_name, middle_name, last_name,
                    date_of_birth, gender, nationality, ghana_card_number,
                    personal_email, personal_phone, residential_address, ghana_post_gps,
                    lifecycle_state
                )
                VALUES (
                    @EmployeeCode, @TenantId, @OrgId,
                    @FirstName, @MiddleName, @LastName,
                    @DateOfBirth, @Gender, @Nationality, @GhanaCardNumber,
                    @PersonalEmail, @PersonalPhone, @ResidentialAddress, @GhanaPostGps,
                    @LifecycleState
                )
                RETURNING id
                """,
                new
                {
                    EmployeeCode = employeeCode,
                    TenantId = tenantId,
                    OrgId = orgId,
                    FirstName = data.FirstName.Trim(),
                    MiddleName = string.IsNullOrWhiteSpace(data.MiddleName) ? null : data.MiddleName.Trim(),
                    LastName = data.LastName.Trim(),
                    DateOfBirth = data.DateOfBirth,
                    Gender = data.Gender.Trim(),
                    Nationality = data.Nationality.Trim(),
                    GhanaCardNumber = normalizedGhanaCard,
                    PersonalEmail = data.PersonalEmail.Trim().ToLowerInvariant(),
                    PersonalPhone = data.PersonalPhone.Trim(),
                    ResidentialAddress = data.ResidentialAddress.Trim(),
                    GhanaPostGps = data.GhanaPostGps.Trim(),
                    LifecycleState = EmployeeLifecycleStates.PreHire,
                },
                transaction);

            await transaction.CommitAsync(ct);

            _logger.LogInformation(
                "Created employee {EmployeeId} code={EmployeeCode} tenant={TenantId}",
                id,
                employeeCode,
                tenantId);

            return Respons<CreateEmployeeServiceReadDto>.Ok(new CreateEmployeeServiceReadDto
            {
                Id = id,
                EmployeeCode = employeeCode,
                FirstName = data.FirstName.Trim(),
                MiddleName = string.IsNullOrWhiteSpace(data.MiddleName) ? null : data.MiddleName.Trim(),
                LastName = data.LastName.Trim(),
                LifecycleState = EmployeeLifecycleStates.PreHire,
            });
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            _logger.LogWarning(ex, "Duplicate Ghana Card on create");
            return Respons<CreateEmployeeServiceReadDto>.Fail(DuplicateGhanaCardMessage, statusCode: 409);
        }
    }

    public async Task<Respons<GetEmployeesServiceReadDto>> ListEmployeesAsync(
        string tenantId,
        string orgId,
        CancellationToken ct = default)
    {
        await using var connection = await _database.GetConnectionAsync(ct);

        var rows = await connection.QueryAsync<EmployeeListRow>(
            $"""
            SELECT
                id,
                employee_code AS EmployeeCode,
                TRIM(CONCAT(first_name, ' ', COALESCE(middle_name || ' ', ''), last_name)) AS FullName,
                lifecycle_state AS LifecycleState
            FROM {_settings.EmployeesTable}
            WHERE tenant_id = @TenantId AND org_id = @OrgId
            ORDER BY created_at DESC
            """,
            new { TenantId = tenantId, OrgId = orgId });

        var items = rows.Select(r => new EmployeeListItemServiceReadDto
        {
            Id = r.Id,
            EmployeeCode = r.EmployeeCode,
            FullName = r.FullName,
            LifecycleState = r.LifecycleState,
        }).ToList();

        return Respons<GetEmployeesServiceReadDto>.Ok(new GetEmployeesServiceReadDto { Items = items });
    }

    public async Task<Respons<EmployeeDetailDto>> GetByIdAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default)
    {
        await using var connection = await _database.GetConnectionAsync(ct);
        var row = await connection.QuerySingleOrDefaultAsync<EmployeeDetailRow>(
            $"""
            SELECT
                e.id AS Id,
                e.employee_code AS EmployeeCode,
                e.first_name AS FirstName,
                e.middle_name AS MiddleName,
                e.last_name AS LastName,
                e.date_of_birth AS DateOfBirth,
                e.gender AS Gender,
                e.nationality AS Nationality,
                e.ghana_card_number AS GhanaCardNumber,
                e.personal_email AS PersonalEmail,
                e.personal_phone AS PersonalPhone,
                e.residential_address AS ResidentialAddress,
                e.ghana_post_gps AS GhanaPostGps,
                e.lifecycle_state AS LifecycleState,
                e.job_title AS JobTitle,
                e.department_id AS DepartmentId,
                d.name AS DepartmentName,
                e.branch_id AS BranchId,
                b.name AS BranchName,
                e.manager_id AS ManagerId,
                m.first_name AS ManagerFirstName,
                m.middle_name AS ManagerMiddleName,
                m.last_name AS ManagerLastName,
                e.employment_type AS EmploymentType,
                e.employment_status AS EmploymentStatus,
                e.contract_type AS ContractType,
                e.probation_end_date AS ProbationEndDate,
                e.employment_start_date AS EmploymentStartDate,
                e.created_at AS CreatedAt,
                e.updated_at AS UpdatedAt
            FROM {_settings.EmployeesTable} e
            LEFT JOIN zeloshr.zhr_departments d ON d.id = e.department_id
            LEFT JOIN zeloshr.zhr_branches b ON b.id = e.branch_id
            LEFT JOIN {_settings.EmployeesTable} m ON m.id = e.manager_id
            WHERE e.id = @Id AND e.tenant_id = @TenantId AND e.org_id = @OrgId AND e.is_deleted = FALSE
            """,
            new { Id = id, TenantId = tenantId, OrgId = orgId });

        if (row is null)
            return Respons<EmployeeDetailDto>.Fail("Employee not found.", statusCode: 404);

        return Respons<EmployeeDetailDto>.Ok(MapDetail(row));
    }

    public async Task<Respons<EmployeeDetailDto>> UpdateProfileAsync(
        Guid id,
        UpdateEmployeeProfileDto data,
        string tenantId,
        string orgId,
        CancellationToken ct = default)
    {
        var existing = await GetByIdAsync(id, tenantId, orgId, ct);
        if (!existing.Success)
            return existing;

        var fieldErrors = ValidateProfilePatch(data);
        if (fieldErrors.Count > 0)
            return Respons<EmployeeDetailDto>.ValidationError(fieldErrors);

        if (!string.IsNullOrWhiteSpace(data.GhanaCardNumber))
        {
            var normalized = NormalizeGhanaCard(data.GhanaCardNumber);
            await using var connection = await _database.GetConnectionAsync(ct);
            var duplicate = await connection.ExecuteScalarAsync<bool>(
                $"""
                SELECT EXISTS(
                    SELECT 1 FROM {_settings.EmployeesTable}
                    WHERE tenant_id = @TenantId AND ghana_card_number = @GhanaCardNumber AND id <> @Id AND is_deleted = FALSE
                )
                """,
                new { TenantId = tenantId, GhanaCardNumber = normalized, Id = id });
            if (duplicate)
                return Respons<EmployeeDetailDto>.Fail(DuplicateGhanaCardMessage, statusCode: 409);
        }

        var sets = new List<string>();
        var parameters = new DynamicParameters(new { Id = id, TenantId = tenantId, OrgId = orgId });

        void Set<T>(string column, string param, T? value)
        {
            if (value is null) return;
            sets.Add($"{column} = @{param}");
            parameters.Add(param, value);
        }

        if (!string.IsNullOrWhiteSpace(data.FirstName))
            Set("first_name", "FirstName", data.FirstName.Trim());
        if (data.MiddleName is not null)
            Set("middle_name", "MiddleName", string.IsNullOrWhiteSpace(data.MiddleName) ? null : data.MiddleName.Trim());
        if (!string.IsNullOrWhiteSpace(data.LastName))
            Set("last_name", "LastName", data.LastName.Trim());
        if (data.DateOfBirth.HasValue)
            Set("date_of_birth", "DateOfBirth", data.DateOfBirth.Value);
        if (!string.IsNullOrWhiteSpace(data.Gender))
            Set("gender", "Gender", data.Gender.Trim());
        if (!string.IsNullOrWhiteSpace(data.Nationality))
            Set("nationality", "Nationality", data.Nationality.Trim());
        if (!string.IsNullOrWhiteSpace(data.GhanaCardNumber))
            Set("ghana_card_number", "GhanaCardNumber", NormalizeGhanaCard(data.GhanaCardNumber));
        if (!string.IsNullOrWhiteSpace(data.PersonalEmail))
            Set("personal_email", "PersonalEmail", data.PersonalEmail.Trim().ToLowerInvariant());
        if (!string.IsNullOrWhiteSpace(data.PersonalPhone))
            Set("personal_phone", "PersonalPhone", data.PersonalPhone.Trim());
        if (!string.IsNullOrWhiteSpace(data.ResidentialAddress))
            Set("residential_address", "ResidentialAddress", data.ResidentialAddress.Trim());
        if (!string.IsNullOrWhiteSpace(data.GhanaPostGps))
            Set("ghana_post_gps", "GhanaPostGps", data.GhanaPostGps.Trim());

        if (sets.Count == 0)
            return Respons<EmployeeDetailDto>.Fail("No fields to update.", statusCode: 400);

        sets.Add("updated_at = NOW()");
        await using (var connection = await _database.GetConnectionAsync(ct))
        {
            var affected = await connection.ExecuteAsync(
                $"""
                UPDATE {_settings.EmployeesTable}
                SET {string.Join(", ", sets)}
                WHERE id = @Id AND tenant_id = @TenantId AND org_id = @OrgId AND is_deleted = FALSE
                """,
                parameters);
            if (affected == 0)
                return Respons<EmployeeDetailDto>.Fail("Employee not found.", statusCode: 404);
        }

        return await GetByIdAsync(id, tenantId, orgId, ct);
    }

    public async Task<Respons<EmployeeDetailDto>> UpdateEmploymentAsync(
        Guid id,
        UpdateEmployeeEmploymentDto data,
        string tenantId,
        string orgId,
        CancellationToken ct = default)
    {
        var existing = await GetByIdAsync(id, tenantId, orgId, ct);
        if (!existing.Success)
            return existing;

        await using var connection = await _database.GetConnectionAsync(ct);

        if (data.DepartmentId.HasValue)
        {
            var deptOk = await connection.ExecuteScalarAsync<bool>(
                """
                SELECT EXISTS(
                    SELECT 1 FROM zeloshr.zhr_departments
                    WHERE id = @Id AND tenant_id = @TenantId AND org_id = @OrgId AND is_archived = FALSE
                )
                """,
                new { Id = data.DepartmentId, TenantId = tenantId, OrgId = orgId });
            if (!deptOk)
                return Respons<EmployeeDetailDto>.ValidationError(
                    new Dictionary<string, string> { ["departmentId"] = "Department not found." });
        }

        if (data.BranchId.HasValue)
        {
            var branchOk = await connection.ExecuteScalarAsync<bool>(
                """
                SELECT EXISTS(
                    SELECT 1 FROM zeloshr.zhr_branches
                    WHERE id = @Id AND tenant_id = @TenantId AND org_id = @OrgId AND is_archived = FALSE
                )
                """,
                new { Id = data.BranchId, TenantId = tenantId, OrgId = orgId });
            if (!branchOk)
                return Respons<EmployeeDetailDto>.ValidationError(
                    new Dictionary<string, string> { ["branchId"] = "Branch not found." });
        }

        if (data.ManagerId.HasValue)
        {
            if (data.ManagerId == id)
                return Respons<EmployeeDetailDto>.ValidationError(
                    new Dictionary<string, string> { ["managerId"] = "Employee cannot be their own manager." });
            var mgrOk = await connection.ExecuteScalarAsync<bool>(
                $"""
                SELECT EXISTS(
                    SELECT 1 FROM {_settings.EmployeesTable}
                    WHERE id = @ManagerId AND tenant_id = @TenantId AND org_id = @OrgId AND is_deleted = FALSE
                )
                """,
                new { ManagerId = data.ManagerId, TenantId = tenantId, OrgId = orgId });
            if (!mgrOk)
                return Respons<EmployeeDetailDto>.ValidationError(
                    new Dictionary<string, string> { ["managerId"] = "Manager not found." });
        }

        var sets = new List<string>();
        var parameters = new DynamicParameters(new { Id = id, TenantId = tenantId, OrgId = orgId });

        void SetNullable<T>(string column, string param, T? value, bool include)
        {
            if (!include) return;
            sets.Add($"{column} = @{param}");
            parameters.Add(param, value);
        }

        SetNullable("job_title", "JobTitle", string.IsNullOrWhiteSpace(data.JobTitle) ? null : data.JobTitle.Trim(), data.JobTitle is not null);
        SetNullable("department_id", "DepartmentId", data.DepartmentId, data.DepartmentId.HasValue);
        SetNullable("branch_id", "BranchId", data.BranchId, data.BranchId.HasValue);
        SetNullable("manager_id", "ManagerId", data.ManagerId, data.ManagerId.HasValue);
        SetNullable("employment_type", "EmploymentType", data.EmploymentType?.Trim(), data.EmploymentType is not null);
        SetNullable("employment_status", "EmploymentStatus", data.EmploymentStatus?.Trim(), !string.IsNullOrWhiteSpace(data.EmploymentStatus));
        SetNullable("contract_type", "ContractType", data.ContractType?.Trim(), data.ContractType is not null);
        SetNullable("probation_end_date", "ProbationEndDate", data.ProbationEndDate, data.ProbationEndDate is not null);
        SetNullable("employment_start_date", "EmploymentStartDate", data.EmploymentStartDate, data.EmploymentStartDate is not null);

        if (sets.Count == 0)
            return Respons<EmployeeDetailDto>.Fail("No fields to update.", statusCode: 400);

        sets.Add("updated_at = NOW()");
        var affected = await connection.ExecuteAsync(
            $"""
            UPDATE {_settings.EmployeesTable}
            SET {string.Join(", ", sets)}
            WHERE id = @Id AND tenant_id = @TenantId AND org_id = @OrgId AND is_deleted = FALSE
            """,
            parameters);

        if (affected == 0)
            return Respons<EmployeeDetailDto>.Fail("Employee not found.", statusCode: 404);

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

        await using var connection = await _database.GetConnectionAsync(ct);
        var affected = await connection.ExecuteAsync(
            $"""
            UPDATE {_settings.EmployeesTable}
            SET lifecycle_state = @LifecycleState, updated_at = NOW()
            WHERE id = @Id AND tenant_id = @TenantId AND org_id = @OrgId AND is_deleted = FALSE
            """,
            new { Id = id, TenantId = tenantId, OrgId = orgId, LifecycleState = lifecycleState.Trim() });

        if (affected == 0)
            return Respons<EmployeeDetailDto>.Fail("Employee not found.", statusCode: 404);

        return await GetByIdAsync(id, tenantId, orgId, ct);
    }

    public async Task<Respons<object>> SoftDeleteAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default)
    {
        await using var connection = await _database.GetConnectionAsync(ct);
        var affected = await connection.ExecuteAsync(
            $"""
            UPDATE {_settings.EmployeesTable}
            SET is_deleted = TRUE, employment_status = 'Inactive', updated_at = NOW()
            WHERE id = @Id AND tenant_id = @TenantId AND org_id = @OrgId AND is_deleted = FALSE
            """,
            new { Id = id, TenantId = tenantId, OrgId = orgId });

        if (affected == 0)
            return Respons<object>.Fail("Employee not found.", statusCode: 404);

        return Respons<object>.Ok(new { employeeId = id.ToString() }, "Employee removed.");
    }

    public async Task<EmployeeDisplayInfo?> ResolveEmployeeDisplayAsync(
        Guid employeeId, string tenantId, string orgId, CancellationToken ct)
    {
        await using var connection = await _database.GetConnectionAsync(ct);
        return await connection.QuerySingleOrDefaultAsync<EmployeeDisplayInfo>(
            $"""
            SELECT
                TRIM(CONCAT(first_name, ' ', COALESCE(middle_name || ' ', ''), last_name)) AS FullName,
                employee_code AS EmployeeCode
            FROM {_settings.EmployeesTable}
            WHERE id = @Id AND tenant_id = @TenantId AND org_id = @OrgId AND is_deleted = FALSE
            """,
            new { Id = employeeId, TenantId = tenantId, OrgId = orgId });
    }

    public sealed class EmployeeDisplayInfo
    {
        public required string FullName { get; init; }
        public string? EmployeeCode { get; init; }
    }

    private static EmployeeDetailDto MapDetail(EmployeeDetailRow row) => new()
    {
        EmployeeId = row.Id.ToString(),
        EmployeeCode = row.EmployeeCode,
        FirstName = row.FirstName,
        MiddleName = row.MiddleName,
        LastName = row.LastName,
        FullName = NameFormatting.BuildFullName(row.FirstName, row.MiddleName, row.LastName),
        DateOfBirth = row.DateOfBirth,
        Gender = row.Gender,
        Nationality = row.Nationality,
        GhanaCardNumber = row.GhanaCardNumber,
        PersonalEmail = row.PersonalEmail,
        PersonalPhone = row.PersonalPhone,
        ResidentialAddress = row.ResidentialAddress,
        GhanaPostGps = row.GhanaPostGps,
        LifecycleState = row.LifecycleState,
        JobTitle = row.JobTitle,
        DepartmentId = row.DepartmentId?.ToString(),
        DepartmentName = row.DepartmentName,
        BranchId = row.BranchId?.ToString(),
        BranchName = row.BranchName,
        ManagerId = row.ManagerId?.ToString(),
        ManagerName = row.ManagerFirstName is null
            ? null
            : NameFormatting.BuildFullName(row.ManagerFirstName, row.ManagerMiddleName, row.ManagerLastName!),
        EmploymentType = row.EmploymentType,
        EmploymentStatus = row.EmploymentStatus,
        ContractType = row.ContractType,
        ProbationEndDate = row.ProbationEndDate,
        EmploymentStartDate = row.EmploymentStartDate,
        CreatedAt = row.CreatedAt,
        UpdatedAt = row.UpdatedAt,
    };

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
        value.Trim().ToUpperInvariant();

    private async Task<string> GenerateEmployeeCodeAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string tenantId,
        string orgId,
        CancellationToken ct)
    {
        var next = await connection.ExecuteScalarAsync<long>(
            $"""
            SELECT COALESCE(MAX(
                NULLIF(REGEXP_REPLACE(employee_code, '\\D', '', 'g'), '')::bigint
            ), 0) + 1
            FROM {_settings.EmployeesTable}
            WHERE tenant_id = @TenantId AND org_id = @OrgId
            """,
            new { TenantId = tenantId, OrgId = orgId },
            transaction);

        return $"ZEL-{next:D4}";
    }

    private sealed class EmployeeListRow
    {
        public Guid Id { get; init; }
        public required string EmployeeCode { get; init; }
        public required string FullName { get; init; }
        public required string LifecycleState { get; init; }
    }

    private sealed class EmployeeDetailRow
    {
        public Guid Id { get; init; }
        public required string EmployeeCode { get; init; }
        public required string FirstName { get; init; }
        public string? MiddleName { get; init; }
        public required string LastName { get; init; }
        public DateOnly DateOfBirth { get; init; }
        public required string Gender { get; init; }
        public required string Nationality { get; init; }
        public required string GhanaCardNumber { get; init; }
        public required string PersonalEmail { get; init; }
        public required string PersonalPhone { get; init; }
        public required string ResidentialAddress { get; init; }
        public required string GhanaPostGps { get; init; }
        public required string LifecycleState { get; init; }
        public string? JobTitle { get; init; }
        public Guid? DepartmentId { get; init; }
        public string? DepartmentName { get; init; }
        public Guid? BranchId { get; init; }
        public string? BranchName { get; init; }
        public Guid? ManagerId { get; init; }
        public string? ManagerFirstName { get; init; }
        public string? ManagerMiddleName { get; init; }
        public string? ManagerLastName { get; init; }
        public string? EmploymentType { get; init; }
        public required string EmploymentStatus { get; init; }
        public string? ContractType { get; init; }
        public DateOnly? ProbationEndDate { get; init; }
        public DateOnly? EmploymentStartDate { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset UpdatedAt { get; init; }
    }
}
