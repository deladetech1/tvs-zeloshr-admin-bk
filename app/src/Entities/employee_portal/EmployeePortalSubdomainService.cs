using Microsoft.Extensions.Options;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Persistence.Entities;
using ZelosHR.Api.Persistence.Repositories;

namespace ZelosHR.Api.Entities.EmployeePortal;

public sealed class EmployeePortalSubdomainService
{
    private readonly IEmployeePortalSubdomainRepository _subdomains;
    private readonly ICpUserRepository _cpUsers;
    private readonly string _appId;

    public EmployeePortalSubdomainService(
        IEmployeePortalSubdomainRepository subdomains,
        ICpUserRepository cpUsers,
        IOptions<AppSettings> appSettings)
    {
        _subdomains = subdomains;
        _cpUsers = cpUsers;
        _appId = appSettings.Value.AppId;
    }

    public async Task<Respons<EmployeePortalSubdomainListDto>> ListAsync(
        string tenantId,
        string orgId,
        string? actorUserId,
        CancellationToken ct = default)
    {
        var entity = await _subdomains.GetEntityAsync(tenantId, orgId, ct);
        if (entity is null)
            return Respons<EmployeePortalSubdomainListDto>.Ok(new EmployeePortalSubdomainListDto { Items = [] });

        var dto = await MapToReadDtoAsync(entity, tenantId, actorUserId, ct);
        return Respons<EmployeePortalSubdomainListDto>.Ok(new EmployeePortalSubdomainListDto { Items = [dto] });
    }

    public async Task<Respons<EmployeePortalSubdomainReadDto>> GetAsync(
        string tenantId,
        string orgId,
        string? actorUserId,
        CancellationToken ct = default)
    {
        var entity = await _subdomains.GetEntityAsync(tenantId, orgId, ct);
        if (entity is null)
            return Respons<EmployeePortalSubdomainReadDto>.Fail(
                "Employee portal subdomain is not configured.", statusCode: 404);

        var dto = await MapToReadDtoAsync(entity, tenantId, actorUserId, ct);
        return Respons<EmployeePortalSubdomainReadDto>.Ok(dto);
    }

    public async Task<Respons<EmployeePortalSubdomainReadDto>> CreateAsync(
        CreateEmployeePortalSubdomainDto body,
        string tenantId,
        string orgId,
        string busId,
        string locId,
        string? actorUserId,
        CancellationToken ct = default)
    {
        var errors = EmployeePortalSubdomainRules.Validate(body.Subdomain);
        if (errors is not null)
            return Respons<EmployeePortalSubdomainReadDto>.ValidationError(errors);

        var normalized = EmployeePortalSubdomainRules.Normalize(body.Subdomain!);

        var existing = await _subdomains.GetEntityAsync(tenantId, orgId, ct);
        if (existing is not null)
        {
            return Respons<EmployeePortalSubdomainReadDto>.ValidationError(new Dictionary<string, string>
            {
                ["subdomain"] =
                    "Employee portal subdomain already exists for this organisation. Use PUT /company/portal-subdomain/update instead.",
            });
        }

        if (await _subdomains.SubdomainTakenAsync(normalized, ct: ct))
        {
            return Respons<EmployeePortalSubdomainReadDto>.ValidationError(new Dictionary<string, string>
            {
                ["subdomain"] = "Subdomain is already in use by another organisation.",
            });
        }

        var entity = await _subdomains.CreateAsync(
            tenantId, orgId, busId, locId, body, actorUserId, ct);
        var dto = await MapToReadDtoAsync(entity, tenantId, actorUserId, ct);
        return Respons<EmployeePortalSubdomainReadDto>.Ok(dto);
    }

