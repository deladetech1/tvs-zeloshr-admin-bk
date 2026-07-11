using System.Text.Json;
using System.Text.Json.Nodes;
using ZelosHR.Api.Configs;
using ZelosHR.Api.Entities.Shared;
using ZelosHR.Api.Persistence.Entities;
using ZelosHR.Api.Shared.Tenant;

namespace ZelosHR.Api.Entities.Employees;

public sealed class ChangeRequestService
{
    private readonly IEmployeeChangeRequestRepository _changeRequests;
    private readonly IEmployeeUpdateService _employeeUpdate;
    private readonly ICpUserRepository _cpUsers;
    private readonly ITenantContextAccessor _tenant;

    public ChangeRequestService(
        IEmployeeChangeRequestRepository changeRequests,
        IEmployeeUpdateService employeeUpdate,
        ICpUserRepository cpUsers,
        ITenantContextAccessor tenant)
    {
        _changeRequests = changeRequests;
        _employeeUpdate = employeeUpdate;
        _cpUsers = cpUsers;
        _tenant = tenant;
    }

    public async Task<IReadOnlyList<ChangeRequestReadDto>> CreatePendingAsync(
        Guid employeeId,
        string requestedBy,
        IReadOnlyList<EmployeePayloadPendingChange> pending,
        CancellationToken ct = default)
    {
        if (pending.Count == 0)
            return [];

        var tenant = _tenant.Current;
        var now = DateTimeOffset.UtcNow;
        var fieldPaths = pending.Select(p => p.FieldPath).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var existing = await _changeRequests.ListPendingByFieldPathsAsync(employeeId, fieldPaths, ct);

        foreach (var row in existing)
        {
            row.Status = ChangeRequestStatuses.Superseded;
            row.UpdatedAt = now;
            row.UpdatedBy = requestedBy;
        }

        var created = new List<EmployeeChangeRequestEntity>();
        foreach (var item in pending)
        {
            var entity = new EmployeeChangeRequestEntity
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.TenantId,
                OrgId = tenant.OrgId,
                EmployeeId = employeeId,
                FieldPath = item.FieldPath,
                OldValueJson = item.OldValue is null ? null : item.OldValue.ToJsonString(),
                NewValueJson = item.NewValue.ToJsonString(),
                Status = ChangeRequestStatuses.Pending,
                RequestedBy = requestedBy,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = requestedBy,
                UpdatedBy = requestedBy,
            };
            await _changeRequests.AddAsync(entity, ct);
            created.Add(entity);
        }

