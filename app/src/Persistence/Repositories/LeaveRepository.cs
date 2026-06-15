using Microsoft.EntityFrameworkCore;
using ZelosHR.Api.Entities.Employees;
using ZelosHR.Api.Entities.Leave;
using ZelosHR.Api.Persistence.Entities;

namespace ZelosHR.Api.Persistence.Repositories;

public sealed class LeaveRepository(ZelosHrDbContext db) : ILeaveRepository
{
    private const decimal LowBalanceThresholdDays = 3m;

    private IQueryable<LeaveRequestEntity> Requests(string tenantId, string orgId) =>
        db.LeaveRequests.AsNoTracking()
            .Where(r => r.TenantId == tenantId && r.OrgId == orgId);

    private IQueryable<LeaveBalanceEntity> Balances(string tenantId, string orgId) =>
        db.LeaveBalances.AsNoTracking()
            .Where(b => b.TenantId == tenantId && b.OrgId == orgId);

    public async Task<LeaveSummaryDto> GetSummaryScopedAsync(
        string tenantId, string orgId, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var weekEnd = today.AddDays(7);
        var monthStart = new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var monthEnd = monthStart.AddMonths(1);
        var query = Requests(tenantId, orgId);

        return new LeaveSummaryDto
        {
            PendingRequests = await query.CountAsync(r => r.Status == LeaveRequestStatuses.Pending, ct),
            PendingFinalApprovals = await query.CountAsync(
                r => r.Status == LeaveRequestStatuses.Pending
                     && r.ApprovalStage == LeaveApprovalStages.PendingFinal,
                ct),
            ApprovedThisMonth = await query.CountAsync(
                r => r.Status == LeaveRequestStatuses.Approved
                     && r.DecidedAt.HasValue
                     && DateOnly.FromDateTime(r.DecidedAt.Value.UtcDateTime) >= monthStart
                     && DateOnly.FromDateTime(r.DecidedAt.Value.UtcDateTime) < monthEnd,
                ct),
            OnLeaveToday = await query.CountAsync(
                r => r.Status == LeaveRequestStatuses.Approved && r.StartDate <= today && r.EndDate >= today, ct),
            LeavingThisWeek = await query.CountAsync(
                r => (r.Status == LeaveRequestStatuses.Approved || r.Status == LeaveRequestStatuses.Pending)
                     && r.StartDate > today
                     && r.StartDate <= weekEnd,
                ct),
            LowBalanceAlert = await Balances(tenantId, orgId)
                .Where(b => b.RemainingDays <= LowBalanceThresholdDays)
                .Select(b => b.EmployeeId)
                .Distinct()
                .CountAsync(ct),
            TotalRequests = await query.CountAsync(ct),
        };
    }

    public async Task<int> CountEmployeeRequestsScopedAsync(
        string tenantId,
        string orgId,
        Guid employeeId,
        string? status,
        DateOnly? fromDate,
        CancellationToken ct = default)
    {
        var query = Requests(tenantId, orgId).Where(r => r.EmployeeId == employeeId);
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(r => r.Status == status);
        if (fromDate.HasValue)
            query = query.Where(r => r.StartDate >= fromDate.Value);
        return await query.CountAsync(ct);
    }

    public async Task<(IReadOnlyList<LeaveRequestRawRow> Requests, int Total)> ListRequestsScopedAsync(
        string tenantId,
        string orgId,
        LeaveRequestListQuery queryParams,
        CancellationToken ct = default)
    {
        var query = ApplyRequestFilters(tenantId, orgId, queryParams);
        var total = await query.CountAsync(ct);
        var entities = await query
            .OrderByDescending(r => r.SubmittedAt)
            .Skip((queryParams.Page - 1) * queryParams.Size)
            .Take(queryParams.Size)
            .ToListAsync(ct);

        return (await MapRawRowsAsync(tenantId, orgId, entities, ct), total);
    }

