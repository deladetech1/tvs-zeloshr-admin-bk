using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Persistence.Entities;
using ZelosHR.Api.Persistence.Repositories;
using ZelosHR.Api.Shared.Pagination;

namespace ZelosHR.Api.Entities.EmploymentTypes;

public class EmploymentTypesService
{
    private readonly IEmploymentTypeRepository _repository;
    private readonly ICpUserRepository _cpUsers;

    public EmploymentTypesService(IEmploymentTypeRepository repository, ICpUserRepository cpUsers)
    {
        _repository = repository;
        _cpUsers = cpUsers;
    }

    public async Task<Respons<EmploymentTypeListDto>> ListAsync(
        EmploymentTypeListQuery query,
        string tenantId,
        string orgId,
        CancellationToken ct = default)
    {
        await EnsureReadyAsync(tenantId, orgId, ct);

        var paging = PagedQuery.From(query.Page, query.Size);
        var (items, total) = await _repository.ListScopedAsync(
            tenantId, orgId, query, paging.Page, paging.Size, ct);

        var enriched = await EnrichAuditAsync(items, tenantId, ct);

        return Respons<EmploymentTypeListDto>.Ok(
            new EmploymentTypeListDto { Items = enriched },
            pagination: new PaginationMeta
            {
                Page = paging.Page,
                Size = paging.Size,
                Total = total,
                HasNext = paging.Offset + enriched.Count < total,
            });
    }

    public async Task<Respons<EmploymentTypeListItemDto>> GetByIdAsync(
        Guid employmentTypeId,
        string tenantId,
        string orgId,
        CancellationToken ct = default)
    {
        await EnsureReadyAsync(tenantId, orgId, ct);

        var item = await _repository.GetByIdScopedAsync(employmentTypeId, tenantId, orgId, ct);
        if (item is null)
            return Respons<EmploymentTypeListItemDto>.Fail("Employment type not found.", statusCode: 404);

        var enriched = await EnrichAuditAsync([item], tenantId, ct);
        return Respons<EmploymentTypeListItemDto>.Ok(enriched[0]);
    }

    public async Task<Respons<EmploymentTypeListItemDto>> CreateAsync(
        CreateEmploymentTypeDto body,
        string tenantId,
        string orgId,
        string? actorUserId,
        CancellationToken ct = default)
    {
        var errors = ValidateCreate(body);
        if (errors is not null)
            return Respons<EmploymentTypeListItemDto>.ValidationError(errors);

        await EnsureReadyAsync(tenantId, orgId, ct);

        if (await _repository.NameExistsScopedAsync(tenantId, orgId, body.Name!.Trim(), null, ct))
        {
            return Respons<EmploymentTypeListItemDto>.ValidationError(new Dictionary<string, string>
            {
                ["name"] = "An employment type with this name already exists.",
            });
        }

        var id = await _repository.CreateScopedAsync(tenantId, orgId, body, actorUserId, ct);
        return await GetByIdAsync(id, tenantId, orgId, ct);
    }