        await _changeRequests.SaveChangesAsync(ct);
        return await MapManyAsync(created, ct);
    }

    public async Task<Respons<EmployeeAggregateReadDto>> ApproveAsync(Guid changeRequestId, CancellationToken ct = default)
    {
        var tenant = _tenant.Current;
        var reviewerId = tenant.UserId;
        if (string.IsNullOrWhiteSpace(reviewerId))
            return Respons<EmployeeAggregateReadDto>.Forbidden("Authenticated user is required.");

        var entity = await _changeRequests.GetTrackedByIdScopedAsync(changeRequestId, tenant.TenantId, tenant.OrgId, ct);
        if (entity is null)
            return Respons<EmployeeAggregateReadDto>.NotFound("Change request not found.");

        if (!string.Equals(entity.Status, ChangeRequestStatuses.Pending, StringComparison.Ordinal))
            return Respons<EmployeeAggregateReadDto>.Fail(
                "Only pending change requests can be approved.",
                statusCode: 409);

        var entityId = entity.Id;
        var employeeId = entity.EmployeeId;
        var patch = ChangeRequestReplayBuilder.Build(entity.FieldPath, entity.NewValueJson);
        var applyResult = await _employeeUpdate.ApplyAsync(employeeId, patch, ct);
        if (!applyResult.Success)
            return applyResult;

        var tracked = await _changeRequests.GetTrackedByIdScopedAsync(
            entityId, tenant.TenantId, tenant.OrgId, ct);
        if (tracked is null)
            return Respons<EmployeeAggregateReadDto>.NotFound("Change request not found.");

        var now = DateTimeOffset.UtcNow;
        tracked.Status = ChangeRequestStatuses.Approved;
        tracked.ReviewedBy = reviewerId;
        tracked.UpdatedAt = now;
        tracked.UpdatedBy = reviewerId;
        await _changeRequests.SaveChangesAsync(ct);

        return applyResult;
    }

    public async Task<Respons<ChangeRequestReadDto>> RejectAsync(
        Guid changeRequestId,
        string? reviewNote,
        CancellationToken ct = default)
    {
        var tenant = _tenant.Current;
        var reviewerId = tenant.UserId;
        if (string.IsNullOrWhiteSpace(reviewerId))
            return Respons<ChangeRequestReadDto>.Forbidden("Authenticated user is required.");

        var entity = await _changeRequests.GetTrackedByIdScopedAsync(changeRequestId, tenant.TenantId, tenant.OrgId, ct);
        if (entity is null)
            return Respons<ChangeRequestReadDto>.NotFound("Change request not found.");

        if (!string.Equals(entity.Status, ChangeRequestStatuses.Pending, StringComparison.Ordinal))
            return Respons<ChangeRequestReadDto>.Fail(
                "Only pending change requests can be rejected.",
                statusCode: 409);

        var now = DateTimeOffset.UtcNow;
        entity.Status = ChangeRequestStatuses.Rejected;
        entity.ReviewedBy = reviewerId;
        entity.ReviewNote = string.IsNullOrWhiteSpace(reviewNote) ? null : reviewNote.Trim();
        entity.UpdatedAt = now;
        entity.UpdatedBy = reviewerId;
        await _changeRequests.SaveChangesAsync(ct);

        var dto = await MapOneAsync(entity, ct);
        return Respons<ChangeRequestReadDto>.Ok(dto);
    }

    public async Task<Respons<IReadOnlyList<ChangeRequestReadDto>>> ListAsync(
        ChangeRequestListQuery query,
        CancellationToken ct = default)
    {
        var tenant = _tenant.Current;
        var rows = await _changeRequests.ListScopedAsync(
            tenant.TenantId,
            tenant.OrgId,
            query.EmployeeId,
            query.Status,
            ct);
        var mapped = await MapManyAsync(rows, ct);
        return Respons<IReadOnlyList<ChangeRequestReadDto>>.Ok(mapped);
    }

    public async Task<Respons<IReadOnlyList<ChangeRequestReadDto>>> ListForEmployeeAsync(
        Guid employeeId,
        string? status,
        CancellationToken ct = default)
    {
        var tenant = _tenant.Current;
        var rows = await _changeRequests.ListScopedAsync(
            tenant.TenantId,
            tenant.OrgId,
            employeeId,
            status,
            ct);
        var mapped = await MapManyAsync(rows, ct);
        return Respons<IReadOnlyList<ChangeRequestReadDto>>.Ok(mapped);
    }

    private async Task<IReadOnlyList<ChangeRequestReadDto>> MapManyAsync(
        IReadOnlyList<EmployeeChangeRequestEntity> rows,
        CancellationToken ct)
    {
        if (rows.Count == 0)
            return [];

        var userIds = ResourceAuditMapper.CollectUserIds(rows.SelectMany(r => new[]
        {
            new[] { r.RequestedBy, r.ReviewedBy, r.CreatedBy, r.UpdatedBy },
        }));
        var users = await _cpUsers.GetByIdsAsync(userIds, _tenant.Current.TenantId, ct);

        return rows.Select(r => MapEntity(r, users)).ToList();
    }

    private async Task<ChangeRequestReadDto> MapOneAsync(EmployeeChangeRequestEntity row, CancellationToken ct)
    {
        var users = await _cpUsers.GetByIdsAsync(
            ResourceAuditMapper.CollectUserIds(new[] { row.RequestedBy, row.ReviewedBy, row.CreatedBy, row.UpdatedBy }),
            _tenant.Current.TenantId,
            ct);
        return MapEntity(row, users);
    }

    private static ChangeRequestReadDto MapEntity(
        EmployeeChangeRequestEntity row,
        IReadOnlyDictionary<string, CpUserDto> users)
    {
        JsonNode? oldValue = row.OldValueJson is null ? null : JsonNode.Parse(row.OldValueJson);
        var newValue = JsonNode.Parse(row.NewValueJson);

        return new ChangeRequestReadDto
        {
            Id = row.Id,
            EmployeeId = row.EmployeeId,
            FieldPath = row.FieldPath,
            OldValue = oldValue,
            NewValue = newValue!,
            Status = row.Status,
            RequestedById = row.RequestedBy,
            RequestedBy = ResourceAuditMapper.ResolveDisplayName(row.RequestedBy, users),
            ReviewedById = row.ReviewedBy,
            ReviewedBy = ResourceAuditMapper.ResolveDisplayName(row.ReviewedBy, users),
            ReviewNote = row.ReviewNote,
            CreatedAt = row.CreatedAt,
            UpdatedAt = row.UpdatedAt,
            CreatedById = row.CreatedBy,
            UpdatedById = row.UpdatedBy,
            CreatedBy = ResourceAuditMapper.ResolveDisplayName(row.CreatedBy, users),
            UpdatedBy = ResourceAuditMapper.ResolveDisplayName(row.UpdatedBy, users),
        };
    }
}