    public async Task<LeaveRequestRawRow?> GetRequestRawByIdScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default)
    {
        var entity = await Requests(tenantId, orgId).FirstOrDefaultAsync(r => r.Id == id, ct);
        if (entity is null)
            return null;

        var rows = await MapRawRowsAsync(tenantId, orgId, [entity], ct);
        return rows.FirstOrDefault();
    }

    public async Task<IReadOnlyList<LeaveRequestRawRow>> ListOnLeaveTodayScopedAsync(
        string tenantId, string orgId, int limit, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var entities = await Requests(tenantId, orgId)
            .Where(r => r.Status == LeaveRequestStatuses.Approved && r.StartDate <= today && r.EndDate >= today)
            .OrderBy(r => r.EndDate)
            .Take(limit)
            .ToListAsync(ct);

        return await MapRawRowsAsync(tenantId, orgId, entities, ct);
    }

    public async Task<IReadOnlyList<LeaveRequestRawRow>> ListPendingApprovalsScopedAsync(
        string tenantId, string orgId, int limit, CancellationToken ct = default)
    {
        var entities = await Requests(tenantId, orgId)
            .Where(r => r.Status == LeaveRequestStatuses.Pending)
            .OrderBy(r => r.SubmittedAt)
            .Take(limit)
            .ToListAsync(ct);

        return await MapRawRowsAsync(tenantId, orgId, entities, ct);
    }

    public async Task<IReadOnlyList<LeaveRequestRawRow>> ListPendingFinalApprovalsScopedAsync(
        string tenantId, string orgId, int limit, CancellationToken ct = default)
    {
        var entities = await Requests(tenantId, orgId)
            .Where(r => r.Status == LeaveRequestStatuses.Pending
                        && r.ApprovalStage == LeaveApprovalStages.PendingFinal)
            .OrderBy(r => r.SubmittedAt)
            .Take(limit)
            .ToListAsync(ct);

        return await MapRawRowsAsync(tenantId, orgId, entities, ct);
    }

    public async Task<IReadOnlyList<LeaveRequestRawRow>> ListLeavingThisWeekScopedAsync(
        string tenantId, string orgId, int limit, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var weekEnd = today.AddDays(7);
        var entities = await Requests(tenantId, orgId)
            .Where(r => (r.Status == LeaveRequestStatuses.Approved || r.Status == LeaveRequestStatuses.Pending)
                        && r.StartDate > today
                        && r.StartDate <= weekEnd)
            .OrderBy(r => r.StartDate)
            .Take(limit)
            .ToListAsync(ct);

        return await MapRawRowsAsync(tenantId, orgId, entities, ct);
    }

    public async Task<HashSet<DateOnly>> GetPublicHolidayDatesInRangeScopedAsync(
        string tenantId,
        string orgId,
        DateOnly start,
        DateOnly end,
        string? countryCode,
        Guid? branchId,
        CancellationToken ct = default)
    {
        var query = db.PublicHolidays.AsNoTracking()
            .Where(h => h.TenantId == tenantId && h.OrgId == orgId && h.IsActive);

        if (!string.IsNullOrWhiteSpace(countryCode))
            query = query.Where(h => h.CountryCode == countryCode.Trim().ToUpperInvariant());

        if (branchId.HasValue)
            query = query.Where(h => h.BranchId == null || h.BranchId == branchId.Value);

        var holidays = await query.ToListAsync(ct);
        var dates = new HashSet<DateOnly>();
        foreach (var holiday in holidays)
        {
            if (holiday.IsRecurring)
            {
                for (var year = start.Year; year <= end.Year; year++)
                {
                    var date = new DateOnly(year, holiday.HolidayDate.Month, holiday.HolidayDate.Day);
                    if (date >= start && date <= end)
                        dates.Add(date);
                }
            }
            else if (holiday.HolidayDate >= start && holiday.HolidayDate <= end)
            {
                dates.Add(holiday.HolidayDate);
            }
        }

        return dates;
    }

    public async Task<Guid> CreateRequestScopedAsync(
        string tenantId,
        string orgId,
        Guid employeeId,
        string employeeFullName,
        Guid leaveTypeId,
        string leaveTypeName,
        DateOnly startDate,
        DateOnly endDate,
        decimal daysRequested,
        string? notes,
        string initialApprovalStage,
        string? actorUserId = null,
        CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var entity = new LeaveRequestEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OrgId = orgId,
            EmployeeId = employeeId,
            EmployeeFullName = employeeFullName,
            LeaveTypeId = leaveTypeId,
            LeaveType = leaveTypeName,
            StartDate = startDate,
            EndDate = endDate,
            DaysRequested = daysRequested,
            Status = LeaveRequestStatuses.Pending,
            ApprovalStage = initialApprovalStage,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            SubmittedAt = now,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = actorUserId,
            UpdatedBy = actorUserId,
        };
        db.LeaveRequests.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity.Id;
    }

    public async Task<LeaveRequestRawRow?> UpdateRequestScopedAsync(
        Guid id,
        string tenantId,
        string orgId,
        string? status,
        string? notes,
        string? actorUserId = null,
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
        if (notes is not null)
        {
            entity.Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
            changed = true;
        }

        if (!changed)
            return null;

        entity.UpdatedAt = DateTimeOffset.UtcNow;
        entity.UpdatedBy = actorUserId;
        await db.SaveChangesAsync(ct);
        return await GetRequestRawByIdScopedAsync(id, tenantId, orgId, ct);
    }

    public async Task<LeaveRequestRawRow?> AdvanceApprovalScopedAsync(
        Guid id,
        string tenantId,
        string orgId,
        string approverPlatformUserId,
        Guid? approverEmployeeId,
        EmployeeLeaveContext requestEmployee,
        CancellationToken ct = default)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var entity = await db.LeaveRequests.FirstOrDefaultAsync(
            r => r.Id == id && r.TenantId == tenantId && r.OrgId == orgId, ct);
        if (entity is null || entity.Status != LeaveRequestStatuses.Pending)
            return null;

        var now = DateTimeOffset.UtcNow;
        switch (entity.ApprovalStage)
        {
            case LeaveApprovalStages.PendingLineManager:
                if (!IsLineManager(approverEmployeeId, requestEmployee))
                    return null;

                entity.LmApproverId = approverPlatformUserId.Trim();
                entity.LmDecidedAt = now;
                entity.ApprovalStage = ResolveNextStageAfterLineManager(requestEmployee);
                if (entity.ApprovalStage == LeaveApprovalStages.PendingHeadOfDepartment
                    && requestEmployee.LineManagerEmployeeId == requestEmployee.HeadOfDepartmentEmployeeId)
                {
                    entity.HodApproverId = approverPlatformUserId.Trim();
                    entity.HodDecidedAt = now;
                    entity.ApprovalStage = LeaveApprovalStages.PendingFinal;
                }

                break;

            case LeaveApprovalStages.PendingHeadOfDepartment:
                if (!IsHeadOfDepartment(approverEmployeeId, requestEmployee))
                    return null;

                entity.HodApproverId = approverPlatformUserId.Trim();
                entity.HodDecidedAt = now;
                entity.ApprovalStage = LeaveApprovalStages.PendingFinal;
                break;

            case LeaveApprovalStages.PendingFinal:
                if (entity.LeaveTypeId.HasValue)
                {
                    var balance = await db.LeaveBalances.FirstOrDefaultAsync(
                        b => b.TenantId == tenantId
                             && b.OrgId == orgId
                             && b.EmployeeId == entity.EmployeeId
                             && b.LeaveTypeId == entity.LeaveTypeId,
                        ct);
                    if (balance is not null)
                    {
                        balance.UsedDays += entity.DaysRequested;
                        balance.RemainingDays = balance.EntitledDays - balance.UsedDays;
                        balance.UpdatedAt = now;
                        balance.UpdatedBy = approverPlatformUserId.Trim();
                    }
                }

                entity.Status = LeaveRequestStatuses.Approved;
                entity.ApprovalStage = LeaveApprovalStages.Approved;
                entity.ApproverId = approverPlatformUserId.Trim();
                entity.DecidedAt = now;
                break;

            default:
                return null;
        }

        entity.UpdatedAt = now;
        entity.UpdatedBy = approverPlatformUserId.Trim();
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return await GetRequestRawByIdScopedAsync(id, tenantId, orgId, ct);
    }

    public async Task<LeaveRequestRawRow?> RejectRequestScopedAsync(
        Guid id, string tenantId, string orgId, string approverId, string? notes, CancellationToken ct = default)
    {
        var entity = await db.LeaveRequests.FirstOrDefaultAsync(
            r => r.Id == id && r.TenantId == tenantId && r.OrgId == orgId, ct);
        if (entity is null || entity.Status != LeaveRequestStatuses.Pending)
            return null;

        entity.Status = LeaveRequestStatuses.Rejected;
        entity.ApprovalStage = LeaveApprovalStages.Rejected;
        entity.ApproverId = approverId.Trim();
        entity.DecidedAt = DateTimeOffset.UtcNow;
        entity.UpdatedAt = entity.DecidedAt.Value;
        entity.UpdatedBy = approverId.Trim();
        if (notes is not null)
            entity.Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();

        await db.SaveChangesAsync(ct);
        return await GetRequestRawByIdScopedAsync(id, tenantId, orgId, ct);
    }

    public async Task<bool> DeletePendingRequestScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default)
    {
        var entity = await db.LeaveRequests.FirstOrDefaultAsync(
            r => r.Id == id && r.TenantId == tenantId && r.OrgId == orgId && r.Status == LeaveRequestStatuses.Pending, ct);
        if (entity is null)
            return false;
        db.LeaveRequests.Remove(entity);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<IReadOnlyList<LeaveBalanceRawRow>> ListBalancesScopedAsync(
        string tenantId,
        string orgId,
        Guid? employeeId,
        Guid? leaveTypeId,
        CancellationToken ct = default)
    {
        var query = Balances(tenantId, orgId);
        if (employeeId.HasValue)
            query = query.Where(b => b.EmployeeId == employeeId.Value);
        if (leaveTypeId.HasValue)
            query = query.Where(b => b.LeaveTypeId == leaveTypeId.Value);

        return await query
            .OrderBy(b => b.EmployeeFullName)
            .ThenBy(b => b.LeaveType)
            .Select(b => ToBalanceRawRow(b))
            .ToListAsync(ct);
    }

    public async Task<LeaveBalanceRawRow?> GetBalanceScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default)
    {
        var entity = await Balances(tenantId, orgId).FirstOrDefaultAsync(b => b.Id == id, ct);
        return entity is null ? null : ToBalanceRawRow(entity);
    }

    public async Task<LeaveBalanceRawRow?> GetBalanceForEmployeeScopedAsync(
        string tenantId, string orgId, Guid employeeId, Guid leaveTypeId, CancellationToken ct = default)
    {
        var entity = await Balances(tenantId, orgId).FirstOrDefaultAsync(
            b => b.EmployeeId == employeeId && b.LeaveTypeId == leaveTypeId, ct);
        return entity is null ? null : ToBalanceRawRow(entity);
    }

    public async Task<Guid> CreateBalanceScopedAsync(
        string tenantId,
        string orgId,
        Guid employeeId,
        string employeeFullName,
        Guid leaveTypeId,
        string leaveTypeName,
        decimal entitledDays,
        decimal usedDays,
        string? actorUserId = null,
        CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var entity = new LeaveBalanceEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OrgId = orgId,
            EmployeeId = employeeId,
            EmployeeFullName = employeeFullName,
            LeaveTypeId = leaveTypeId,
            LeaveType = leaveTypeName,
            EntitledDays = entitledDays,
            UsedDays = usedDays,
            RemainingDays = entitledDays - usedDays,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = actorUserId,
            UpdatedBy = actorUserId,
        };
        db.LeaveBalances.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity.Id;
    }

    public async Task<LeaveBalanceRawRow?> UpdateBalanceScopedAsync(
        Guid id, string tenantId, string orgId, decimal? entitledDays, decimal? usedDays, string? actorUserId = null, CancellationToken ct = default)
    {
        var entity = await db.LeaveBalances.FirstOrDefaultAsync(
            b => b.Id == id && b.TenantId == tenantId && b.OrgId == orgId, ct);
        if (entity is null)
            return null;

        var changed = false;
        if (entitledDays.HasValue)
        {
            entity.EntitledDays = entitledDays.Value;
            changed = true;
        }
        if (usedDays.HasValue)
        {
            entity.UsedDays = usedDays.Value;
            changed = true;
        }

        if (!changed)
            return null;

        entity.RemainingDays = entity.EntitledDays - entity.UsedDays;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        entity.UpdatedBy = actorUserId;
        await db.SaveChangesAsync(ct);
        return ToBalanceRawRow(entity);
    }

    public async Task<(IReadOnlyList<LeaveTypeListItemDto> Items, int Total)> ListTypesScopedAsync(
        string tenantId,
        string orgId,
        bool activeOnly,
        string? search,
        int page,
        int size,
        CancellationToken ct = default)
    {
        var query = db.LeaveTypes.AsNoTracking()
            .Where(t => t.TenantId == tenantId && t.OrgId == orgId);
        if (activeOnly)
            query = query.Where(t => t.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(t => EF.Functions.ILike(t.Name, $"%{term}%"));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(t => t.Name)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(t => ToTypeDto(t))
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<IReadOnlyList<LeaveTypeListItemDto>> ListAllTypesScopedAsync(
        string tenantId, string orgId, bool activeOnly, CancellationToken ct = default)
    {
        var query = db.LeaveTypes.AsNoTracking()
            .Where(t => t.TenantId == tenantId && t.OrgId == orgId);
        if (activeOnly)
            query = query.Where(t => t.IsActive);

        return await query
            .OrderBy(t => t.Name)
            .Select(t => ToTypeDto(t))
            .ToListAsync(ct);
    }

    public async Task<LeaveTypeListItemDto?> GetTypeByIdScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default) =>
        await db.LeaveTypes.AsNoTracking()
            .Where(t => t.TenantId == tenantId && t.OrgId == orgId && t.Id == id)
            .Select(t => ToTypeDto(t))
            .FirstOrDefaultAsync(ct);

    public async Task<bool> TypeNameExistsScopedAsync(
        string tenantId, string orgId, string name, Guid? excludeId, CancellationToken ct = default)
    {
        var query = db.LeaveTypes.AsNoTracking()
            .Where(t => t.TenantId == tenantId && t.OrgId == orgId && t.Name == name);
        if (excludeId.HasValue)
            query = query.Where(t => t.Id != excludeId.Value);
        return await query.AnyAsync(ct);
    }

    public async Task<Guid> CreateTypeScopedAsync(
        string tenantId, string orgId, CreateLeaveTypeDto data, string? actorUserId = null, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var entity = new LeaveTypeEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OrgId = orgId,
            Name = data.Name!.Trim(),
            CountryCode = null,
            DefaultEntitledDays = data.DefaultEntitledDays!.Value,
            IsPaid = data.IsPaid,
            IsActive = true,
            AccrualMethod = LeaveTypePolicy.NormalizeAccrualMethod(data.AccrualMethod!),
            CarryOverAllowed = data.CarryOverAllowed,
            AppliesToEmploymentTypes = LeaveTypePolicy.SerializeEmploymentTypes(
                LeaveTypePolicy.NormalizeEmploymentTypes(data.AppliesToEmploymentTypes)),
            MinNoticeWorkingDays = data.MinNoticeWorkingDays,
            MaxConsecutiveDays = data.MaxConsecutiveDays,
            RequiresSupportingDocument = data.RequiresSupportingDocument,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = actorUserId,
            UpdatedBy = actorUserId,
        };
        db.LeaveTypes.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity.Id;
    }

    public async Task<LeaveTypeListItemDto?> UpdateTypeScopedAsync(
        Guid id, string tenantId, string orgId, CreateLeaveTypeDto data, string? actorUserId = null, CancellationToken ct = default)
    {
        var entity = await db.LeaveTypes.FirstOrDefaultAsync(
            t => t.Id == id && t.TenantId == tenantId && t.OrgId == orgId, ct);
        if (entity is null)
            return null;

        entity.Name = data.Name!.Trim();
        entity.DefaultEntitledDays = data.DefaultEntitledDays!.Value;
        entity.IsPaid = data.IsPaid;
        entity.AccrualMethod = LeaveTypePolicy.NormalizeAccrualMethod(data.AccrualMethod!);
        entity.CarryOverAllowed = data.CarryOverAllowed;
        entity.AppliesToEmploymentTypes = LeaveTypePolicy.SerializeEmploymentTypes(
            LeaveTypePolicy.NormalizeEmploymentTypes(data.AppliesToEmploymentTypes));
        entity.MinNoticeWorkingDays = data.MinNoticeWorkingDays;
        entity.MaxConsecutiveDays = data.MaxConsecutiveDays;
        entity.RequiresSupportingDocument = data.RequiresSupportingDocument;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        entity.UpdatedBy = actorUserId;
        await db.SaveChangesAsync(ct);
        return ToTypeDto(entity);
    }

    public async Task<LeaveTypeListItemDto?> ArchiveTypeScopedAsync(
        Guid id, string tenantId, string orgId, string? actorUserId = null, CancellationToken ct = default)
    {
        var entity = await db.LeaveTypes.FirstOrDefaultAsync(
            t => t.Id == id && t.TenantId == tenantId && t.OrgId == orgId, ct);
        if (entity is null)
            return null;

        if (entity.IsActive)
        {
            entity.IsActive = false;
            entity.UpdatedAt = DateTimeOffset.UtcNow;
            entity.UpdatedBy = actorUserId;
            await db.SaveChangesAsync(ct);
        }

        return ToTypeDto(entity);
    }

    public async Task<bool> TypeInUseScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default) =>
        await db.LeaveRequests.AsNoTracking().AnyAsync(
            r => r.TenantId == tenantId && r.OrgId == orgId && r.LeaveTypeId == id, ct)
        || await db.LeaveBalances.AsNoTracking().AnyAsync(
            b => b.TenantId == tenantId && b.OrgId == orgId && b.LeaveTypeId == id, ct);

    public async Task<bool> DeleteTypeScopedAsync(Guid id, string tenantId, string orgId, CancellationToken ct = default)
    {
        var entity = await db.LeaveTypes.FirstOrDefaultAsync(
            t => t.Id == id && t.TenantId == tenantId && t.OrgId == orgId, ct);
        if (entity is null)
            return false;

        if (await TypeInUseScopedAsync(id, tenantId, orgId, ct))
            return false;

        db.LeaveTypes.Remove(entity);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<(IReadOnlyList<PublicHolidayListItemDto> Items, int Total)> ListHolidaysScopedAsync(
        string tenantId,
        string orgId,
        string? countryCode,
        int? year,
        Guid? branchId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = db.PublicHolidays.AsNoTracking()
            .Where(h => h.TenantId == tenantId && h.OrgId == orgId && h.IsActive);
        if (!string.IsNullOrWhiteSpace(countryCode))
            query = query.Where(h => h.CountryCode == countryCode.Trim().ToUpperInvariant());
        if (year.HasValue)
            query = query.Where(h => h.HolidayDate.Year == year.Value || h.IsRecurring);
        if (branchId.HasValue)
            query = query.Where(h => h.BranchId == null || h.BranchId == branchId.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(h => h.HolidayDate)
            .ThenBy(h => h.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(h => ToHolidayDto(h))
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<PublicHolidayListItemDto?> GetHolidayByIdScopedAsync(
        Guid id, string tenantId, string orgId, CancellationToken ct = default) =>
        await db.PublicHolidays.AsNoTracking()
            .Where(h => h.TenantId == tenantId && h.OrgId == orgId && h.Id == id)
            .Select(h => ToHolidayDto(h))
            .FirstOrDefaultAsync(ct);

    public async Task<Guid> CreateHolidayScopedAsync(
        string tenantId, string orgId, CreatePublicHolidayDto data, string? actorUserId = null, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var entity = new PublicHolidayEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OrgId = orgId,
            CountryCode = data.CountryCode!.Trim().ToUpperInvariant(),
            Name = data.Name!.Trim(),
            HolidayDate = data.HolidayDate,
            IsRecurring = data.IsRecurring,
            BranchId = data.BranchId,
            IsActive = data.IsActive,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = actorUserId,
            UpdatedBy = actorUserId,
        };
        db.PublicHolidays.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity.Id;
    }

    public async Task<PublicHolidayListItemDto?> UpdateHolidayScopedAsync(
        Guid id, string tenantId, string orgId, UpdatePublicHolidayDto data, string? actorUserId = null, CancellationToken ct = default)
    {
        var entity = await db.PublicHolidays.FirstOrDefaultAsync(
            h => h.Id == id && h.TenantId == tenantId && h.OrgId == orgId, ct);
        if (entity is null)
            return null;

        var changed = false;
        if (!string.IsNullOrWhiteSpace(data.CountryCode))
        {
            entity.CountryCode = data.CountryCode.Trim().ToUpperInvariant();
            changed = true;
        }
        if (!string.IsNullOrWhiteSpace(data.Name))
        {
            entity.Name = data.Name.Trim();
            changed = true;
        }
        if (data.HolidayDate.HasValue)
        {
            entity.HolidayDate = data.HolidayDate.Value;
            changed = true;
        }
        if (data.IsRecurring.HasValue)
        {
            entity.IsRecurring = data.IsRecurring.Value;
            changed = true;
        }
        if (data.BranchId.HasValue)
        {
            entity.BranchId = data.BranchId;
            changed = true;
        }
        if (data.IsActive.HasValue)
        {
            entity.IsActive = data.IsActive.Value;
            changed = true;
        }

        if (!changed)
            return null;

        entity.UpdatedAt = DateTimeOffset.UtcNow;
        entity.UpdatedBy = actorUserId;
        await db.SaveChangesAsync(ct);
        return ToHolidayDto(entity);
    }

    public async Task<bool> DeleteHolidayScopedAsync(Guid id, string tenantId, string orgId, CancellationToken ct = default)
    {
        var entity = await db.PublicHolidays.FirstOrDefaultAsync(
            h => h.Id == id && h.TenantId == tenantId && h.OrgId == orgId, ct);
        if (entity is null)
            return false;

        entity.IsActive = false;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return true;
    }

    private IQueryable<LeaveRequestEntity> ApplyRequestFilters(
        string tenantId, string orgId, LeaveRequestListQuery queryParams)
    {
        var query = Requests(tenantId, orgId);

        if (queryParams.LeaveRequestId.HasValue)
            query = query.Where(r => r.Id == queryParams.LeaveRequestId.Value);

        if (queryParams.EmployeeId.HasValue)
            query = query.Where(r => r.EmployeeId == queryParams.EmployeeId.Value);

        if (!string.IsNullOrWhiteSpace(queryParams.EmployeeCode))
        {
            var codeTerm = $"%{queryParams.EmployeeCode.Trim()}%";
            var codeEmployeeIds = db.Employees.AsNoTracking()
                .Where(e => e.TenantId == tenantId && e.OrgId == orgId && !e.IsDeleted)
                .Where(e => EF.Functions.ILike(e.EmployeeCode, codeTerm))
                .Select(e => e.Id);
            query = query.Where(r => codeEmployeeIds.Contains(r.EmployeeId));
        }

        if (!string.IsNullOrWhiteSpace(queryParams.Search) && queryParams.Search.Trim().Length >= 2)
        {
            var term = $"%{queryParams.Search.Trim()}%";
            var matchingEmployeeIds = db.Employees.AsNoTracking()
                .Where(e => e.TenantId == tenantId && e.OrgId == orgId && !e.IsDeleted)
                .Where(e =>
                    EF.Functions.ILike(e.FullName, term)
                    || EF.Functions.ILike(e.EmployeeCode, term)
                    || (e.JobTitle != null && EF.Functions.ILike(e.JobTitle, term)))
                .Select(e => e.Id);

            query = query.Where(r =>
                EF.Functions.ILike(r.EmployeeFullName, term)
                || EF.Functions.ILike(r.LeaveType, term)
                || matchingEmployeeIds.Contains(r.EmployeeId));
        }

        if (!string.IsNullOrWhiteSpace(queryParams.Status) && !queryParams.Status.Equals("all", StringComparison.OrdinalIgnoreCase))
            query = query.Where(r => r.Status == queryParams.Status.Trim());

        if (!string.IsNullOrWhiteSpace(queryParams.ApprovalStage)
            && !queryParams.ApprovalStage.Equals("all", StringComparison.OrdinalIgnoreCase))
            query = query.Where(r => r.ApprovalStage == queryParams.ApprovalStage.Trim());

        if (queryParams.LeaveTypeId.HasValue)
            query = query.Where(r => r.LeaveTypeId == queryParams.LeaveTypeId.Value);

        if (queryParams.DepartmentId.HasValue)
        {
            var deptEmployeeIds = db.Employees.AsNoTracking()
                .Where(e => e.TenantId == tenantId && e.OrgId == orgId && !e.IsDeleted
                            && e.DepartmentId == queryParams.DepartmentId.Value)
                .Select(e => e.Id);
            query = query.Where(r => deptEmployeeIds.Contains(r.EmployeeId));
        }

        if (queryParams.BranchId.HasValue)
        {
            var branchEmployeeIds = db.Employees.AsNoTracking()
                .Where(e => e.TenantId == tenantId && e.OrgId == orgId && !e.IsDeleted
                            && e.BranchId == queryParams.BranchId.Value)
                .Select(e => e.Id);
            query = query.Where(r => branchEmployeeIds.Contains(r.EmployeeId));
        }

        if (queryParams.FromDate.HasValue)
            query = query.Where(r => r.EndDate >= queryParams.FromDate.Value);
        if (queryParams.ToDate.HasValue)
            query = query.Where(r => r.StartDate <= queryParams.ToDate.Value);

        if (queryParams.SubmittedFromDate.HasValue)
        {
            var from = new DateTimeOffset(queryParams.SubmittedFromDate.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            query = query.Where(r => r.SubmittedAt >= from);
        }

        if (queryParams.SubmittedToDate.HasValue)
        {
            var to = new DateTimeOffset(queryParams.SubmittedToDate.Value.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero);
            query = query.Where(r => r.SubmittedAt <= to);
        }

        return query;
    }

    private async Task<IReadOnlyList<LeaveRequestRawRow>> MapRawRowsAsync(
        string tenantId,
        string orgId,
        IReadOnlyList<LeaveRequestEntity> entities,
        CancellationToken ct)
    {
        if (entities.Count == 0)
            return [];

        var remainingByKey = await LoadRemainingLookupAsync(
            tenantId,
            orgId,
            entities.Where(r => r.LeaveTypeId.HasValue).Select(r => (r.EmployeeId, r.LeaveTypeId!.Value)),
            ct);

        return entities
            .Select(r => ToRawRow(
                r,
                r.LeaveTypeId.HasValue
                    ? remainingByKey.GetValueOrDefault((r.EmployeeId, r.LeaveTypeId.Value))
                    : null))
            .ToList();
    }

    private static bool IsLineManager(Guid? approverEmployeeId, EmployeeLeaveContext requestEmployee) =>
        approverEmployeeId.HasValue
        && requestEmployee.LineManagerEmployeeId.HasValue
        && approverEmployeeId.Value == requestEmployee.LineManagerEmployeeId.Value;

    private static bool IsHeadOfDepartment(Guid? approverEmployeeId, EmployeeLeaveContext requestEmployee) =>
        approverEmployeeId.HasValue
        && requestEmployee.HeadOfDepartmentEmployeeId.HasValue
        && approverEmployeeId.Value == requestEmployee.HeadOfDepartmentEmployeeId.Value;

    private static string ResolveNextStageAfterLineManager(EmployeeLeaveContext requestEmployee) =>
        requestEmployee.HeadOfDepartmentEmployeeId.HasValue
            ? LeaveApprovalStages.PendingHeadOfDepartment
            : LeaveApprovalStages.PendingFinal;

    private async Task<Dictionary<(Guid EmployeeId, Guid LeaveTypeId), decimal>> LoadRemainingLookupAsync(
        string tenantId,
        string orgId,
        IEnumerable<(Guid EmployeeId, Guid LeaveTypeId)> keys,
        CancellationToken ct)
    {
        var keyList = keys.Distinct().ToList();
        if (keyList.Count == 0)
            return [];

        var employeeIds = keyList.Select(k => k.EmployeeId).Distinct().ToList();
        var leaveTypeIds = keyList.Select(k => k.LeaveTypeId).Distinct().ToList();
        var rows = await Balances(tenantId, orgId)
            .Where(b => employeeIds.Contains(b.EmployeeId) && b.LeaveTypeId.HasValue && leaveTypeIds.Contains(b.LeaveTypeId.Value))
            .Select(b => new { b.EmployeeId, LeaveTypeId = b.LeaveTypeId!.Value, b.RemainingDays })
            .ToListAsync(ct);

        return rows.ToDictionary(r => (r.EmployeeId, r.LeaveTypeId), r => r.RemainingDays);
    }

    private static LeaveRequestRawRow ToRawRow(LeaveRequestEntity r, decimal? remainingDays) => new(
        r.Id.ToString(),
        r.EmployeeId.ToString(),
        r.EmployeeFullName,
        r.LeaveTypeId?.ToString() ?? string.Empty,
        r.LeaveType,
        r.StartDate,
        r.EndDate,
        r.DaysRequested,
        r.Status,
        r.ApprovalStage,
        r.ApproverId,
        r.ApproverName,
        r.LmApproverId,
        r.LmDecidedAt,
        r.HodApproverId,
        r.HodDecidedAt,
        r.Notes,
        remainingDays,
        r.SubmittedAt,
        r.DecidedAt,
        r.CreatedAt,
        r.UpdatedAt,
        r.CreatedBy,
        r.UpdatedBy);

    private static LeaveBalanceRawRow ToBalanceRawRow(LeaveBalanceEntity b) => new(
        b.Id.ToString(),
        b.EmployeeId.ToString(),
        b.EmployeeFullName,
        b.LeaveTypeId?.ToString() ?? string.Empty,
        b.LeaveType,
        b.EntitledDays,
        b.UsedDays,
        b.RemainingDays,
        b.CreatedAt,
        b.UpdatedAt,
        b.CreatedBy,
        b.UpdatedBy);

    private static LeaveTypeListItemDto ToTypeDto(LeaveTypeEntity t) => new()
    {
        LeaveTypeId = t.Id.ToString(),
        Name = t.Name,
        DefaultEntitledDays = t.DefaultEntitledDays,
        IsPaid = t.IsPaid,
        AccrualMethod = t.AccrualMethod,
        CarryOverAllowed = t.CarryOverAllowed,
        AppliesToEmploymentTypes = LeaveTypePolicy.DeserializeEmploymentTypes(t.AppliesToEmploymentTypes),
        MinNoticeWorkingDays = t.MinNoticeWorkingDays,
        MaxConsecutiveDays = t.MaxConsecutiveDays,
        RequiresSupportingDocument = t.RequiresSupportingDocument,
        CreatedAt = t.CreatedAt,
        UpdatedAt = t.UpdatedAt,
        CreatedById = t.CreatedBy,
        UpdatedById = t.UpdatedBy,
    };

    private static PublicHolidayListItemDto ToHolidayDto(PublicHolidayEntity h) => new()
    {
        HolidayId = h.Id.ToString(),
        CountryCode = h.CountryCode,
        Name = h.Name,
        HolidayDate = h.HolidayDate,
        IsRecurring = h.IsRecurring,
        BranchId = h.BranchId?.ToString(),
        IsActive = h.IsActive,
        CreatedAt = h.CreatedAt,
        UpdatedAt = h.UpdatedAt,
        CreatedById = h.CreatedBy,
        UpdatedById = h.UpdatedBy,
    };
}
