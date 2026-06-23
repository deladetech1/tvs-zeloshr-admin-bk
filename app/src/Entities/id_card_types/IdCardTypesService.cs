using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Persistence.Repositories;
using ZelosHR.Api.Shared.Pagination;

namespace ZelosHR.Api.Entities.IdCardTypes;

public class IdCardTypesService
{
    private readonly IIdCardTypeRepository _repository;
    private readonly ICpUserRepository _cpUsers;

    public IdCardTypesService(IIdCardTypeRepository repository, ICpUserRepository cpUsers)
    {
        _repository = repository;
        _cpUsers = cpUsers;
    }

    public async Task<Respons<IdCardTypeListDto>> ListAsync(
        IdCardTypeListQuery query,
        string tenantId,
        string orgId,
        CancellationToken ct = default)
    {
        await _repository.EnsureSystemDefaultsScopedAsync(tenantId, orgId, ct);

        var paging = PagedQuery.From(query.Page, query.Size);
        var (items, total) = await _repository.ListScopedAsync(
            tenantId, orgId, query, paging.Page, paging.Size, ct);

        var enriched = await EnrichAuditAsync(items, tenantId, ct);

        return Respons<IdCardTypeListDto>.Ok(
            new IdCardTypeListDto { Items = enriched },
            pagination: new PaginationMeta
            {
                Page = paging.Page,
                Size = paging.Size,
                Total = total,
                HasNext = paging.Offset + enriched.Count < total,
            });
    }

    public async Task<Respons<IdCardTypeListItemDto>> GetByIdAsync(
        Guid idCardTypeId,
        string tenantId,
        string orgId,
        CancellationToken ct = default)
    {
        await _repository.EnsureSystemDefaultsScopedAsync(tenantId, orgId, ct);

        var item = await _repository.GetByIdScopedAsync(idCardTypeId, tenantId, orgId, ct);
        if (item is null)
            return Respons<IdCardTypeListItemDto>.Fail("ID card type not found.", statusCode: 404);

        var enriched = await EnrichAuditAsync([item], tenantId, ct);
        return Respons<IdCardTypeListItemDto>.Ok(enriched[0]);
    }

    public async Task<Respons<IdCardTypeListItemDto>> CreateAsync(
        CreateIdCardTypeDto body,
        string tenantId,
        string orgId,
        string? actorUserId,
        CancellationToken ct = default)
    {
        var errors = ValidateCreate(body);
        if (errors is not null)
            return Respons<IdCardTypeListItemDto>.ValidationError(errors);

        await _repository.EnsureSystemDefaultsScopedAsync(tenantId, orgId, ct);

        if (await _repository.NameExistsScopedAsync(tenantId, orgId, body.Name!.Trim(), null, ct))
        {
            return Respons<IdCardTypeListItemDto>.ValidationError(new Dictionary<string, string>
            {
                ["name"] = "An ID card type with this name already exists.",
            });
        }

        var id = await _repository.CreateScopedAsync(tenantId, orgId, body, actorUserId, ct);
        return await GetByIdAsync(id, tenantId, orgId, ct);
    }

    public async Task<Respons<IdCardTypeListItemDto>> UpdateAsync(
        Guid idCardTypeId,
        UpdateIdCardTypeDto body,
        string tenantId,
        string orgId,
        string? actorUserId,
        CancellationToken ct = default)
    {
        var errors = ValidateUpdate(body);
        if (errors is not null)
            return Respons<IdCardTypeListItemDto>.ValidationError(errors);

        var existing = await _repository.GetEntityByIdScopedAsync(idCardTypeId, tenantId, orgId, ct);
        if (existing is null)
            return Respons<IdCardTypeListItemDto>.Fail("ID card type not found.", statusCode: 404);

        if (!string.IsNullOrWhiteSpace(body.Name)
            && existing.IsSystemDefault
            && !string.Equals(body.Name.Trim(), existing.Name, StringComparison.Ordinal))
        {
            return Respons<IdCardTypeListItemDto>.ValidationError(new Dictionary<string, string>
            {
                ["name"] = "System default ID card types cannot be renamed.",
            });
        }

        if (!string.IsNullOrWhiteSpace(body.Name)
            && !string.Equals(body.Name.Trim(), existing.Name, StringComparison.Ordinal)
            && await _repository.NameExistsScopedAsync(tenantId, orgId, body.Name.Trim(), idCardTypeId, ct))
        {
            return Respons<IdCardTypeListItemDto>.ValidationError(new Dictionary<string, string>
            {
                ["name"] = "An ID card type with this name already exists.",
            });
        }

        try
        {
            var updated = await _repository.UpdateScopedAsync(
                idCardTypeId, tenantId, orgId, body, actorUserId, ct);
            if (updated is null)
                return Respons<IdCardTypeListItemDto>.Fail("ID card type not found.", statusCode: 404);

            var enriched = await EnrichAuditAsync([updated], tenantId, ct);
            return Respons<IdCardTypeListItemDto>.Ok(enriched[0]);
        }
        catch (InvalidOperationException ex)
        {
            return Respons<IdCardTypeListItemDto>.ValidationError(new Dictionary<string, string>
            {
                ["name"] = ex.Message,
            });
        }
    }

    public async Task<Respons<object>> DeleteAsync(
        Guid idCardTypeId,
        string tenantId,
        string orgId,
        CancellationToken ct = default)
    {
        var existing = await _repository.GetEntityByIdScopedAsync(idCardTypeId, tenantId, orgId, ct);
        if (existing is null)
            return Respons<object>.Fail("ID card type not found.", statusCode: 404);

        if (existing.IsSystemDefault)
        {
            return Respons<object>.ValidationError(new Dictionary<string, string>
            {
                ["id_card_type_id"] = "System default ID card types cannot be deleted.",
            });
        }

        try
        {
            var (found, inUse) = await _repository.DeleteScopedAsync(idCardTypeId, tenantId, orgId, ct);
            if (!found)
                return Respons<object>.Fail("ID card type not found.", statusCode: 404);
            if (inUse)
            {
                return Respons<object>.Fail(
                    "ID card type is in use and cannot be deleted.",
                    statusCode: 409);
            }

            return Respons<object>.Ok(new { }, detail: "ID card type deleted.");
        }
        catch (InvalidOperationException ex)
        {
            return Respons<object>.ValidationError(new Dictionary<string, string>
            {
                ["id_card_type_id"] = ex.Message,
            });
        }
    }

    private static Dictionary<string, string>? ValidateCreate(CreateIdCardTypeDto body)
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

    private static Dictionary<string, string>? ValidateUpdate(UpdateIdCardTypeDto body)
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

    private async Task<IReadOnlyList<IdCardTypeListItemDto>> EnrichAuditAsync(
        IReadOnlyList<IdCardTypeListItemDto> items,
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