    public async Task<Respons<EmployeePortalSubdomainReadDto>> UpdateAsync(
        UpdateEmployeePortalSubdomainDto body,
        string tenantId,
        string orgId,
        string busId,
        string locId,
        string? actorUserId,
        CancellationToken ct = default)
    {
        var errors = MergeErrors(ValidateId(body.Id), EmployeePortalSubdomainRules.Validate(body.Subdomain));
        if (errors is not null)
            return Respons<EmployeePortalSubdomainReadDto>.ValidationError(errors);

        var existing = await _subdomains.GetEntityAsync(tenantId, orgId, ct);
        if (existing is null)
            return Respons<EmployeePortalSubdomainReadDto>.Fail(
                "Employee portal subdomain is not configured.", statusCode: 404);

        if (!Guid.TryParse(body.Id, out var parsedId) || parsedId != existing.Id)
        {
            return Respons<EmployeePortalSubdomainReadDto>.ValidationError(new Dictionary<string, string>
            {
                ["id"] = "id does not match the current employee portal subdomain settings.",
            });
        }

        var normalized = EmployeePortalSubdomainRules.Normalize(body.Subdomain!);
        if (await _subdomains.SubdomainTakenAsync(normalized, existing.Id, ct))
        {
            return Respons<EmployeePortalSubdomainReadDto>.ValidationError(new Dictionary<string, string>
            {
                ["subdomain"] = "Subdomain is already in use by another organisation.",
            });
        }

        var updated = await _subdomains.UpdateAsync(
            tenantId, orgId, busId, locId, body, actorUserId, ct);
        if (updated is null)
            return Respons<EmployeePortalSubdomainReadDto>.Fail(
                "Employee portal subdomain is not configured.", statusCode: 404);

        var dto = await MapToReadDtoAsync(updated, tenantId, actorUserId, ct);
        return Respons<EmployeePortalSubdomainReadDto>.Ok(dto);
    }

    public async Task<Respons<object>> DeleteAsync(
        string tenantId, string orgId, Guid id, CancellationToken ct = default)
    {
        var existing = await _subdomains.GetEntityAsync(tenantId, orgId, ct);
        if (existing is null)
            return Respons<object>.Fail("Employee portal subdomain is not configured.", statusCode: 404);

        if (id != existing.Id)
        {
            return Respons<object>.ValidationError(new Dictionary<string, string>
            {
                ["id"] = "id does not match the current employee portal subdomain settings.",
            });
        }

        var deleted = await _subdomains.DeleteAsync(tenantId, orgId, ct);
        if (!deleted)
            return Respons<object>.Fail("Employee portal subdomain is not configured.", statusCode: 404);

        return Respons<object>.Ok(new { }, detail: "Employee portal subdomain deleted.");
    }

    public async Task<Respons<EmployeePortalResolveDto>> ResolveBySubdomainAsync(
        string subdomain,
        CancellationToken ct = default)
    {
        var errors = EmployeePortalSubdomainRules.Validate(subdomain);
        if (errors is not null)
            return Respons<EmployeePortalResolveDto>.ValidationError(errors);

        var normalized = EmployeePortalSubdomainRules.Normalize(subdomain);
        var entity = await _subdomains.GetBySubdomainAsync(normalized, ct);
        if (entity is null)
        {
            return Respons<EmployeePortalResolveDto>.Fail(
                "Employee portal subdomain was not found.", statusCode: 404);
        }

        return Respons<EmployeePortalResolveDto>.Ok(new EmployeePortalResolveDto
        {
            Subdomain = entity.Subdomain,
            PortalUrl = EmployeePortalSubdomainRules.PortalHost(entity.Subdomain),
            TenantId = entity.TenantId,
            OrgId = entity.OrgId,
            BusId = entity.BusId,
            LocId = entity.LocId,
            AppId = _appId,
        });
    }

    private async Task<EmployeePortalSubdomainReadDto> MapToReadDtoAsync(
        EmployeePortalSubdomainEntity entity,
        string tenantId,
        string? actorUserId,
        CancellationToken ct)
    {
        var users = await _cpUsers.GetByIdsAsync(
            ResourceAuditMapper.CollectUserIds(new[] { new[] { entity.CreatedBy, entity.UpdatedBy } }),
            tenantId, ct);

        return new EmployeePortalSubdomainReadDto
        {
            Id = entity.Id.ToString(),
            Subdomain = entity.Subdomain,
            PortalUrl = EmployeePortalSubdomainRules.PortalHost(entity.Subdomain),
            TenantId = entity.TenantId,
            OrgId = entity.OrgId,
            BusId = entity.BusId,
            LocId = entity.LocId,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            CreatedById = entity.CreatedBy,
            UpdatedById = entity.UpdatedBy,
            CreatedBy = ResourceAuditMapper.ResolveDisplayName(entity.CreatedBy, users),
            UpdatedBy = ResourceAuditMapper.ResolveDisplayName(entity.UpdatedBy, users),
        };
    }

    private static Dictionary<string, string>? ValidateId(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return new Dictionary<string, string> { ["id"] = "Employee portal subdomain id is required." };
        if (!Guid.TryParse(id, out _))
            return new Dictionary<string, string> { ["id"] = "Employee portal subdomain id must be a valid UUID." };
        return null;
    }

    private static Dictionary<string, string>? MergeErrors(
        Dictionary<string, string>? first, Dictionary<string, string>? second)
    {
        if (first is null)
            return second;
        if (second is null)
            return first;
        foreach (var (key, value) in second)
            first[key] = value;
        return first;
    }
}
