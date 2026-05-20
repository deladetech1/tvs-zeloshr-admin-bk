using Microsoft.EntityFrameworkCore;
using ZelosHR.Api.Entities.Leave;
using ZelosHR.Api.Persistence.Entities;

namespace ZelosHR.Api.Persistence.Repositories;

public sealed class LeaveRepository(ZelosHrDbContext db) : ILeaveRepository
{
    private IQueryable<LeaveRequestEntity> Requests(string tenantId, string orgId) =>
        db.LeaveRequests.AsNoTracking()
            .Where(r => r.TenantId == tenantId && r.OrgId == orgId);

    public async Task<LeaveSummaryDto> GetSummaryScopedAsync(
        string tenantId, string orgId, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var monthStart = new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var monthEnd = monthStart.AddMonths(1);
        var query = Requests(tenantId, orgId);

        return new LeaveSummaryDto
        {
            PendingRequests = await query.CountAsync(r => r.Status == "Pending", ct),
            ApprovedThisMonth = await query.CountAsync(
                r => r.Status == "Approved"
                     && DateOnly.FromDateTime(r.SubmittedAt.UtcDateTime) >= monthStart
                     && DateOnly.FromDateTime(r.SubmittedAt.UtcDateTime) < monthEnd,
                ct),
            OnLeaveToday = await query.CountAsync(
                r => r.Status == "Approved" && r.StartDate <= today && r.EndDate >= today, ct),
            TotalRequests = await query.CountAsync(ct),
        };
    }

    public async Task<(IReadOnlyList<LeaveRequestListItemDto> Requests, IReadOnlyList<LeaveBalanceListItemDto> Balances, int Total)> ListScopedAsync(
        string tenantId,
        string orgId,
        string? search,
        string? status,
        string? leaveType,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = Requests(tenantId, orgId);
        if (!string.IsNullOrWhiteSpace(search) && search.Trim().Length >= 3)
            query = query.Where(r => EF.Functions.ILike(r.EmployeeFullName, $"%{search.Trim()}%"));
        if (!string.IsNullOrWhiteSpace(status) && !status.Equals("all", StringComparison.OrdinalIgnoreCase))
            query = query.Where(r => r.Status == status.Trim());
        if (!string.IsNullOrWhiteSpace(leaveType) && !leaveType.Equals("all", StringComparison.OrdinalIgnoreCase))
            query = query.Where(r => r.LeaveType == leaveType.Trim());

        var total = await query.CountAsync(ct);
        var requests = await query
            .OrderByDescending(r => r.SubmittedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => ToRequestDto(r))
            .ToListAsync(ct);

        var balances = await db.LeaveBalances.AsNoTracking()
            .Where(b => b.TenantId == tenantId && b.OrgId == orgId)
            .OrderBy(b => b.EmployeeFullName)
            .Select(b => new LeaveBalanceListItemDto
            {
                EmployeeId = b.EmployeeId.ToString(),
                EmployeeFullName = b.EmployeeFullName,
                LeaveType = b.LeaveType,
                EntitledDays = b.EntitledDays,
                UsedDays = b.UsedDays,
                RemainingDays = b.RemainingDays,
            })
            .ToListAsync(ct);

        return (requests, balances, total);
    }

    public async Task<LeaveRequestListItemDto?> GetRequestByIdScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default)
    {
        var entity = await Requests(tenantId, orgId).FirstOrDefaultAsync(r => r.Id == id, ct);
        return entity is null ? null : ToRequestDto(entity);
    }

    public async Task<Guid> CreateRequestScopedAsync(
        string tenantId,
        string orgId,
        Guid employeeId,
        string employeeFullName,
        string leaveType,
        DateOnly startDate,
        DateOnly endDate,
        decimal daysRequested,
        CancellationToken ct = default)
    {
        var entity = new LeaveRequestEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OrgId = orgId,
            EmployeeId = employeeId,
            EmployeeFullName = employeeFullName,
            LeaveType = leaveType,
            StartDate = startDate,
            EndDate = endDate,
            DaysRequested = daysRequested,
            Status = "Pending",
            SubmittedAt = DateTimeOffset.UtcNow,
        };
        db.LeaveRequests.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity.Id;
    }

    public async Task<LeaveRequestListItemDto?> UpdateRequestScopedAsync(
        Guid id,
        string tenantId,
        string orgId,
        string? status,
        string? approverName,
        CancellationToken ct = default)
    {
        var entity = await db.LeaveRequests.FirstOrDefaultAsync(
            r => r.Id == id && r.TenantId == tenantId && r.OrgId == orgId, ct);
        if (entity is null)
            return null;

        var changed = false;
        if (!string.IsNullOrWhiteSpace(status))
        {
            entity.Status = status.Trim();
            changed = true;
        }
        if (approverName is not null)
        {
            entity.ApproverName = string.IsNullOrWhiteSpace(approverName) ? null : approverName.Trim();
            changed = true;
        }

        if (!changed)
            return null;

        await db.SaveChangesAsync(ct);
        return ToRequestDto(entity);
    }

    public async Task<bool> DeletePendingRequestScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default)
    {
        var entity = await db.LeaveRequests.FirstOrDefaultAsync(
            r => r.Id == id && r.TenantId == tenantId && r.OrgId == orgId && r.Status == "Pending", ct);
        if (entity is null)
            return false;
        db.LeaveRequests.Remove(entity);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static LeaveRequestListItemDto ToRequestDto(LeaveRequestEntity r) => new()
    {
        LeaveRequestId = r.Id.ToString(),
        EmployeeId = r.EmployeeId.ToString(),
        EmployeeFullName = r.EmployeeFullName,
        LeaveType = r.LeaveType,
        StartDate = r.StartDate,
        EndDate = r.EndDate,
        DaysRequested = r.DaysRequested,
        Status = r.Status,
        ApproverName = r.ApproverName,
    };
}