    public async Task<Respons<EmploymentTypeListItemDto>> UpdateAsync(
        Guid employmentTypeId,
        UpdateEmploymentTypeDto body,
        string tenantId,
        string orgId,
        string? actorUserId,
        CancellationToken ct = default)
    {
        var errors = ValidateUpdate(body);
        if (errors is not null)
            return Respons<EmploymentTypeListItemDto>.ValidationError(errors);

        var existing = await _repository.GetEntityByIdScopedAsync(employmentTypeId, tenantId, orgId, ct);
        if (existing is null)
            return Respons<EmploymentTypeListItemDto>.Fail("Employment type not found.", statusCode: 404);

        if (!string.IsNullOrWhiteSpace(body.Name)
            && existing.IsSystemDefault
            && !string.Equals(body.Name.Trim(), existing.Name, StringComparison.Ordinal))
        {
            return Respons<EmploymentTypeListItemDto>.ValidationError(new Dictionary<string, string>
            {
                ["name"] = "System default employment types cannot be renamed.",
            });
        }

        if (!string.IsNullOrWhiteSpace(body.Name)
            && !string.Equals(body.Name.Trim(), existing.Name, StringComparison.Ordinal)
            && await _repository.NameExistsScopedAsync(tenantId, orgId, body.Name.Trim(), employmentTypeId, ct))
        {
            return Respons<EmploymentTypeListItemDto>.ValidationError(new Dictionary<string, string>
            {
                ["name"] = "An employment type with this name already exists.",
            });
        }

        try
        {
            var updated = await _repository.UpdateScopedAsync(
                employmentTypeId, tenantId, orgId, body, actorUserId, ct);
            if (updated is null)
                return Respons<EmploymentTypeListItemDto>.Fail("Employment type not found.", statusCode: 404);

            var enriched = await EnrichAuditAsync([updated], tenantId, ct);
            return Respons<EmploymentTypeListItemDto>.Ok(enriched[0]);
        }
        catch (InvalidOperationException ex)
        {
            return Respons<EmploymentTypeListItemDto>.ValidationError(new Dictionary<string, string>
            {
                ["name"] = ex.Message,
            });
        }
    }

    public async Task<Respons<object>> DeleteAsync(
        Guid employmentTypeId,
        string tenantId,
        string orgId,
        CancellationToken ct = default)
    {
        var existing = await _repository.GetEntityByIdScopedAsync(employmentTypeId, tenantId, orgId, ct);
        if (existing is null)
            return Respons<object>.Fail("Employment type not found.", statusCode: 404);

        if (existing.IsSystemDefault)
        {
            return Respons<object>.ValidationError(new Dictionary<string, string>
            {
                ["employment_type_id"] = "System default employment types cannot be deleted.",
            });
        }

        try
        {
            var (found, inUse) = await _repository.DeleteScopedAsync(employmentTypeId, tenantId, orgId, ct);
            if (!found)
                return Respons<object>.Fail("Employment type not found.", statusCode: 404);
            if (inUse)
            {
                return Respons<object>.Fail(
                    "Employment type is assigned to employees and cannot be deleted.",
                    statusCode: 409);
            }

            return Respons<object>.Ok(new { }, detail: "Employment type deleted.");
        }
        catch (InvalidOperationException ex)
        {
            return Respons<object>.ValidationError(new Dictionary<string, string>
            {
                ["employment_type_id"] = ex.Message,
            });
        }
    }

    internal async Task<EmployeeEmploymentTypeRefDto?> ResolveRefAsync(
        Guid? employmentTypeId,
        string? employmentTypeName,
        string tenantId,
        string orgId,
        CancellationToken ct)
    {
        if (employmentTypeId is null && string.IsNullOrWhiteSpace(employmentTypeName))
            return null;

        await EnsureReadyAsync(tenantId, orgId, ct);

        if (employmentTypeId.HasValue)
        {
            var byId = await _repository.GetEntityByIdScopedAsync(employmentTypeId.Value, tenantId, orgId, ct);
            if (byId is not null)
                return ToRef(byId);
        }

        if (string.IsNullOrWhiteSpace(employmentTypeName))
            return null;

        var (items, _) = await _repository.ListScopedAsync(
            tenantId,
            orgId,
            new EmploymentTypeListQuery { Size = 200, Page = 1 },
            1,
            200,
            ct);

        var match = items.FirstOrDefault(i =>
            string.Equals(i.Name, employmentTypeName.Trim(), StringComparison.OrdinalIgnoreCase));
        return match is null ? null : ToRef(match);
    }

    internal static EmployeeEmploymentTypeRefDto ToRef(EmploymentTypeEntity entity) =>
        new()
        {
            Id = entity.Id.ToString(),
            Name = entity.Name,
            Description = entity.Description,
            Type = entity.IsSystemDefault ? EmploymentTypeKind.Default : EmploymentTypeKind.Custom,
        };

