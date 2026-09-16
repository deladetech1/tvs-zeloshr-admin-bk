using ZelosHR.Api.Persistence.Entities;

namespace ZelosHR.Api.Entities.Adjustments;

public interface IAdjustmentRepository
{
    Task<(IReadOnlyList<AdjustmentDto> Items, int Total)> ListScopedAsync(
        string tenantId,
        string orgId,
        Guid? employeeId,
        DateOnly? from,
        DateOnly? to,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<AdjustmentEntity> AddAsync(
        string tenantId,
        string orgId,
        Guid employeeId,
        Guid? attendanceId,
        DateOnly attendanceDate,
        string kind,
        string? punchType,
        TimeOnly? punchTime,
        Guid? originalPunchId,
        string reason,
        string? actorUserId,
        CancellationToken ct = default);
}
