using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Shared.Infrastructure;
using ZelosHR.Api.Shared.Validation;

namespace ZelosHR.Api.Entities.Employees;

/// <summary>IEmployeesService CRUD (EF + tenant context). Legacy routes use overloads with explicit tenant ids.</summary>
public partial class EmployeesService
{
    public async Task<Respons<EmployeeReadDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _employees.GetByIdScopedAsync(id, _tenant.TenantId, _tenant.OrgId, ct);
        return entity is null
            ? Respons<EmployeeReadDto>.NotFound("Employee not found.")
            : Respons<EmployeeReadDto>.Ok(entity.ToReadDto());
    }

    public async Task<Respons<IReadOnlyList<EmployeeReadDto>>> GetAllAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        var (items, total) = await _employees.GetPagedScopedAsync(
            _tenant.TenantId, _tenant.OrgId, page, pageSize, ct);
        var dtos = items.Select(e => e.ToReadDto()).ToList();
        return Respons<IReadOnlyList<EmployeeReadDto>>.Ok(
            dtos,
            pagination: new PaginationMeta { Page = page, Size = pageSize, Total = total });
    }

    public async Task<Respons<EmployeeReadDto>> CreateAsync(EmployeeWriteDto dto, CancellationToken ct = default)
    {
        var fieldErrors = EmployeeValidator.ValidateCreate(dto);
        if (fieldErrors.Count > 0)
            return Respons<EmployeeReadDto>.ValidationError(fieldErrors);

        var normalized = EmployeeMappingExtensions.NormalizeGhanaCard(dto.GhanaCardNumber!);
        if (await _employees.ExistsByGhanaCardAsync(normalized, _tenant.TenantId, ct: ct))
            return Respons<EmployeeReadDto>.Fail(DuplicateGhanaCardMessage, statusCode: 409);

        var entity = dto.ToEntity(_tenant.TenantId, _tenant.OrgId, employeeCode: string.Empty);
        entity.GhanaCardNumber = normalized;

        const int maxAttempts = EmployeeCodeAllocation.MaxAttempts;
        var startSeq = await _employees.GetNextEmployeeSequenceAsync(_tenant.TenantId, _tenant.OrgId, ct);
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            entity.EmployeeCode = EmployeeCodeAllocation.Format(startSeq, attempt);

            try
            {
                await _employees.AddAsync(entity, ct);
                _logger.LogInformation("Created employee {EmployeeId} code={EmployeeCode}", entity.Id, entity.EmployeeCode);
                return Respons<EmployeeReadDto>.Ok(entity.ToReadDto(), "Employee created.", statusCode: 201);
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException ex) when (PostgresUniqueViolation.IsGhanaCard(ex))
            {
                return Respons<EmployeeReadDto>.Fail(DuplicateGhanaCardMessage, statusCode: 409);
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException ex) when (PostgresUniqueViolation.IsEmployeeCode(ex))
            {
                if (attempt == maxAttempts - 1)
                {
                    return Respons<EmployeeReadDto>.Fail(
                        EmployeeErrorMessages.EmployeeCodeAllocationFailed, statusCode: 409);
                }
            }
        }

        return Respons<EmployeeReadDto>.Fail(
            EmployeeErrorMessages.EmployeeCodeAllocationFailed, statusCode: 409);
    }

    public async Task<Respons<EmployeeReadDto>> UpdateAsync(
        Guid id, EmployeeWriteDto dto, CancellationToken ct = default)
    {
        var fieldErrors = EmployeeValidator.ValidateUpdate(dto);
        if (fieldErrors.Count > 0)
            return Respons<EmployeeReadDto>.ValidationError(fieldErrors);

        var entity = await _employees.GetByIdScopedForUpdateAsync(id, _tenant.TenantId, _tenant.OrgId, ct);
        if (entity is null)
            return Respons<EmployeeReadDto>.NotFound("Employee not found.");

        if (!string.IsNullOrWhiteSpace(dto.GhanaCardNumber))
        {
            var normalized = EmployeeMappingExtensions.NormalizeGhanaCard(dto.GhanaCardNumber);
            if (await _employees.ExistsByGhanaCardAsync(normalized, _tenant.TenantId, id, ct))
                return Respons<EmployeeReadDto>.Fail(DuplicateGhanaCardMessage, statusCode: 409);
            entity.GhanaCardNumber = normalized;
        }

        if (!string.IsNullOrWhiteSpace(dto.FirstName))
            entity.FirstName = dto.FirstName.Trim();
        if (dto.MiddleName is not null)
            entity.MiddleName = string.IsNullOrWhiteSpace(dto.MiddleName) ? null : dto.MiddleName.Trim();
        if (!string.IsNullOrWhiteSpace(dto.LastName))
            entity.LastName = dto.LastName.Trim();
        if (!string.IsNullOrWhiteSpace(dto.Email))
            entity.PersonalEmail = dto.Email.Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(dto.Phone))
            entity.PersonalPhone = dto.Phone.Trim();
        if (dto.JobTitle is not null)
            entity.JobTitle = dto.JobTitle;
        if (dto.DepartmentId.HasValue)
            entity.DepartmentId = dto.DepartmentId;
        if (!string.IsNullOrWhiteSpace(dto.EmploymentType))
            entity.EmploymentType = dto.EmploymentType;

        await _employees.UpdateAsync(entity, ct);
        return Respons<EmployeeReadDto>.Ok(entity.ToReadDto());
    }

    public async Task<Respons<bool>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var deleted = await _employees.SoftDeleteScopedAsync(id, _tenant.TenantId, _tenant.OrgId, ct);
        return deleted
            ? Respons<bool>.Ok(true, "Employee removed.")
            : Respons<bool>.NotFound("Employee not found.");
    }

    public async Task<Respons<IReadOnlyList<EmployeeReadDto>>> SearchAsync(
        string? nameQuery,
        Guid? departmentId,
        string? status,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var (items, total) = await _employees.SearchScopedAsync(
            nameQuery, departmentId, status, _tenant.TenantId, _tenant.OrgId, page, pageSize, ct);
        var dtos = items.Select(e => e.ToReadDto()).ToList();
        return Respons<IReadOnlyList<EmployeeReadDto>>.Ok(
            dtos,
            pagination: new PaginationMeta { Page = page, Size = pageSize, Total = total });
    }
}