    internal static EmployeeEmploymentTypeRefDto ToRef(EmploymentTypeListItemDto item) =>
        new()
        {
            Id = item.EmploymentTypeId,
            Name = item.Name,
            Description = item.Description,
            Type = item.Type,
        };

    internal async Task<(bool Ok, string? Error, EmploymentTypeEntity? Type)> ResolveForWriteAsync(
        Guid? employmentTypeId,
        string? employmentTypeName,
        string tenantId,
        string orgId,
        CancellationToken ct)
    {
        await EnsureReadyAsync(tenantId, orgId, ct);

        if (employmentTypeId.HasValue)
        {
            var entity = await _repository.GetEntityByIdScopedAsync(employmentTypeId.Value, tenantId, orgId, ct);
            if (entity is null)
                return (false, "employment_type_id is not a valid employment type for this organisation.", null);
            if (!entity.IsActive)
                return (false, "employment_type_id refers to an inactive employment type.", null);
            return (true, null, entity);
        }

        if (!string.IsNullOrWhiteSpace(employmentTypeName))
        {
            var (items, _) = await _repository.ListScopedAsync(
                tenantId,
                orgId,
                new EmploymentTypeListQuery { Size = 200, Page = 1 },
                1,
                200,
                ct);
            var match = items.FirstOrDefault(i =>
                string.Equals(i.Name, employmentTypeName.Trim(), StringComparison.OrdinalIgnoreCase));
            if (match is null)
                return (false, $"employment_type '{employmentTypeName.Trim()}' is not configured for this organisation.", null);

            var entity = await _repository.GetEntityByIdScopedAsync(
                Guid.Parse(match.EmploymentTypeId), tenantId, orgId, ct);
            return (true, null, entity);
        }

        return (true, null, null);
    }

    private async Task EnsureReadyAsync(string tenantId, string orgId, CancellationToken ct)
    {
        await _repository.EnsureSystemDefaultsScopedAsync(tenantId, orgId, ct);
        await _repository.BackfillEmployeeTypeIdsScopedAsync(tenantId, orgId, ct);
    }

    private static Dictionary<string, string>? ValidateCreate(CreateEmploymentTypeDto body)
    {
        var errors = new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(body.Name))
            errors["name"] = "Type name is required.";
        else if (body.Name.Trim().Length > 100)
            errors["name"] = "Type name must be at most 100 characters.";

        if (body.Description is not null && body.Description.Trim().Length > 500)
            errors["description"] = "Description must be at most 500 characters.";

        return errors.Count == 0 ? null : errors;
    }

    private static Dictionary<string, string>? ValidateUpdate(UpdateEmploymentTypeDto body)
    {
        if (body.Name is null && body.Description is null && body.IsActive is null)
        {
            return new Dictionary<string, string>
            {
                ["body"] = "Provide at least one field to update.",
            };
        }

        var errors = new Dictionary<string, string>();
        if (body.Name is not null && string.IsNullOrWhiteSpace(body.Name))
            errors["name"] = "Type name cannot be empty.";
        else if (body.Name is not null && body.Name.Trim().Length > 100)
            errors["name"] = "Type name must be at most 100 characters.";

        if (body.Description is not null && body.Description.Trim().Length > 500)
            errors["description"] = "Description must be at most 500 characters.";

        return errors.Count == 0 ? null : errors;
    }

    private async Task<IReadOnlyList<EmploymentTypeListItemDto>> EnrichAuditAsync(
        IReadOnlyList<EmploymentTypeListItemDto> items,
        string tenantId,
        CancellationToken ct)
    {
        if (items.Count == 0)
            return items;

        var userIds = items
            .SelectMany(i => new[] { i.CreatedById, i.UpdatedById })
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!)
            .Distinct();
        var users = await _cpUsers.GetByIdsAsync(userIds, tenantId, ct);

        return items.Select(item => item with
        {
            CreatedBy = ResourceAuditMapper.ResolveDisplayName(item.CreatedById, users),
            UpdatedBy = ResourceAuditMapper.ResolveDisplayName(item.UpdatedById, users),
        }).ToList();
    }
}
